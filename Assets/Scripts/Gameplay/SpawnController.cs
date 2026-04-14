using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Levels;
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

        PressureTracker              _pressureTracker;
        readonly List<BaseBird>      _activeBirds          = new();
        readonly List<Egg>           _activeEggs           = new();
        readonly List<AttackingBird> _activeAttackingBirds = new();
        readonly Dictionary<string, int> _eggTierCounts    = new();

        float _spawnTimer;
        bool  _reliefMode;
        bool  _levelCompleted;
        bool  _draining;
        int   _totalTrackedScore;

        float _performanceExpectedScore;
        float _performanceRatio = 1f;

        private int _sortingIndex = 10;

        BossBird _activeBoss;
        bool     _bossSpawned;
        bool     _bossDefeated;

        float _attackingBirdTimer;

        public int  TotalTrackedScore => _totalTrackedScore;
        public int  ActiveEggCount    => _activeEggs.Count;
        public int  ActiveBirdCount   => _activeBirds.Count;
        public bool IsBossAlive       => _activeBoss != null && !_activeBoss.IsDefeated;

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

            _activeBoss   = null;
            _bossSpawned  = false;
            _bossDefeated = false;

            _attackingBirdTimer = levelProfile != null
                ? levelProfile.attackingBirdSpawnInterval
                : 20f;

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

            if (levelProfile != null && levelProfile.isBossLevel && !_bossSpawned)
                TrySpawnBoss();

            if (levelProfile != null
                && levelProfile.attackingBirdPool != null
                && levelProfile.attackingBirdPool.Length > 0
                && levelProfile.attackingBirdSpawnInterval > 0f
                && !_levelCompleted && !_draining)
            {
                _attackingBirdTimer -= Time.deltaTime;
                if (_attackingBirdTimer <= 0f)
                {
                    TrySpawnAttackingBird();
                    _attackingBirdTimer = levelProfile.attackingBirdSpawnInterval;
                }
            }
        }

        private void ValidateProfile()
        {
            if (levelProfile.expectedDps <= 0) return;
            float estimatedTime = levelProfile.targetScore / (float)levelProfile.expectedDps;
            if (estimatedTime > levelProfile.maxDuration)
                Debug.LogWarning(
                    $"[SpawnController] Level {levelProfile.globalLevel}: targetScore unreachable! " +
                    $"Needs {estimatedTime:F0}s but maxDuration={levelProfile.maxDuration}s");
        }

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
                      $"{_activeEggs.Count} eggs, {_activeAttackingBirds.Count} attacking birds remaining.");

            for (int i = _activeBirds.Count - 1; i >= 0; i--)
            {
                var bird = _activeBirds[i];
                bird.OnLayEgg    -= HandleBirdLayEgg;
                bird.OnDestroyed -= HandleBirdDestroyed;
                Destroy(bird.gameObject);
            }
            _activeBirds.Clear();

            for (int i = _activeAttackingBirds.Count - 1; i >= 0; i--)
            {
                var ab = _activeAttackingBirds[i];
                ab.OnDestroyed -= HandleAttackingBirdDestroyed;
                ab.ForceKill();
            }
            _activeAttackingBirds.Clear();

            if (_activeBoss != null)
            {
                _activeBoss.OnDestroyed -= HandleBossDestroyed;
                _activeBoss.ForceKill();
                _activeBoss = null;
            }

            if (_activeEggs.Count == 0)
            {
                Debug.Log("[SpawnController] No eggs — firing AllEggsCleared immediately.");
                GameEvents.FireAllEggsCleared();
            }
        }

        private bool IsEggTierAllowed(EggTierConfig tier)
        {
            if (tier == null) return false;
            int current = _eggTierCounts.GetValueOrDefault(tier.tierId, 0);
            int cap = tier.tierId switch
            {
                "E4" => levelProfile.maxE4,
                "E3" => levelProfile.maxE3,
                "E2" => levelProfile.maxE2,
                _    => int.MaxValue
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

        void UpdatePerformance()
        {
            float t = Mathf.Clamp01(elapsedTime / levelProfile.maxDuration);
            _performanceExpectedScore = levelProfile.targetScore * t;

            float expected   = Mathf.Max(1f, _performanceExpectedScore);
            int currentScore = Managers.ScoreManager.Instance != null
                ? Managers.ScoreManager.Instance.CurrentScore : 0;
            _performanceRatio = currentScore / expected;
        }

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

        private void TrySpawnBoss()
        {
            if (_bossSpawned) return;
            if (levelProfile.bossConfig == null)
            {
                Debug.LogWarning("[SpawnController] isBossLevel=true but no BossConfig assigned!");
                _bossSpawned = true;
                return;
            }

            _bossSpawned = true;

            var bossConfig = levelProfile.bossConfig;
            if (bossConfig.bossPrefab == null)
            {
                Debug.LogError($"[SpawnController] BossConfig '{bossConfig.bossId}' has no prefab.");
                return;
            }

            Vector3 spawnPos = bossConfig.fixedSpawnPoint != null
                ? bossConfig.fixedSpawnPoint.position
                : birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)].position;

            var go   = Instantiate(bossConfig.bossPrefab, spawnPos, Quaternion.identity);
            var boss = go.GetComponent<BossBird>();

            if (boss == null)
            {
                Debug.LogError($"[SpawnController] Boss prefab '{bossConfig.bossPrefab.name}' missing BossBird component.");
                Destroy(go);
                return;
            }

            boss.InitBoss(bossConfig);
            boss.OnLayEgg    += HandleBossLayEgg;
            boss.OnBurstLay  += HandleBossBurstLay;
            boss.OnDestroyed += HandleBossDestroyed;

            _activeBoss = boss;
            Debug.Log($"[SpawnController] Boss '{bossConfig.bossId}' spawned at {spawnPos} (L{levelProfile.globalLevel})");
        }

        private void HandleBossLayEgg(BaseBird bird)
        {
            if (_activeBoss == null) return;
            SpawnEggFromPosition(_activeBoss.transform.position, _activeBoss.GetCurrentEggTier());
        }

        private void HandleBossBurstLay(BossBird boss, int count)
        {
            if (boss == null) return;
            var tier = boss.GetCurrentEggTier();
            for (int i = 0; i < count; i++)
            {
                float xOffset = Random.Range(-1.5f, 1.5f);
                SpawnEggFromPosition(boss.transform.position + new Vector3(xOffset, 0f, 0f), tier);
            }
            Debug.Log($"[SpawnController] Boss burst — spawned {count} eggs.");
        }

        private void HandleBossDestroyed(BaseBird bird)
        {
            if (_activeBoss != null)
            {
                _activeBoss.OnLayEgg    -= HandleBossLayEgg;
                _activeBoss.OnBurstLay  -= HandleBossBurstLay;
                _activeBoss.OnDestroyed -= HandleBossDestroyed;
                _bossDefeated = _activeBoss.IsDefeated;
                _activeBoss   = null;
            }
            Destroy(bird.gameObject);
        }

        #region Attacking Birds
        
        private void TrySpawnAttackingBird()
        {
            if (levelProfile.attackingBirdPool == null || levelProfile.attackingBirdPool.Length == 0)
                return;

            if (Random.value > levelProfile.attackingBirdSpawnChance)
                return;

            var config = PickAttackingBirdConfig();
            if (config == null || config.prefab == null)
            {
                Debug.LogWarning("[SpawnController] Selected AttackingBirdConfig has no prefab.");
                return;
            }

            Transform spawnPoint = birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)];
            var go = Instantiate(config.prefab, spawnPoint.position, Quaternion.identity);

            var attackingBird = go.GetComponent<AttackingBird>();
            if (attackingBird == null)
            {
                Debug.LogError($"[SpawnController] Prefab for '{config.attackingBirdId}' missing AttackingBird component.");
                Destroy(go);
                return;
            }

            attackingBird.Init(config, levelProfile.hpMultiplier);
            attackingBird.OnDestroyed += HandleAttackingBirdDestroyed;
            _activeAttackingBirds.Add(attackingBird);

            Debug.Log($"[SpawnController] Attacking bird '{config.attackingBirdId}' ({config.type}) spawned.");
        }

        private AttackingBirdConfig PickAttackingBirdConfig()
        {
            var pool = levelProfile.attackingBirdPool;
            float totalWeight = 0f;
            foreach (var c in pool) totalWeight += c != null ? c.spawnWeight : 0f;
            if (totalWeight <= 0f) return pool[Random.Range(0, pool.Length)];

            float roll       = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var c in pool)
            {
                if (c == null) continue;
                cumulative += c.spawnWeight;
                if (roll <= cumulative) return c;
            }
            return pool[pool.Length - 1];
        }
        
        private void HandleAttackingBirdDestroyed(AttackingBird bird)
        {
            bird.OnDestroyed -= HandleAttackingBirdDestroyed;
            _activeAttackingBirds.Remove(bird);
            Destroy(bird.gameObject);
        }
        
        #endregion
        
        void HandleBirdLayEgg(BaseBird bird)
        {
            if (_draining) return;

            EggTierConfig tier = bird.config.eggTier;
            if (tier == null || tier.eggPrefab == null)
            {
                Debug.LogError($"[SpawnController] No eggPrefab in EggTierConfig for bird '{bird.config.birdId}'.");
                return;
            }

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

            egg.Init(tier, hp, sortingIndex: _sortingIndex++);
            
            egg.OnTrySplit += HandleEggSplit;
            egg.OnDestroyed += HandleEggDestroyed;

            _activeEggs.Add(egg);
            _pressureTracker.RegisterEgg(egg);
            TrackEggAdded(tier);
            _totalTrackedScore += CalculateEggMaxScore(tier);
        }

        private void HandleEggSplit(Egg egg)
        {
            Debug.Log($"[SpawnController] Egg Splitting tier={egg.config?.tierId}, " +
                      $"splitInto={egg.config?.splitInto?.tierId}, remaining={_activeEggs.Count}");
            if (egg == null) return;
            
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

                        var offset = i == 0 ? new Vector3(-.5f, 0.5f, 0f) : new Vector3(.5f, 0.5f, 0f);
                        
                        var isLeft = i % 2 == 0;
                        
                        var go     = Instantiate(splitTier.eggPrefab, egg.transform.position, Quaternion.identity);
                        
                        go.transform.DOJump(egg.transform.position + offset, 0.5f, 1, 0.5f).SetEase(Ease.OutBack);
                        
                        var newEgg = go.GetComponent<Egg>();

                        int baseHp = Random.Range(splitTier.baseHpMin, splitTier.baseHpMax + 1);
                        int hp     = Mathf.RoundToInt(baseHp * levelProfile.hpMultiplier);

                        newEgg.Init(splitTier, hp, _sortingIndex++);
                        
                        newEgg.OnTrySplit += HandleEggSplit;
                        newEgg.OnDestroyed += HandleEggDestroyed;

                        _activeEggs.Add(newEgg);
                        _pressureTracker.RegisterEgg(newEgg);
                        TrackEggAdded(splitTier);
                    }
                }
            }
        }

        private void SpawnEggFromPosition(Vector3 worldPos, EggTierConfig tier)
        {
            if (_draining) return;

            if (tier == null || tier.eggPrefab == null)
            {
                Debug.LogError("[SpawnController] SpawnEggFromPosition: tier or eggPrefab is null.");
                return;
            }

            if (!IsEggTierAllowed(tier))
            {
                Debug.Log($"[SpawnController] Egg tier {tier.tierId} at cap — skipping lay.");
                return;
            }

            var go  = Instantiate(tier.eggPrefab, worldPos, Quaternion.identity);
            var egg = go.GetComponent<Egg>();

            int baseHp = Random.Range(tier.baseHpMin, tier.baseHpMax + 1);
            int hp     = Mathf.RoundToInt(baseHp * levelProfile.hpMultiplier);

            var rb = egg.GetComponent<Rigidbody2D>();
            rb.AddForce(new Vector2(-2f, 0f), ForceMode2D.Impulse);

            egg.Init(tier, hp, sortingIndex: _sortingIndex++);
            egg.OnTrySplit += HandleEggSplit;
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
            egg.OnTrySplit -= HandleEggSplit;
            egg.OnDestroyed -= HandleEggDestroyed;
            _activeEggs.Remove(egg);
            TrackEggRemoved(egg.config);

            Debug.Log($"[SpawnController] Egg destroyed tier={egg.config?.tierId}");

            Destroy(egg.gameObject);

            if (_activeEggs.Count == 0)
            {
                Debug.Log("[SpawnController] All eggs cleared!");
                GameEvents.FireAllEggsCleared();
            }
        }

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