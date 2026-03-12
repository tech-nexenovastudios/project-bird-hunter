using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Levels;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Managers;
using Gameplay.Events;

namespace Gameplay
{
    public class SpawnController : MonoBehaviour
    {
        public static SpawnController Instance;

        [Header("Config")] public LevelProfile levelProfile;
        public AdaptiveDifficultyConfig adaptiveConfig;
        public BirdConfig[]  birds;
        public Transform[]   birdSpawnPoints;

        [Header("State (read-only)")] public float elapsedTime;

        PressureTracker         _pressureTracker;
        readonly List<BaseBird> _activeBirds          = new();
        readonly List<Egg>      _activeEggs            = new();
        readonly Dictionary<string, int> _eggTierCounts = new(); // per-tier active count

        float _spawnTimer;
        bool  _reliefMode;
        bool  _levelCompleted;
        bool  _draining;
        int   _totalTrackedScore;

        float _performanceExpectedScore;
        float _performanceRatio = 1f;

        // Public accessors for UI
        public int   TotalTrackedScore => _totalTrackedScore;
        public int   ActiveEggCount    => _activeEggs.Count;
        public int   ActiveBirdCount   => _activeBirds.Count;

        void Awake()
        {
            Instance         = this;
            _pressureTracker = new PressureTracker();
        }

        void OnEnable()  => GameEvents.OnLevelCompleted += HandleLevelCompleted;
        void OnDisable() => GameEvents.OnLevelCompleted -= HandleLevelCompleted;

        void HandleLevelCompleted(int finalScore) => _levelCompleted = true;

        void Start() => ResetLevel();

        public void ResetLevel()
        {
            elapsedTime               = 0f;
            _spawnTimer               = 0f;
            _reliefMode               = false;
            _levelCompleted           = false;
            _draining                 = false;
            _totalTrackedScore        = 0;
            _performanceExpectedScore = 0f;
            _performanceRatio         = 1f;
            _eggTierCounts.Clear();

            if (levelProfile != null)
                ValidateProfile();
        }

        void Update()
        {
            if (Managers.GameManager.Instance.state != GameState.Gameplay) return;
            elapsedTime += Time.deltaTime;
            UpdatePerformance();
            UpdateReliefMode();

            _spawnTimer += Time.deltaTime;
            float interval = GetCurrentSpawnInterval();

            if (!_reliefMode && !_levelCompleted && !_draining && _spawnTimer >= interval)
            {
                TrySpawnBird();
                _spawnTimer = 0f;
            }
        }

        // ─────────────────────────────────────────────
        // Profile Validation — uses expectedDps to catch
        // misconfigured levels before gameplay starts
        // ─────────────────────────────────────────────
        private void ValidateProfile()
        {
            if (levelProfile.expectedDps <= 0) return;
            float estimatedTime = levelProfile.targetScore / (float)levelProfile.expectedDps;
            if (estimatedTime > levelProfile.maxDuration)
                Debug.LogWarning(
                    $"[SpawnController] Level {levelProfile.globalLevel}: targetScore unreachable! " +
                    $"Needs {estimatedTime:F0}s but maxDuration={levelProfile.maxDuration}s");
        }

        // ─────────────────────────────────────────────
        // Drain — called when target score is reached
        // Respects minDuration before clearing
        // ─────────────────────────────────────────────
        public void StartDrain(int scoreAtTrigger)
        {
            if (_draining) return;
            _draining = true;

            float remaining = levelProfile.minDuration - elapsedTime;
            if (remaining > 0f)
            {
                Debug.Log($"[SpawnController] Target reached — waiting {remaining:F1}s for minDuration.");
                StartCoroutine(DelayedDrain(remaining));
            }
            else
            {
                ExecuteDrain();
            }
        }

        private IEnumerator DelayedDrain(float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteDrain();
        }

        private void ExecuteDrain()
        {
            _levelCompleted = true;

            Debug.Log($"[SpawnController] Drain — killing {_activeBirds.Count} birds, " +
                      $"{_activeEggs.Count} eggs remaining.");

            for (int i = _activeBirds.Count - 1; i >= 0; i--)
            {
                var bird = _activeBirds[i];
                bird.OnLayEgg    -= HandleBirdLayEgg;
                bird.OnDestroyed -= HandleBirdDestroyed;
                Destroy(bird.gameObject);
            }
            _activeBirds.Clear();

            if (_activeEggs.Count == 0)
            {
                Debug.Log("[SpawnController] No eggs — firing AllEggsCleared immediately.");
                GameEvents.FireAllEggsCleared();
            }
        }

        // ─────────────────────────────────────────────
        // Egg Tier Cap — uses maxE4 / maxE3 / maxE2
        // ─────────────────────────────────────────────
        private bool IsEggTierAllowed(EggTierConfig tier)
        {
            if (tier == null) return false;
            int current = _eggTierCounts.GetValueOrDefault(tier.tierId, 0);
            int cap = tier.tierId switch
            {
                "E4" => levelProfile.maxE4,
                "E3" => levelProfile.maxE3,
                "E2" => levelProfile.maxE2,
                _    => int.MaxValue    // E1 always allowed
            };
            return current < cap;
        }

        private void TrackEggAdded(EggTierConfig tier)
        {
            if (tier == null) return;
            _eggTierCounts[tier.tierId] = _eggTierCounts.GetValueOrDefault(tier.tierId, 0) + 1;
        }

        private void TrackEggRemoved(EggTierConfig tier)
        {
            if (tier == null) return;
            _eggTierCounts[tier.tierId] = Mathf.Max(0, _eggTierCounts.GetValueOrDefault(tier.tierId, 0) - 1);
        }

        // ─────────────────────────────────────────────
        // Performance
        // ─────────────────────────────────────────────
        void UpdatePerformance()
        {
            float t = Mathf.Clamp01(elapsedTime / levelProfile.maxDuration);
            _performanceExpectedScore = levelProfile.targetScore * t;

            float expected   = Mathf.Max(1f, _performanceExpectedScore);
            int currentScore = Managers.ScoreManager.Instance != null
                ? Managers.ScoreManager.Instance.CurrentScore : 0;
            _performanceRatio = currentScore / expected;
        }

        // ─────────────────────────────────────────────
        // Relief — uses pressureAvg as steady-state target
        // instead of raw pressureMax, giving more accurate
        // breathing room tuned to each level's design
        // ─────────────────────────────────────────────
        void UpdateReliefMode()
        {
            int steadyPressure = Mathf.Min(levelProfile.pressureAvg, GetCurrentPressureMax());

            if (!_reliefMode && _pressureTracker.CurrentPressure > steadyPressure * adaptiveConfig.reliefEnterRatio)
                _reliefMode = true;
            else if (_reliefMode && _pressureTracker.CurrentPressure < steadyPressure * adaptiveConfig.reliefExitRatio)
                _reliefMode = false;
        }

        float GetCurrentSpawnInterval()
        {
            float baseInterval = Random.Range(levelProfile.spawnIntervalMin, levelProfile.spawnIntervalMax);
            float multiplier   = 1f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
                multiplier = Random.Range(adaptiveConfig.spawnIntervalMultiplierHigh.x, adaptiveConfig.spawnIntervalMultiplierHigh.y);
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
                multiplier = Random.Range(adaptiveConfig.spawnIntervalMultiplierLow.x, adaptiveConfig.spawnIntervalMultiplierLow.y);

            return Mathf.Max(0.3f, baseInterval * multiplier);
        }

        int GetCurrentPressureMax()
        {
            float baseMax = levelProfile.pressureMax;
            float mult    = 1f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
                mult = Random.Range(adaptiveConfig.pressureMaxMultiplierHigh.x, adaptiveConfig.pressureMaxMultiplierHigh.y);
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
                mult = Random.Range(adaptiveConfig.pressureMaxMultiplierLow.x, adaptiveConfig.pressureMaxMultiplierLow.y);

            return Mathf.RoundToInt(baseMax * mult);
        }

        // ─────────────────────────────────────────────
        // Bird Spawning
        // ─────────────────────────────────────────────
        void TrySpawnBird()
        {
            int maxPressure = GetCurrentPressureMax();
            if (_pressureTracker.CurrentPressure >= maxPressure) return;

            BirdConfig chosen = SelectBirdType();
            if (chosen == null) return;

            int lifetimePressure = EstimateLifetimePressure(chosen);
            if (_pressureTracker.CurrentPressure + lifetimePressure > maxPressure)
            {
                var cheap = GetCheaperBird(chosen);
                if (cheap == null) return;
                chosen = cheap;
            }

            SpawnBirdInstance(chosen);
        }

        BirdConfig GetCheaperBird(BirdConfig current)
        {
            if (current.birdId == "B4") return FindBird("B3");
            if (current.birdId == "B3") return FindBird("B2");
            if (current.birdId == "B2") return FindBird("B1");
            return null;
        }

        BirdConfig FindBird(string id)
        {
            foreach (var b in birds)
                if (b != null && b.birdId == id) return b;
            return null;
        }

        BirdConfig SelectBirdType()
        {
            float b1 = 0.50f, b2 = 0.30f, b3 = 0.15f, b4 = 0.05f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
            {
                b3 += adaptiveConfig.b3WeightShiftHigh;
                b4 += adaptiveConfig.b4WeightShiftHigh;
                float delta = adaptiveConfig.b3WeightShiftHigh + adaptiveConfig.b4WeightShiftHigh;
                b1 -= delta * 0.7f;
                b2 -= delta * 0.3f;
            }
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
            {
                b1 += adaptiveConfig.b1WeightShiftLow;
                b2 += adaptiveConfig.b2WeightShiftLow;
                if (adaptiveConfig.blockB4WhenLow) b4 = 0f;
                float total = b1 + b2 + b3 + b4;
                b1 /= total; b2 /= total; b3 /= total; b4 /= total;
            }

            float roll = Random.value;
            if (roll < b1)           return FindBird("B1");
            if (roll < b1 + b2)      return FindBird("B2");
            if (roll < b1 + b2 + b3) return FindBird("B3");
            return FindBird("B4");
        }

        int EstimateLifetimePressure(BirdConfig bird)
        {
            EggTierConfig tier = bird.eggTier;
            if (tier == null) return 0;

            int total = 0, count = 1;
            EggTierConfig t = tier;
            while (t != null)
            {
                total += count * t.pressureValue;
                count *= t.splitCount;
                t      = t.splitInto;
            }

            int expectedEggs = Mathf.Max(1, Mathf.RoundToInt(bird.lifetime / bird.layIntervalMin));
            return total * expectedEggs;
        }

        void SpawnBirdInstance(BirdConfig birdConfig)
        {
            if (birdConfig.birdPrefab == null)
            {
                Debug.LogError($"[SpawnController] No birdPrefab in BirdConfig '{birdConfig.birdId}'.");
                return;
            }

            Transform spawnPoint = birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)];
            var go   = Instantiate(birdConfig.birdPrefab, spawnPoint.position, Quaternion.identity);
            var bird = go.GetComponent<BaseBird>();

            int hp = Mathf.RoundToInt(birdConfig.baseHp * levelProfile.hpMultiplier);
            bird.Init(birdConfig, hp);

            bird.OnLayEgg    += HandleBirdLayEgg;
            bird.OnDestroyed += HandleBirdDestroyed;

            _activeBirds.Add(bird);
        }

        // ─────────────────────────────────────────────
        // Egg Lifecycle
        // ─────────────────────────────────────────────
        void HandleBirdLayEgg(BaseBird bird)
        {
            if (_draining) return;

            EggTierConfig tier = bird.config.eggTier;
            if (tier == null || tier.eggPrefab == null)
            {
                Debug.LogError($"[SpawnController] No eggPrefab in EggTierConfig for bird '{bird.config.birdId}'.");
                return;
            }

            // Respect per-tier cap from LevelProfile
            if (!IsEggTierAllowed(tier))
            {
                Debug.Log($"[SpawnController] Egg tier {tier.tierId} at cap — skipping lay.");
                return;
            }

            var go  = Instantiate(tier.eggPrefab, bird.transform.position, Quaternion.identity);
            var egg = go.GetComponent<Egg>();

            int baseHp = Random.Range(tier.baseHpMin, tier.baseHpMax + 1);
            int hp     = Mathf.RoundToInt(baseHp * levelProfile.hpMultiplier);

            var rb = egg.GetComponent<Rigidbody2D>();
            rb.AddForce(new Vector2(-2f, 0f), ForceMode2D.Impulse);

            egg.Init(tier, hp);
            egg.OnDestroyed += HandleEggDestroyed;

            _activeEggs.Add(egg);
            _pressureTracker.RegisterEgg(egg);
            TrackEggAdded(tier);
            _totalTrackedScore += CalculateEggMaxScore(tier);
        }

        void HandleBirdDestroyed(BaseBird bird)
        {
            bird.OnLayEgg    -= HandleBirdLayEgg;
            bird.OnDestroyed -= HandleBirdDestroyed;
            _activeBirds.Remove(bird);
            Destroy(bird.gameObject);
        }

        void HandleEggDestroyed(Egg egg)
        {
            egg.OnDestroyed -= HandleEggDestroyed;
            _activeEggs.Remove(egg);
            TrackEggRemoved(egg.config);

            Debug.Log($"[SpawnController] Egg destroyed tier={egg.config?.tierId}, " +
                      $"splitInto={egg.config?.splitInto?.tierId}, remaining={_activeEggs.Count}");

            // Spawn splits — also respect tier caps
            if (egg.config?.splitInto != null)
            {
                EggTierConfig splitTier = egg.config.splitInto;

                if (splitTier.eggPrefab == null)
                {
                    Debug.LogError($"[SpawnController] No eggPrefab in EggTierConfig '{splitTier.tierId}'.");
                }
                else
                {
                    for (int i = 0; i < egg.config.splitCount; i++)
                    {
                        if (!IsEggTierAllowed(splitTier))
                        {
                            Debug.Log($"[SpawnController] Split tier {splitTier.tierId} at cap — skipping.");
                            continue;
                        }

                        Vector3 offset = egg.config.GetSplitImpulse(i);
                        
                        var go     = Instantiate(splitTier.eggPrefab);
                        
                        go.TryGetComponent(out Rigidbody2D rb);
                        
                        rb.AddForce(offset, ForceMode2D.Impulse);
                        
                        var newEgg = go.GetComponent<Egg>();

                        int baseHp = Random.Range(splitTier.baseHpMin, splitTier.baseHpMax + 1);
                        int hp     = Mathf.RoundToInt(baseHp * levelProfile.hpMultiplier);
                        
                        newEgg.Init(splitTier, hp);
                        newEgg.OnDestroyed += HandleEggDestroyed;

                        _activeEggs.Add(newEgg);
                        _pressureTracker.RegisterEgg(newEgg);
                        TrackEggAdded(splitTier);
                    }
                }
            }

            Destroy(egg.gameObject);

            // Check AFTER splits added — covers E1 (no split) too
            if (_activeEggs.Count == 0)
            {
                Debug.Log("[SpawnController] All eggs cleared!");
                GameEvents.FireAllEggsCleared();
            }
        }

        // ─────────────────────────────────────────────
        // Score estimate — avg HP × scorePerHit + scoreOnDestroy + splits
        // ─────────────────────────────────────────────
        private int CalculateEggMaxScore(EggTierConfig tier)
        {
            if (tier == null) return 0;
            int avgHp = (tier.baseHpMin + tier.baseHpMax) / 2;
            int score = (tier.scorePerHit * avgHp) + tier.scoreOnDestroy;
            if (tier.splitInto != null)
                score += tier.splitCount * CalculateEggMaxScore(tier.splitInto);
            return score;
        }
    }
}
