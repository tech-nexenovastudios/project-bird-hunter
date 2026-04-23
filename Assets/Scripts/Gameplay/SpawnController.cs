using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Levels;
using Gameplay.Events;
using Gameplay.Managers;

namespace Gameplay
{
    public class SpawnController : MonoBehaviour
    {
        public static SpawnController Instance;

        [Header("Config")]
        public LevelProfile levelProfile;
        public AdaptiveDifficultyConfig adaptiveConfig;
        public BirdConfig[] birds;
        public Transform[] birdSpawnPoints;
        public Transform bossSpawnPoint;

        [Header("Boss Configs (index = chapter - 1)")]
        public BossBirdConfig[] chapterBossConfigs;

        [Header("Boss Prefabs (index = chapter - 1)")]
        public GameObject[] bossPrefabs;

        [Header("State (read-only)")]
        public float elapsedTime;

        PressureTracker _pressureTracker;
        readonly List<BaseBird> _activeBirds = new();
        readonly List<Egg> _activeEggs = new();
        readonly List<AttackingBird> _activeAttackingBirds = new();
        readonly Dictionary<string, int> _eggTierCounts = new();

        float _spawnTimer;
        bool _reliefMode;
        bool _levelCompleted;
        bool _draining;
        int _totalTrackedScore;

        float _performanceExpectedScore;
        float _performanceRatio = 1f;

        private int _sortingIndex = 10;

        GameObject _activeBossGO;
        BossBirdController _activeBossController;
        bool _bossSpawned;
        bool _bossDefeated;
        float _bossSpawnTimer;
        bool _bossTimerRunning;

        float _attackingBirdTimer;
        Coroutine _drainingRoutine;

        // ── Cached original scale for boss enter/exit animations ──
        Vector3 _bossOriginalScale;

        // ── Persisted boss state for level-20 re-spawn ──
        GameObject _parkedBossGO;
        float _parkedBossHpNormalized;
        bool _hasBossWaitingForLevel20;

        public bool IsBossLevel => levelProfile.isBossLevel;
        public int TotalTrackedScore => _totalTrackedScore;
        public int ActiveEggCount => _activeEggs.Count;
        public int ActiveBirdCount => _activeBirds.Count;

        public bool IsBossAlive => _activeBossController != null && !_activeBossController.IsDead;

        void Awake()
        {
            Instance = this;
            _pressureTracker = new PressureTracker();
        }

        void OnEnable()
        {
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        void OnDisable()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossDefeated -= HandleBossDefeated;
                BossEventBus.Instance.OnBossRetreated -= HandleBossRetreated;
            }
        }


        void HandleLevelCompleted(int finalScore) => _levelCompleted = true;

        void Start() => ResetLevel();

        public void ResetLevel()
        {
            elapsedTime = 0f;
            _spawnTimer = 0f;
            _reliefMode = false;
            _levelCompleted = false;
            _draining = false;
            _totalTrackedScore = 0;
            _performanceExpectedScore = 0f;
            _performanceRatio = 1f;
            _sortingIndex = 10;
            _eggTierCounts.Clear();

            if (_drainingRoutine != null)
            {
                StopCoroutine(_drainingRoutine);
                _drainingRoutine = null;
            }

            ClearRegularEnemies();
            _pressureTracker.Clear();

            _activeBossGO = null;
            _activeBossController = null;
            _bossSpawned = false;
            _bossDefeated = false;
            _bossTimerRunning = false;
            _bossSpawnTimer = 0f;

            // NOTE: _parkedBossGO / _hasBossWaitingForLevel20 intentionally
            // persist across level resets so the boss survives until level 20.

            _attackingBirdTimer = levelProfile != null
                ? levelProfile.attackingBirdSpawnInterval
                : 20f;

            if (levelProfile != null)
                ValidateProfile();

            if (levelProfile != null && levelProfile.isBossLevel)
            {
                _bossSpawnTimer = levelProfile.bossSpawnDelay;
                _bossTimerRunning = true;

                bool isLevel20 = levelProfile.globalLevel % 20 == 0;
                if (isLevel20 && _hasBossWaitingForLevel20)
                    Debug.Log($"[SpawnController] Level 20 boss level — re-spawning parked boss in {_bossSpawnTimer:F1}s.");
                else
                    Debug.Log($"[SpawnController] Boss level — spawning boss in {_bossSpawnTimer:F1}s.");
            }
        }

        void Update()
        {
            if (Managers.GameManager.Instance.state != GameState.Gameplay) return;

            elapsedTime += Time.deltaTime;
            UpdatePerformance();
            UpdateReliefMode();

            _spawnTimer += Time.deltaTime;
            float interval = GetCurrentSpawnInterval();

            if (!IsBossLevel && !_reliefMode && !_levelCompleted && !_draining && _spawnTimer >= interval)
            {
                TrySpawnBird();
                _spawnTimer = 0f;
            }

            if (_bossTimerRunning && !_bossSpawned && !_draining && !_levelCompleted)
            {
                _bossSpawnTimer -= Time.deltaTime;
                if (_bossSpawnTimer <= 0f)
                {
                    _bossTimerRunning = false;
                    TrySpawnBoss();
                }
            }

            if (levelProfile != null
                && levelProfile.attackingBirdPool != null
                && levelProfile.attackingBirdPool.Length > 0
                && levelProfile.attackingBirdSpawnInterval > 0f
                && !_levelCompleted && !_draining && !IsBossAlive)
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
                Debug.LogWarning($"[SpawnController] Level {levelProfile.globalLevel}: targetScore unreachable!");
        }

        // ═══════════════════════════════════════════════════════════════
        //  BOSS SPAWN / ENTER / EXIT / RETREAT
        // ═══════════════════════════════════════════════════════════════

        private void TrySpawnBoss()
        {
            if (_bossSpawned) return;

            // ── Subscribe here, guaranteed BossEventBus exists by spawn time ──
            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossDefeated -= HandleBossDefeated;   // unsub first to avoid doubles
                BossEventBus.Instance.OnBossRetreated -= HandleBossRetreated;
                BossEventBus.Instance.OnBossDefeated += HandleBossDefeated;
                BossEventBus.Instance.OnBossRetreated += HandleBossRetreated;
            }
            else
            {
                Debug.LogError("[SpawnController] BossEventBus.Instance is null at spawn time — retreat/defeat won't fire!");
                return;
            }

            ClearRegularEnemies();

            bool isLevel20 = levelProfile.globalLevel % 20 == 0;

            // ── Level 20 re-spawn of a parked (retreated) boss ──
            if (isLevel20 && _hasBossWaitingForLevel20 && _parkedBossGO != null)
            {
                RespawnParkedBoss();
                return;
            }

            // ── Fresh boss spawn ──
            BossBirdConfig cfg = levelProfile.bossBirdConfig;
            if (cfg == null && chapterBossConfigs != null)
            {
                int idx = levelProfile.chapter - 1;
                if (idx >= 0 && idx < chapterBossConfigs.Length)
                    cfg = chapterBossConfigs[idx];
            }

            if (cfg == null || bossPrefabs == null || bossPrefabs.Length == 0)
            {
                _bossSpawned = true;
                return;
            }

            int prefabIdx = levelProfile.chapter - 1;
            if (prefabIdx < 0 || prefabIdx >= bossPrefabs.Length || bossPrefabs[prefabIdx] == null)
            {
                _bossSpawned = true;
                return;
            }

            // Spawn off-screen at top-right corner
            Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 1.2f, 0f));
            topRight.z = 0f;

            var go = Instantiate(bossPrefabs[prefabIdx], topRight, Quaternion.identity);
            var controller = go.GetComponent<BossBirdController>();

            controller.Initialize(cfg, isLevel20);

            // Invulnerable during entrance tween
            controller.SetInvulnerable(true);

            // Cache original scale, start at 70%
            _bossOriginalScale = go.transform.localScale;
            go.transform.localScale = _bossOriginalScale * 0.7f;

            Vector3 targetPos = bossSpawnPoint.position;
            go.transform.DOMove(targetPos, 3f).SetEase(Ease.OutCubic);
            go.transform.DOScale(_bossOriginalScale, 3f).SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    if (controller != null)
                        controller.SetInvulnerable(false);
                });

            _activeBossGO = go;
            _activeBossController = controller;
            _bossSpawned = true;
        }

        /// <summary>
        /// Re-spawns a boss that retreated at 50% HP.
        /// The parked GO is re-activated, re-initialized for phase 2,
        /// and animated in from the top-right corner.
        /// </summary>
        private void RespawnParkedBoss()
        {
            GameObject go = _parkedBossGO;
            float remainingHp = _parkedBossHpNormalized;

            _parkedBossGO = null;
            _hasBossWaitingForLevel20 = false;

            go.SetActive(true);

            var controller = go.GetComponent<BossBirdController>();
            controller.ReinitializeForPhase2(remainingHp);

            // Invulnerable during entrance tween
            controller.SetInvulnerable(true);

            // Position at top-right corner off-screen
            Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 1.2f, 0f));
            topRight.z = 0f;
            go.transform.position = topRight;

            _bossOriginalScale = go.transform.localScale;
            go.transform.localScale = _bossOriginalScale * 0.7f;

            Vector3 targetPos = bossSpawnPoint.position;
            go.transform.DOMove(targetPos, 3f).SetEase(Ease.OutCubic);
            go.transform.DOScale(_bossOriginalScale, 3f).SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    if (controller != null)
                        controller.SetInvulnerable(false);
                });

            _activeBossGO = go;
            _activeBossController = controller;
            _bossSpawned = true;

            Debug.Log($"[SpawnController] Parked boss re-spawned for Level 20 with {remainingHp:P0} HP.");
        }

        /// <summary>
        /// Animates the boss exiting toward the bottom-left corner while
        /// scaling down by 30%. Does NOT destroy — caller decides.
        /// </summary>
        private void AnimateBossExit(GameObject bossGO, System.Action onComplete = null)
        {
            if (bossGO == null)
            {
                onComplete?.Invoke();
                return;
            }

            var controller = bossGO.GetComponent<BossBirdController>();
            if (controller != null)
                controller.SetInvulnerable(true);

            Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(-0.2f, -0.2f, 0f));
            bottomLeft.z = 0f;

            Vector3 shrunkScale = bossGO.transform.localScale * 0.7f;

            bossGO.transform.DOMove(bottomLeft, 3f).SetEase(Ease.InCubic);
            bossGO.transform.DOScale(shrunkScale, 3f).SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
        }

        // ═══════════════════════════════════════════════════════════════
        //  BOSS EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Boss reached 50% HP on a non-level-20 encounter.
        /// Animate exit, then park (hide) the boss for re-use at level 20.
        /// </summary>
        private void HandleBossRetreated(string bossName, float hpNormalized)
        {
            if (_activeBossGO == null) return;

            GameObject bossGO = _activeBossGO;
            _activeBossController = null;
            _activeBossGO = null;

            AnimateBossExit(bossGO, () =>
            {
                // Don't destroy — park it for level 20
                bossGO.SetActive(false);
                DontDestroyOnLoad(bossGO);

                _parkedBossGO = bossGO;
                _parkedBossHpNormalized = hpNormalized;
                _hasBossWaitingForLevel20 = true;
                GameProgressManager.Instance.CompleteLevel(100);

                Debug.Log($"[SpawnController] Boss '{bossName}' parked at {hpNormalized:P0} HP for Level 20.");
            });
        }

        /// <summary>
        /// Boss truly killed (level 20 / phase 2).
        /// Animate exit, then destroy.
        /// </summary>
        private void HandleBossDefeated(string bossName, int score)
        {
            _bossDefeated = true;

            if (_activeBossGO != null)
            {
                GameObject bossGO = _activeBossGO;
                _activeBossController = null;
                _activeBossGO = null;

                AnimateBossExit(bossGO, () =>
                {
                    Destroy(bossGO);
                });
            }
            else
            {
                _activeBossController = null;
                _activeBossGO = null;
            }

            _hasBossWaitingForLevel20 = false;
            _parkedBossGO = null;
        }

        // ═══════════════════════════════════════════════════════════════
        //  CLEAR / DRAIN
        // ═══════════════════════════════════════════════════════════════

        private void ClearRegularEnemies()
        {
            for (int i = _activeBirds.Count - 1; i >= 0; i--)
            {
                var b = _activeBirds[i];
                if (b != null) Destroy(b.gameObject);
            }
            _activeBirds.Clear();

            for (int i = _activeEggs.Count - 1; i >= 0; i--)
            {
                var e = _activeEggs[i];
                if (e != null) Destroy(e.gameObject);
            }
            _activeEggs.Clear();
            _eggTierCounts.Clear();

            for (int i = _activeAttackingBirds.Count - 1; i >= 0; i--)
            {
                var ab = _activeAttackingBirds[i];
                if (ab != null) ab.ForceKill();
            }
            _activeAttackingBirds.Clear();
        }

        public void StartDrain(int scoreAtTrigger)
        {
            if (_draining) return;
            _draining = true;

            float remaining = levelProfile.minDuration - elapsedTime;
            if (remaining > 0f)
            {
                if (_drainingRoutine == null) _drainingRoutine = StartCoroutine(DelayedDrain(remaining));
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
            _bossTimerRunning = false;
            ClearRegularEnemies();

            if (_activeBossGO != null)
            {
                if (BossEventBus.Instance != null)
                {
                    BossEventBus.Instance.OnBossDefeated -= HandleBossDefeated;
                    BossEventBus.Instance.OnBossRetreated -= HandleBossRetreated;
                }

                GameObject bossGO = _activeBossGO;
                _activeBossGO = null;
                _activeBossController = null;

                AnimateBossExit(bossGO, () =>
                {
                    Destroy(bossGO);

                    if (BossEventBus.Instance != null)
                    {
                        BossEventBus.Instance.OnBossDefeated += HandleBossDefeated;
                        BossEventBus.Instance.OnBossRetreated += HandleBossRetreated;
                    }
                });
            }

            if (_activeEggs.Count == 0)
            {
                GameEvents.FireAllEggsCleared();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  EGG TIER TRACKING
        // ═══════════════════════════════════════════════════════════════

        private bool IsEggTierAllowed(EggTierConfig tier)
        {
            if (tier == null) return false;
            int current = _eggTierCounts.GetValueOrDefault(tier.tierId, 0);
            int cap = tier.tierId switch
            {
                "E4" => levelProfile.maxE4,
                "E3" => levelProfile.maxE3,
                "E2" => levelProfile.maxE2,
                _ => int.MaxValue
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

        // ═══════════════════════════════════════════════════════════════
        //  PERFORMANCE / RELIEF
        // ═══════════════════════════════════════════════════════════════

        void UpdatePerformance()
        {
            float t = Mathf.Clamp01(elapsedTime / levelProfile.maxDuration);
            _performanceExpectedScore = levelProfile.targetScore * t;

            float expected = Mathf.Max(1f, _performanceExpectedScore);
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
            float multiplier = 1f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
                multiplier = Random.Range(adaptiveConfig.spawnIntervalMultiplierHigh.x, adaptiveConfig.spawnIntervalMultiplierHigh.y);
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
                multiplier = Random.Range(adaptiveConfig.spawnIntervalMultiplierLow.x, adaptiveConfig.spawnIntervalMultiplierLow.y);

            return Mathf.Max(0.3f, baseInterval * multiplier);
        }

        int GetCurrentPressureMax()
        {
            float baseMax = levelProfile.pressureMax;
            float mult = 1f;

            if (_performanceRatio > adaptiveConfig.highPerformanceThreshold)
                mult = Random.Range(adaptiveConfig.pressureMaxMultiplierHigh.x, adaptiveConfig.pressureMaxMultiplierHigh.y);
            else if (_performanceRatio < adaptiveConfig.lowPerformanceThreshold)
                mult = Random.Range(adaptiveConfig.pressureMaxMultiplierLow.x, adaptiveConfig.pressureMaxMultiplierLow.y);

            return Mathf.RoundToInt(baseMax * mult);
        }

        // ═══════════════════════════════════════════════════════════════
        //  BIRD SPAWNING
        // ═══════════════════════════════════════════════════════════════

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
            if (roll < b1) return FindBird("B1");
            if (roll < b1 + b2) return FindBird("B2");
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
                t = t.splitInto;
            }

            int expectedEggs = Mathf.Max(1, Mathf.RoundToInt(bird.lifetime / bird.layIntervalMin));
            return total * expectedEggs;
        }

        void SpawnBirdInstance(BirdConfig birdConfig)
        {
            if (birdConfig.birdPrefab == null) return;

            Transform spawnPoint = birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)];
            var go = Instantiate(birdConfig.birdPrefab, spawnPoint.position, Quaternion.identity);
            var bird = go.GetComponent<BaseBird>();

            int hp = Mathf.RoundToInt(birdConfig.baseHp * levelProfile.hpMultiplier);
            bird.Init(birdConfig, hp);

            bird.OnLayEgg += HandleBirdLayEgg;
            bird.OnDestroyed += HandleBirdDestroyed;

            _activeBirds.Add(bird);
        }

        #region Attacking Birds

        private void TrySpawnAttackingBird()
        {
            if (levelProfile.attackingBirdPool == null || levelProfile.attackingBirdPool.Length == 0) return;
            if (Random.value > levelProfile.attackingBirdSpawnChance) return;

            var config = PickAttackingBirdConfig();
            if (config == null || config.prefab == null) return;

            Transform spawnPoint = birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)];
            var go = Instantiate(config.prefab, spawnPoint.position, Quaternion.identity);

            var attackingBird = go.GetComponent<AttackingBird>();
            if (attackingBird == null)
            {
                Destroy(go);
                return;
            }

            attackingBird.Init(config, levelProfile.hpMultiplier);
            attackingBird.OnDestroyed += HandleAttackingBirdDestroyed;
            _activeAttackingBirds.Add(attackingBird);
        }

        private AttackingBirdConfig PickAttackingBirdConfig()
        {
            var pool = levelProfile.attackingBirdPool;
            float totalWeight = 0f;
            foreach (var c in pool) totalWeight += c != null ? c.spawnWeight : 0f;
            if (totalWeight <= 0f) return pool[Random.Range(0, pool.Length)];

            float roll = Random.Range(0f, totalWeight);
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

        // ═══════════════════════════════════════════════════════════════
        //  EGG HANDLING
        // ═══════════════════════════════════════════════════════════════

        void HandleBirdLayEgg(BaseBird bird)
        {
            if (_draining || IsBossAlive) return;

            EggTierConfig tier = bird.config.eggTier;
            if (tier == null || tier.eggPrefab == null) return;
            if (!IsEggTierAllowed(tier)) return;

            var go = Instantiate(tier.eggPrefab, bird.transform.position, Quaternion.identity);
            var egg = go.GetComponent<Egg>();

            int baseHp = Random.Range(tier.baseHpMin, tier.baseHpMax + 1);
            int hp = Mathf.RoundToInt(baseHp * levelProfile.hpMultiplier);

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
            if (egg == null || IsBossAlive) return;

            if (egg.config?.splitInto != null)
            {
                EggTierConfig splitTier = egg.config.splitInto;
                if (splitTier.eggPrefab != null)
                {
                    for (int i = 0; i < egg.config.splitCount; i++)
                    {
                        if (!IsEggTierAllowed(splitTier)) continue;

                        var offset = i == 0 ? new Vector3(-.5f, 0.5f, 0f) : new Vector3(.5f, 0.5f, 0f);
                        var go = Instantiate(splitTier.eggPrefab, egg.transform.position, Quaternion.identity);
                        go.transform.DOJump(egg.transform.position + offset, 0.5f, 1, 0.5f).SetEase(Ease.OutBack);

                        var newEgg = go.GetComponent<Egg>();
                        int baseHp = Random.Range(splitTier.baseHpMin, splitTier.baseHpMax + 1);
                        int hp = Mathf.RoundToInt(baseHp * levelProfile.hpMultiplier);

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

        void HandleBirdDestroyed(BaseBird bird)
        {
            bird.OnLayEgg -= HandleBirdLayEgg;
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
            Destroy(egg.gameObject);

            if (_activeEggs.Count == 0)
            {
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