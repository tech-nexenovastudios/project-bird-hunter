using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Interfaces;
using Gameplay.Levels;
using Gameplay.Events;
using Gameplay.Managers;

namespace Gameplay
{
    public class SpawnController : MonoBehaviour
    {
        public static SpawnController Instance;

        [Header("Config")]
        [Tooltip("Chapter config used if Configure() isn't called externally. Runtime-set by GameManager.StartGameplay.")]
        public ChapterProgressionConfig chapterConfig;
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

        // ── Per-level resolved values (computed in ResolveChapterLevel) ──
        int _levelIndex;
        int _priorAttempts;
        int _chapter;
        int _globalLevel;
        int _targetScore;
        int _pressureMax;
        float _spawnIntervalMin;
        float _spawnIntervalMax;
        float _hpMultiplier;
        int _maxE4, _maxE3, _maxE2;
        bool _isBossLevel;
        BossBirdConfig _levelBossBirdConfig;
        float _bossSpawnDelay;
        AttackingBirdConfig[] _attackingBirdPool;
        float _attackingBirdSpawnInterval;
        float _attackingBirdSpawnChance;
        float _minDuration;

        // Bird mix + variance state
        float _w1, _w2, _w3, _w4;
        float _perSpawnWeightJitter;
        float _pressureVariancePercent;
        float _pressureNoiseFrequency;
        float _pressureNoiseSeed;

        float _spawnTimer;
        float _emptyScreenTimer;
        bool _levelCompleted;
        bool _draining;
        int _totalTrackedScore;

        // ── Grace time (non-boss levels only) ──
        // After target score + min duration, give the player a fixed window to clear
        // remaining eggs themselves before we force-destroy them.
        const float GraceDuration = 10f;
        bool _inGrace;
        float _graceTimer;

        // True while eggs are being force-destroyed because grace expired. Read by RewardManager
        // to scale per-egg drops down. Stays true for one frame after the destroy call so the
        // synchronous egg.TakeDamage chain sees it.
        bool _isForceDestroying;
        // True when the *current* level's completion came via force-destroy (grace expired).
        // Cleared on ResetLevel. Read by RewardManager.HandleLevelCompleted.
        bool _lastCompletionWasForceDestroy;
        public bool IsForceDestroying => _isForceDestroying;
        public bool LastCompletionWasForceDestroy => _lastCompletionWasForceDestroy;

        // Matches the LevelDetailPopup "Starting in 3...2...1...Go!" intro so spawning waits
        // until the countdown finishes. Includes a small buffer for fade-in / "Go!" beat.
        const float StartupSpawnDelay = 3.6f;

        private int _sortingIndex = 10;

        GameObject _activeBossGO;
        BossBirdController _activeBossController;
        bool _bossSpawned;
        bool _bossDefeated;
        float _bossSpawnTimer;
        bool _bossTimerRunning;

        float _attackingBirdTimer;
        Coroutine _drainingRoutine;

        Vector3 _bossOriginalScale;

        GameObject _parkedBossGO;
        float _parkedBossHpNormalized;
        bool _hasBossWaitingForLevel20;

        public bool IsBossLevel => _isBossLevel;
        public int TotalTrackedScore => _totalTrackedScore;
        public int ActiveEggCount => _activeEggs.Count;
        public int ActiveBirdCount => _activeBirds.Count;
        public int TargetScore => _targetScore;
        public int GlobalLevel => _globalLevel;
        public int CurrentChapter => _chapter;
        public float MinDuration => _minDuration;

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

        // Auto-reset only when an inspector chapterConfig is assigned (direct-scene debug flow).
        // The normal flow is GameManager.StartGameplay → Configure → ResetLevel.
        void Start()
        {
            if (chapterConfig != null) ResetLevel();
        }

        /// <summary>
        /// Configures the controller for a specific chapter + level.
        /// Call before ResetLevel() when starting gameplay.
        /// </summary>
        /// <param name="cfg">Active chapter config.</param>
        /// <param name="levelIndex">0-based index of the level within the chapter.</param>
        /// <param name="priorAttempts">Number of times the player has *previously* attempted this level
        /// in the current session. 0 on the first try, 1 on the first replay, etc. Drives the replay
        /// difficulty bump.</param>
        public void Configure(ChapterProgressionConfig cfg, int levelIndex, int priorAttempts = 0)
        {
            chapterConfig = cfg;
            _levelIndex = levelIndex;
            _priorAttempts = Mathf.Max(0, priorAttempts);
        }

        public void ResetLevel()
        {
            elapsedTime = 0f;
            _levelCompleted = false;
            _draining = false;
            _inGrace = false;
            _graceTimer = 0f;
            _isForceDestroying = false;
            _lastCompletionWasForceDestroy = false;
            _totalTrackedScore = 0;
            _sortingIndex = 10;
            _eggTierCounts.Clear();

            // Spawning is gated by elapsedTime < StartupSpawnDelay so the player gets a clean
            // 3-2-1 countdown. The first bird arrives just after "Go!".
            _spawnTimer = 0f;
            _emptyScreenTimer = 0f;

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

            ResolveChapterLevel();

            _attackingBirdTimer = _attackingBirdSpawnInterval > 0f ? _attackingBirdSpawnInterval : 20f;

            if (_isBossLevel)
            {
                _bossSpawnTimer = _bossSpawnDelay;
                _bossTimerRunning = true;

                bool isLevel20 = _globalLevel % 20 == 0;
                if (isLevel20 && _hasBossWaitingForLevel20)
                    Debug.Log($"[SpawnController] Level 20 boss level — re-spawning parked boss in {_bossSpawnTimer:F1}s.");
                else
                    Debug.Log($"[SpawnController] Boss level — spawning boss in {_bossSpawnTimer:F1}s.");
            }
        }

        void ResolveChapterLevel()
        {
            if (chapterConfig == null)
            {
                Debug.LogError("[SpawnController] chapterConfig is null — call Configure() before ResetLevel().");
                return;
            }

            var cfg = chapterConfig;
            int n = Mathf.Max(1, cfg.totalLevels);
            int clampedIndex = Mathf.Clamp(_levelIndex, 0, n - 1);
            float t = n > 1 ? clampedIndex / (float)(n - 1) : 0f;

            _chapter = cfg.chapter;
            _globalLevel = (cfg.chapter - 1) * n + (clampedIndex + 1);

            _pressureMax = cfg.pressureMax;

            _targetScore = BucketRound(Mathf.RoundToInt(Geom(cfg.targetScoreMin, cfg.targetScoreMax, t)));
            _hpMultiplier = SnapToStep(Geom(cfg.hpMultMin, cfg.hpMultMax, t), 0.05f);

            float center = Mathf.Lerp(cfg.spawnIntervalEarly, cfg.spawnIntervalLate, t);
            float j = Mathf.Max(0f, cfg.spawnIntervalJitter);
            _spawnIntervalMin = Mathf.Max(0.3f, center - j);
            _spawnIntervalMax = Mathf.Max(_spawnIntervalMin + 0.1f, center + j);

            _minDuration = Mathf.Lerp(cfg.minDurationStart, cfg.minDurationEnd, t);

            _maxE4 = Mathf.RoundToInt(Mathf.Lerp(cfg.maxE4Start, cfg.maxE4End, t));
            _maxE3 = Mathf.RoundToInt(Mathf.Lerp(cfg.maxE3Start, cfg.maxE3End, t));
            _maxE2 = Mathf.RoundToInt(Mathf.Lerp(cfg.maxE2Start, cfg.maxE2End, t));

            // Boss appears at the chapter mid-point (L10) as a phase-1 mid-boss that retreats at 50% HP,
            // and again at the chapter's final level (L20) as a phase-2 rematch. Matches the bar logic
            // in LevelProgressBar.OnBossHealthChanged.
            int oneBasedLevel = clampedIndex + 1;
            _isBossLevel = oneBasedLevel == n || oneBasedLevel == 10;
            _levelBossBirdConfig = cfg.bossBirdConfig;
            _bossSpawnDelay = cfg.bossSpawnDelay;

            _attackingBirdPool = cfg.attackingBirdPool;
            _attackingBirdSpawnInterval = cfg.attackingBirdSpawnInterval;
            _attackingBirdSpawnChance = cfg.attackingBirdSpawnChance;

            // ── Bird-mix weights (lerped across the chapter) ──
            _w1 = Mathf.Lerp(cfg.b1WeightStart, cfg.b1WeightEnd, t);
            _w2 = Mathf.Lerp(cfg.b2WeightStart, cfg.b2WeightEnd, t);
            _w3 = Mathf.Lerp(cfg.b3WeightStart, cfg.b3WeightEnd, t);
            _w4 = Mathf.Lerp(cfg.b4WeightStart, cfg.b4WeightEnd, t);
            NormalizeWeights();
            _perSpawnWeightJitter = Mathf.Clamp01(cfg.perSpawnWeightJitter);

            // ── Live pressure variance ──
            _pressureVariancePercent = Mathf.Clamp(cfg.pressureVariancePercent, 0f, 0.5f);
            _pressureNoiseFrequency = Mathf.Max(0f, cfg.pressureNoiseFrequency);
            // Per-attempt seed so replays don't feel identical even with the same chapter/level.
            _pressureNoiseSeed = Random.Range(0f, 1000f) + _priorAttempts * 13.37f;

            // ── Replay difficulty bump (boss levels get half the cap to avoid snowballing frustration) ──
            int maxBumps = _isBossLevel
                ? Mathf.Max(0, cfg.replayMaxBumps / 2)
                : Mathf.Max(0, cfg.replayMaxBumps);
            int bumps = Mathf.Min(_priorAttempts, maxBumps);
            if (bumps > 0)
            {
                float hpJitter = Random.Range(1f - cfg.replayJitterPercent, 1f + cfg.replayJitterPercent);
                float pJitter = Random.Range(1f - cfg.replayJitterPercent, 1f + cfg.replayJitterPercent);

                float hpBump = 1f + (cfg.replayHpStep * bumps * hpJitter);
                float pBump = 1f + (cfg.replayPressureStep * bumps * pJitter);

                _hpMultiplier = SnapToStep(_hpMultiplier * hpBump, 0.05f);
                _pressureMax = Mathf.Max(1, Mathf.RoundToInt(_pressureMax * pBump));
            }

            Debug.Log(
                $"[SpawnController] Ch{_chapter} L{clampedIndex + 1} (G{_globalLevel}) resolved | " +
                $"target={_targetScore} hp×{_hpMultiplier:F2} pMax={_pressureMax} " +
                $"spawn={_spawnIntervalMin:F2}–{_spawnIntervalMax:F2}s " +
                $"weights B1:{_w1:F2} B2:{_w2:F2} B3:{_w3:F2} B4:{_w4:F2} " +
                $"caps E4:{_maxE4} E3:{_maxE3} E2:{_maxE2} boss={_isBossLevel} retry={_priorAttempts}");
        }

        void NormalizeWeights()
        {
            float sum = _w1 + _w2 + _w3 + _w4;
            if (sum <= 0f) { _w1 = 0.5f; _w2 = 0.3f; _w3 = 0.15f; _w4 = 0.05f; return; }
            _w1 /= sum; _w2 /= sum; _w3 /= sum; _w4 /= sum;
        }

        static float Geom(float min, float max, float t)
        {
            if (min <= 0f || max <= 0f) return Mathf.Lerp(min, max, t);
            return min * Mathf.Pow(max / min, t);
        }

        static float SnapToStep(float value, float step)
            => step <= 0f ? value : Mathf.Round(value / step) * step;

        static int BucketRound(int score)
        {
            int bucket;
            if (score < 1000) bucket = 50;
            else if (score < 5000) bucket = 100;
            else if (score < 20000) bucket = 250;
            else if (score < 50000) bucket = 500;
            else bucket = 1000;
            return Mathf.Max(50, (score / bucket) * bucket);
        }

        void Update()
        {
            if (Managers.GameManager.Instance.state != GameState.Gameplay) return;

            elapsedTime += Time.deltaTime;

            // Hold all spawning until the level-start countdown finishes.
            if (elapsedTime < StartupSpawnDelay) return;

            _spawnTimer += Time.deltaTime;
            float interval = GetCurrentSpawnInterval();

            bool canSpawnRegular = !_isBossLevel && !_levelCompleted && !_draining;

            if (canSpawnRegular && _spawnTimer >= interval)
            {
                TrySpawnBird();
                _spawnTimer = 0f;
            }

            // Empty-screen safety net — Ball Blast vibe never lets the screen breathe for long.
            // If no birds, eggs, or attacking birds are alive, force a spawn after a short grace.
            if (canSpawnRegular &&
                _activeBirds.Count == 0 && _activeEggs.Count == 0 && _activeAttackingBirds.Count == 0)
            {
                _emptyScreenTimer += Time.deltaTime;
                if (_emptyScreenTimer > 1.25f)
                {
                    ForceSpawnBird();
                    _emptyScreenTimer = 0f;
                    _spawnTimer = 0f;
                }
            }
            else
            {
                _emptyScreenTimer = 0f;
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

            if (_attackingBirdPool != null
                && _attackingBirdPool.Length > 0
                && _attackingBirdSpawnInterval > 0f
                && !_levelCompleted && !_draining && !_isBossLevel && !IsBossAlive)
            {
                _attackingBirdTimer -= Time.deltaTime;
                if (_attackingBirdTimer <= 0f)
                {
                    TrySpawnAttackingBird();
                    _attackingBirdTimer = _attackingBirdSpawnInterval;
                }
            }

            if (_inGrace)
            {
                _graceTimer -= Time.deltaTime;
                GameEvents.FireGraceTimeTick(Mathf.Max(0f, _graceTimer));

                if (_graceTimer <= 0f)
                {
                    _inGrace = false;
                    GameEvents.FireGraceTimeEnded();
                    KillRemainingEggsViaDamage();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  BOSS SPAWN / ENTER / EXIT / RETREAT
        // ═══════════════════════════════════════════════════════════════

        private void TrySpawnBoss()
        {
            if (_bossSpawned) return;

            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossDefeated -= HandleBossDefeated;
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

            bool isLevel20 = _globalLevel % 20 == 0;

            if (isLevel20 && _hasBossWaitingForLevel20 && _parkedBossGO != null)
            {
                RespawnParkedBoss();
                return;
            }

            BossBirdConfig cfg = _levelBossBirdConfig;
            if (cfg == null && chapterBossConfigs != null)
            {
                int idx = _chapter - 1;
                if (idx >= 0 && idx < chapterBossConfigs.Length)
                    cfg = chapterBossConfigs[idx];
            }

            if (cfg == null)
            {
                Debug.LogError($"[SpawnController] Boss level Ch{_chapter} has no BossBirdConfig — falling back to a high-tier wave.");
                FallbackToHighTierWave();
                return;
            }

            if (bossPrefabs == null || bossPrefabs.Length == 0)
            {
                Debug.LogError("[SpawnController] Boss level but bossPrefabs array is empty — falling back to a high-tier wave.");
                FallbackToHighTierWave();
                return;
            }

            int prefabIdx = _chapter - 1;
            if (prefabIdx < 0 || prefabIdx >= bossPrefabs.Length || bossPrefabs[prefabIdx] == null)
            {
                Debug.LogError($"[SpawnController] bossPrefabs[{prefabIdx}] is missing for chapter {_chapter} — falling back to a high-tier wave.");
                FallbackToHighTierWave();
                return;
            }

            if (bossSpawnPoint == null)
            {
                Debug.LogError("[SpawnController] bossSpawnPoint is not assigned — falling back to a high-tier wave.");
                FallbackToHighTierWave();
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("[SpawnController] Camera.main is null — falling back to a high-tier wave.");
                FallbackToHighTierWave();
                return;
            }

            Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1.2f, 1.2f, 0f));
            topRight.z = 0f;

            var go = Instantiate(bossPrefabs[prefabIdx], topRight, Quaternion.identity);
            var controller = go.GetComponent<BossBirdController>();

            controller.Initialize(cfg, isLevel20);
            controller.SetInvulnerable(true);

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

        // If a boss-level can't actually spawn its boss (missing config/prefab/spawn point), don't
        // leave the player on a dead screen — degrade to a regular level with high-tier weights so
        // the level is still beatable and feels like a finale.
        private void FallbackToHighTierWave()
        {
            _bossSpawned = true;
            _bossTimerRunning = false;
            _isBossLevel = false;
            _w1 = 0.10f; _w2 = 0.25f; _w3 = 0.35f; _w4 = 0.30f;
            NormalizeWeights();
            _spawnTimer = float.MaxValue; // immediate first spawn
        }

        private void RespawnParkedBoss()
        {
            GameObject go = _parkedBossGO;
            float remainingHp = _parkedBossHpNormalized;

            _parkedBossGO = null;
            _hasBossWaitingForLevel20 = false;

            go.SetActive(true);

            var controller = go.GetComponent<BossBirdController>();
            controller.ReinitializeForPhase2(remainingHp);
            controller.SetInvulnerable(true);

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

        private void HandleBossRetreated(string bossName, float hpNormalized)
        {
            if (_activeBossGO == null) return;

            GameObject bossGO = _activeBossGO;
            _activeBossController = null;
            _activeBossGO = null;

            AnimateBossExit(bossGO, () =>
            {
                bossGO.SetActive(false);
                DontDestroyOnLoad(bossGO);

                _parkedBossGO = bossGO;
                _parkedBossHpNormalized = hpNormalized;
                _hasBossWaitingForLevel20 = true;
                GameProgressManager.Instance.CompleteLevel(100);

                Debug.Log($"[SpawnController] Boss '{bossName}' parked at {hpNormalized:P0} HP for Level 20.");
            });
        }

        private void HandleBossDefeated(string bossName, int score)
        {
            _bossDefeated = true;

            if (_activeBossGO != null)
            {
                GameObject bossGO = _activeBossGO;
                _activeBossController = null;
                _activeBossGO = null;

                AnimateBossExit(bossGO, () => Destroy(bossGO));
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

            float remaining = _minDuration - elapsedTime;

            // Boss levels keep the legacy drain path — boss-defeat / phase logic owns completion.
            if (_isBossLevel)
            {
                if (remaining > 0f)
                {
                    if (_drainingRoutine == null) _drainingRoutine = StartCoroutine(DelayedDrain(remaining));
                }
                else
                {
                    ExecuteDrain();
                }
                return;
            }

            // Non-boss: wait out min duration (if any), then enter grace instead of vanishing eggs.
            float wait = Mathf.Max(0f, remaining);
            if (_drainingRoutine == null) _drainingRoutine = StartCoroutine(BeginGraceAfter(wait));
        }

        private IEnumerator DelayedDrain(float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteDrain();
        }

        private IEnumerator BeginGraceAfter(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            BeginGrace();
        }

        private void BeginGrace()
        {
            // Player already cleared the screen during the min-duration wait — finish immediately.
            if (_activeEggs.Count == 0)
            {
                ExecuteDrain();
                return;
            }

            _inGrace = true;
            _graceTimer = GraceDuration;
            GameEvents.FireGraceTimeStarted(GraceDuration);
        }

        // Called when grace time runs out with eggs still alive. Each remaining egg takes lethal
        // damage so its destroy VFX, score award, and OnEggDestroyed event fire normally.
        // Eggs self-remove via HandleEggDestroyed once their death sequence completes; that path
        // ultimately fires FireAllEggsCleared and the level completes.
        private void KillRemainingEggsViaDamage()
        {
            _isForceDestroying = true;
            _lastCompletionWasForceDestroy = true;

            // Snapshot — TakeDamage may indirectly mutate _activeEggs through HandleEggDestroyed.
            var snapshot = new List<Egg>(_activeEggs);
            foreach (var e in snapshot)
            {
                if (e == null) continue;
                var dmg = e.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive) dmg.TakeDamage(int.MaxValue);
            }

            // Death-sequence animations may run for another frame or two; clear the per-frame
            // flag here. The persistent flag (LastCompletionWasForceDestroy) lives until ResetLevel
            // so RewardManager.HandleLevelCompleted picks it up.
            _isForceDestroying = false;
            _levelCompleted = true;
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
                "E4" => _maxE4,
                "E3" => _maxE3,
                "E2" => _maxE2,
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
        //  SPAWN INTERVAL / LIVE PRESSURE
        // ═══════════════════════════════════════════════════════════════

        float GetCurrentSpawnInterval()
            => Mathf.Max(0.3f, Random.Range(_spawnIntervalMin, _spawnIntervalMax));

        /// <summary>
        /// Live pressure cap — base value oscillates by ±pressureVariancePercent via slow perlin noise.
        /// Floor at 80% of base so the screen never starves to dead air during a low-noise dip.
        /// </summary>
        int GetLivePressureMax()
        {
            if (_pressureVariancePercent <= 0f || _pressureNoiseFrequency <= 0f) return _pressureMax;
            float n = Mathf.PerlinNoise(_pressureNoiseSeed, Time.time * _pressureNoiseFrequency); // 0..1
            float swing = (n - 0.5f) * 2f;                                                        // -1..1
            float mult = 1f + swing * _pressureVariancePercent;
            int v = Mathf.RoundToInt(_pressureMax * mult);
            int floor = Mathf.Max(1, Mathf.RoundToInt(_pressureMax * 0.8f));
            return Mathf.Max(floor, v);
        }

        // ═══════════════════════════════════════════════════════════════
        //  BIRD SPAWNING
        // ═══════════════════════════════════════════════════════════════

        void TrySpawnBird()
        {
            int liveMax = GetLivePressureMax();
            int remaining = liveMax - _pressureTracker.CurrentPressure;

            // Pressure is full — give the screen one tick to clear instead of spawning.
            if (remaining <= 0) return;

            BirdConfig chosen = SelectBirdType(remaining);
            if (chosen == null) return;

            int lifetimePressure = EstimateLifetimePressure(chosen);
            if (lifetimePressure > 0 && lifetimePressure > remaining)
            {
                var cheap = GetCheaperBird(chosen);
                if (cheap != null) chosen = cheap;
                // If even B1 doesn't fit, accept slight overflow — never leave the screen empty
                // because we couldn't perfectly hit the budget.
                else chosen = FindBird("B1") ?? chosen;
            }

            SpawnBirdInstance(chosen);
        }

        // Bypasses pressure checks — used by the empty-screen safety net. Always spawns at least B1.
        void ForceSpawnBird()
        {
            var b = FindBird("B1") ?? FindBird("B2") ?? FindBird("B3") ?? FindBird("B4");
            if (b == null) return;
            SpawnBirdInstance(b);
        }

        BirdConfig GetCheaperBird(BirdConfig current)
        {
            if (current.birdId == "B4") return FindBird("B3") ?? FindBird("B2") ?? FindBird("B1");
            if (current.birdId == "B3") return FindBird("B2") ?? FindBird("B1");
            if (current.birdId == "B2") return FindBird("B1");
            return null;
        }

        BirdConfig FindBird(string id)
        {
            foreach (var b in birds)
                if (b != null && b.birdId == id) return b;
            return null;
        }

        /// <summary>
        /// Picks a bird by combining the per-level weight, an affordability factor (cheaper birds
        /// are more likely when the pressure budget is tight), and a per-spawn random jitter.
        /// </summary>
        BirdConfig SelectBirdType(int remainingPressure)
        {
            var b1 = FindBird("B1");
            var b2 = FindBird("B2");
            var b3 = FindBird("B3");
            var b4 = FindBird("B4");

            float c1 = b1 != null ? EstimateLifetimePressure(b1) : 0f;
            float c2 = b2 != null ? EstimateLifetimePressure(b2) : 0f;
            float c3 = b3 != null ? EstimateLifetimePressure(b3) : 0f;
            float c4 = b4 != null ? EstimateLifetimePressure(b4) : 0f;

            float r = Mathf.Max(1f, remainingPressure);
            float f1 = b1 != null ? Affordability(c1, r) : 0f;
            float f2 = b2 != null ? Affordability(c2, r) : 0f;
            float f3 = b3 != null ? Affordability(c3, r) : 0f;
            float f4 = b4 != null ? Affordability(c4, r) : 0f;

            float w1 = _w1 * f1 * Jitter();
            float w2 = _w2 * f2 * Jitter();
            float w3 = _w3 * f3 * Jitter();
            float w4 = _w4 * f4 * Jitter();

            float sum = w1 + w2 + w3 + w4;
            if (sum <= 0f) return b1 ?? b2 ?? b3 ?? b4; // budget too tight — fall back to any available

            float roll = Random.value * sum;
            if ((roll -= w1) < 0f) return b1;
            if ((roll -= w2) < 0f) return b2;
            if ((roll -= w3) < 0f) return b3;
            return b4;
        }

        // 1.0 when fully affordable; falls off quadratically as cost outstrips remaining budget.
        // Quadratic (instead of linear) keeps the chapter weight curve honest — early chapters
        // really should mostly spawn B1, even with the residual floor for budget edge cases.
        static float Affordability(float cost, float remaining)
        {
            if (cost <= 0f) return 1f;
            float ratio = Mathf.Clamp01(remaining / cost);
            if (ratio >= 1f) return 1f;
            return 0.05f + 0.95f * ratio * ratio;
        }

        float Jitter()
        {
            if (_perSpawnWeightJitter <= 0f) return 1f;
            return 1f + (Random.value - 0.5f) * 2f * _perSpawnWeightJitter;
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

            int hp = Mathf.Max(1, Mathf.RoundToInt(birdConfig.baseHp * _hpMultiplier));
            bird.Init(birdConfig, hp);

            bird.OnLayEgg += HandleBirdLayEgg;
            bird.OnDestroyed += HandleBirdDestroyed;

            _activeBirds.Add(bird);
        }

        #region Attacking Birds

        private void TrySpawnAttackingBird()
        {
            if (_attackingBirdPool == null || _attackingBirdPool.Length == 0) return;
            if (Random.value > _attackingBirdSpawnChance) return;

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

            attackingBird.Init(config, _hpMultiplier);
            attackingBird.OnDestroyed += HandleAttackingBirdDestroyed;
            _activeAttackingBirds.Add(attackingBird);
        }

        private AttackingBirdConfig PickAttackingBirdConfig()
        {
            var pool = _attackingBirdPool;
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

            // If the primary tier is at cap, cascade down (E4→E3→E2→E1) so the lay still lands.
            // Ball Blast vibe: every "lay" the bird does should produce a visible target.
            while (tier != null && (!IsEggTierAllowed(tier) || tier.eggPrefab == null))
                tier = tier.splitInto;
            if (tier == null) return;

            var go = Instantiate(tier.eggPrefab, bird.transform.position, Quaternion.identity);
            var egg = go.GetComponent<Egg>();

            int baseHp = Random.Range(tier.baseHpMin, tier.baseHpMax + 1);
            int hp = Mathf.Max(1, Mathf.RoundToInt(baseHp * _hpMultiplier));

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
                        int hp = Mathf.Max(1, Mathf.RoundToInt(baseHp * _hpMultiplier));

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
                if (_inGrace)
                {
                    _inGrace = false;
                    GameEvents.FireGraceTimeEnded();
                }
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
