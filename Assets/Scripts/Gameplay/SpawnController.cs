using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Levels;
using UnityEngine;
using System.Collections.Generic;
using Gameplay.Managers;

namespace Gameplay
{
    public class SpawnController : MonoBehaviour
    {
        public static SpawnController Instance;

        [Header("Config")] public LevelProfile levelProfile;
        public AdaptiveDifficultyConfig adaptiveConfig;
        public BirdConfig[] birds; // assign B1–B4
        public Transform[] birdSpawnPoints;

        [Header("State (read-only)")] public float elapsedTime;

        PressureTracker _pressureTracker;
        readonly List<BaseBird> _activeBirds = new();
        readonly List<Gameplay.Eggs.Egg> _activeEggs = new();

        float _spawnTimer;
        bool _reliefMode;

        float _performanceExpectedScore;
        float _performanceRatio = 1f;

        void Awake()
        {
            Instance = this;
            _pressureTracker = new PressureTracker();
        }

        void Start()
        {
            ResetLevel();
        }

        public void ResetLevel()
        {
            elapsedTime = 0f;
            _spawnTimer = 0f;
            _reliefMode = false;
            _performanceExpectedScore = 0f;
            _performanceRatio = 1f;
        }

        void Update()
        {
            if (Managers.GameManager.Instance.state != GameState.Gameplay) return;
            elapsedTime += Time.deltaTime;
            UpdatePerformance();
            UpdateReliefMode();

            _spawnTimer += Time.deltaTime;
            float currentSpawnInterval = GetCurrentSpawnInterval();

            if (!_reliefMode && _spawnTimer >= currentSpawnInterval)
            {
                TrySpawnBird();
                _spawnTimer = 0f;
            }
        }

        void UpdatePerformance()
        {
            float t = Mathf.Clamp01(elapsedTime / levelProfile.maxDuration);
            _performanceExpectedScore = levelProfile.targetScore * t;

            float expected = Mathf.Max(1f, _performanceExpectedScore);
            int currentScore = Managers.ScoreManager.Instance != null ? Managers.ScoreManager.Instance.CurrentScore : 0;
            _performanceRatio = currentScore / expected;
        }

        void UpdateReliefMode()
        {
            int maxPressure = GetCurrentPressureMax();

            if (!_reliefMode && _pressureTracker.CurrentPressure > maxPressure * adaptiveConfig.reliefEnterRatio)
            {
                _reliefMode = true;
            }
            else if (_reliefMode && _pressureTracker.CurrentPressure < maxPressure * adaptiveConfig.reliefExitRatio)
            {
                _reliefMode = false;
            }
        }

        float GetCurrentSpawnInterval()
        {
            float baseMin = levelProfile.spawnIntervalMin;
            float baseMax = levelProfile.spawnIntervalMax;
            float baseInterval = Random.Range(baseMin, baseMax);

            float multiplier = 1f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
            {
                multiplier = Random.Range(
                    adaptiveConfig.spawnIntervalMultiplierHigh.x,
                    adaptiveConfig.spawnIntervalMultiplierHigh.y
                );
            }
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
            {
                multiplier = Random.Range(
                    adaptiveConfig.spawnIntervalMultiplierLow.x,
                    adaptiveConfig.spawnIntervalMultiplierLow.y
                );
            }

            return Mathf.Max(0.3f, baseInterval * multiplier);
        }

        int GetCurrentPressureMax()
        {
            float baseMax = levelProfile.pressureMax;
            float mult = 1f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
            {
                mult = Random.Range(
                    adaptiveConfig.pressureMaxMultiplierHigh.x,
                    adaptiveConfig.pressureMaxMultiplierHigh.y
                );
            }
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
            {
                mult = Random.Range(
                    adaptiveConfig.pressureMaxMultiplierLow.x,
                    adaptiveConfig.pressureMaxMultiplierLow.y
                );
            }

            return Mathf.RoundToInt(baseMax * mult);
        }

        void TrySpawnBird()
        {
            int maxPressure = GetCurrentPressureMax();
            if (_pressureTracker.CurrentPressure >= maxPressure)
                return;

            BirdConfig chosen = SelectBirdType();
            if (chosen == null)
                return;

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
                if (b != null && b.birdId == id)
                    return b;

            return null;
        }

        BirdConfig SelectBirdType()
        {
            float baseB1 = 0.50f;
            float baseB2 = 0.30f;
            float baseB3 = 0.15f;
            float baseB4 = 0.05f;

            float b1 = baseB1, b2 = baseB2, b3 = baseB3, b4 = baseB4;

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
                b1 /= total;
                b2 /= total;
                b3 /= total;
                b4 /= total;
            }

            float roll = Random.value;
            if (roll < b1) return FindBird("B1");
            if (roll < b1 + b2) return FindBird("B2");
            if (roll < b1 + b2 + b3) return FindBird("B3");
            return FindBird("B4");
        }

        int EstimateLifetimePressure(BirdConfig bird)
        {
            EggTierConfig tier = bird.eggTier;
            if (tier == null) return 0;

            int total = 0;
            int count = 1;
            EggTierConfig t = tier;

            while (t != null)
            {
                total += count * t.pressureValue;
                count *= t.splitCount;
                t = t.splitInto;
            }

            int expectedEggs = Mathf.Max(1, Mathf.RoundToInt(bird.lifetime / bird.layIntervalMin));
            return total * expectedEggs;
        }

        void SpawnBirdInstance(BirdConfig birdConfig)
        {
            if (birdConfig.birdPrefab == null)
            {
                Debug.LogError($"[SpawnController] No birdPrefab assigned in BirdConfig '{birdConfig.birdId}'.");
                return;
            }

            Transform spawnPoint = birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)];
            var go = Instantiate(birdConfig.birdPrefab, spawnPoint.position, Quaternion.identity);
            var bird = go.GetComponent<BaseBird>();

            int hp = Mathf.RoundToInt(birdConfig.baseHp * levelProfile.hpMultiplier);
            bird.Init(birdConfig, hp);

            bird.OnLayEgg += HandleBirdLayEgg;
            bird.OnDestroyed += HandleBirdDestroyed;

            _activeBirds.Add(bird);
        }

        void HandleBirdLayEgg(BaseBird bird)
        {
            EggTierConfig tier = bird.config.eggTier;

            if (tier == null || tier.eggPrefab == null)
            {
                Debug.LogError($"[SpawnController] No eggPrefab assigned in EggTierConfig for bird '{bird.config.birdId}'.");
                return;
            }

            Vector3 pos = bird.transform.position;
            var go = Instantiate(tier.eggPrefab, pos, Quaternion.identity);
            var egg = go.GetComponent<Gameplay.Eggs.Egg>();

            int hp = Mathf.RoundToInt(tier.baseHp * levelProfile.hpMultiplier);

            egg.Init(tier, hp);
            egg.OnDestroyed += HandleEggDestroyed;

            _activeEggs.Add(egg);
            _pressureTracker.RegisterEgg(egg);
        }

        void HandleBirdDestroyed(BaseBird bird)
        {
            bird.OnLayEgg -= HandleBirdLayEgg;
            bird.OnDestroyed -= HandleBirdDestroyed;

            _activeBirds.Remove(bird);
            Destroy(bird.gameObject);
        }

        void HandleEggDestroyed(Gameplay.Eggs.Egg egg)
        {
            egg.OnDestroyed -= HandleEggDestroyed;

            _activeEggs.Remove(egg);

            string tierId = egg.config != null ? egg.config.tierId : "null";
            string splitTierId = egg.config != null && egg.config.splitInto != null
                ? egg.config.splitInto.tierId
                : "null";
            int splitCount = egg.config != null ? egg.config.splitCount : 0;

            Debug.Log($"[SpawnController] Egg destroyed tier={tierId}, splitInto={splitTierId}, splitCount={splitCount}");

            if (egg.config != null && egg.config.splitInto != null)
            {
                EggTierConfig splitTier = egg.config.splitInto;

                if (splitTier.eggPrefab == null)
                {
                    Debug.LogError($"[SpawnController] No eggPrefab assigned in EggTierConfig '{splitTier.tierId}'.");
                    Destroy(egg.gameObject);
                    return;
                }

                for (int i = 0; i < egg.config.splitCount; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
                    var go = Instantiate(splitTier.eggPrefab, egg.transform.position + offset, Quaternion.identity);
                    var newEgg = go.GetComponent<Eggs.Egg>();

                    int hp = Mathf.RoundToInt(splitTier.baseHp * levelProfile.hpMultiplier);

                    newEgg.Init(splitTier, hp);
                    newEgg.OnDestroyed += HandleEggDestroyed;

                    _activeEggs.Add(newEgg);
                    _pressureTracker.RegisterEgg(newEgg);
                }
            }

            Destroy(egg.gameObject);
        }
    }
}