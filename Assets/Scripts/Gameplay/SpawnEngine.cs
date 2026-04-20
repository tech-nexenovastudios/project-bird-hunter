using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Gameplay
{
    /// <summary>
    /// Spawn engine for Bird Hunter. Drives regular waves on non-boss levels and
    /// orchestrates the boss two-act arc on levels 10 and 20 of every chapter.
    /// </summary>
    public class SpawnEngine : MonoBehaviour
    {
        [Header("Config")]
        public LevelProfile levelProfile;
        public AdaptiveDifficultyConfig adaptiveConfig;
        public BirdConfig[] birds;            // indexed 0..3 (B1..B4); egg tier/prefab come from BirdConfig.eggTier
        public Transform[] birdSpawnPoints;
        public Transform bossSpawnPoint;

        [Header("Boss Configs (index = chapter - 1)")]
        public BossBirdConfig[] chapterBossConfigs;

        [Header("Boss Prefabs (index = chapter - 1)")]
        public GameObject[] bossPrefabs;

        [Header("State (read-only)")]
        public float elapsedTime;

        static readonly int[] BirdBaseHp = { 20, 50, 120, 300 };
        static readonly int[] EggBaseHp = { 5, 12, 30, 80 };

        readonly int[][] _chapter1Spawns = new int[][]
        {
            new[] { 0, 0, 0, 0 },                              // lv1
            new[] { 0, 0, 0, 0, 0 },                           // lv2
            new[] { 0, 0, 0, 0, 0, 0 },                        // lv3
            new[] { 0, 0, 1, 0, 0, 0 },                        // lv4 — first B2
            new[] { 0, 0, 1, 0, 1, 0 },                        // lv5
            new[] { 0, 1, 0, 1, 0, 0, 0 },                     // lv6
            new[] { 0, 1, 0, 1, 0, 1, 0 },                     // lv7
            new[] { 0, 1, 1, 0, 1, 0, 0, 0 },                  // lv8
            new[] { 0, 1, 2, 0, 1, 0, 0, 0 },                  // lv9 — first B3
            null,                                              // lv10 — boss act 1
            new[] { 0, 1, 2, 0, 1, 0, 1, 0 },                  // lv11
            new[] { 1, 0, 2, 1, 0, 1, 0, 0 },                  // lv12
            new[] { 1, 2, 0, 1, 2, 0, 1, 0 },                  // lv13
            new[] { 1, 2, 3, 0, 1, 2, 0, 0, 1 },               // lv14 — first B4
            new[] { 1, 2, 3, 1, 2, 0, 1, 1 },                  // lv15
            new[] { 2, 1, 3, 0, 2, 1, 3, 1, 0 },               // lv16
            new[] { 1, 3, 2, 1, 2, 0, 3, 1, 1 },               // lv17
            new[] { 1, 2, 3, 1, 2, 0, 3, 1, 2 },               // lv18
            new[] { 2, 3, 1, 2, 3, 1, 2, 1, 1 },               // lv19
            null                                               // lv20 — boss act 2
        };

        PressureTracker _pressureTracker;
        readonly List<BaseBird> _activeBirds = new();
        readonly List<Egg> _activeEggs = new();

        int _currentChapter;
        int _currentLevel;
        float _currentD;
        float _currentHM;
        float _currentInterval;
        int _totalBirds;
        int _waveCount;
        int _birdsPerWave;
        List<int> _spawnQueue = new();
        int _spawnQueueIndex;
        float _spawnTimer;
        float _interWaveGapTimer;
        bool _inInterWaveGap;
        int _eggSortingCounter;

        GameObject _activeBossGO;
        BossBirdController _activeBossController;
        bool _bossTimerRunning;
        float _bossSpawnTimer;

        Coroutine _drainingCoroutine;
        int _drainCount;

        readonly Dictionary<BirdConfig, IObjectPool<BaseBird>> _birdPools = new();
        readonly Dictionary<EggTierConfig, IObjectPool<Egg>> _eggPools = new();
        Transform _birdPoolParent;
        Transform _eggPoolParent;

        /// <summary>True while a boss is alive on the field.</summary>
        public bool IsBossAlive => _activeBossController != null && !_activeBossController.IsDead;

        void OnEnable()
        {
            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossDefeated += HandleBossDefeated;
                BossEventBus.Instance.OnBossRetreated += HandleBossRetreated;
            }
        }

        void OnDisable()
        {
            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossDefeated -= HandleBossDefeated;
                BossEventBus.Instance.OnBossRetreated -= HandleBossRetreated;
            }
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.state != GameState.Gameplay) return;

            elapsedTime += Time.deltaTime;

            if (_bossTimerRunning)
            {
                _bossSpawnTimer -= Time.deltaTime;
                if (_bossSpawnTimer <= 0f)
                {
                    _bossTimerRunning = false;
                    TrySpawnBoss();
                }
                return;
            }

            if (IsBossAlive) return;
            if (_drainingCoroutine != null) return;
            if (levelProfile != null && levelProfile.isBossLevel) return;

            if (_inInterWaveGap)
            {
                _interWaveGapTimer -= Time.deltaTime;
                if (_interWaveGapTimer <= 0f)
                {
                    _inInterWaveGap = false;
                    _spawnTimer = 0f;
                }
                return;
            }

            if (_spawnQueueIndex >= _spawnQueue.Count) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _currentInterval)
            {
                SpawnNextBird();
                _spawnTimer = 0f;

                if (_spawnQueueIndex < _spawnQueue.Count
                    && _birdsPerWave > 0
                    && _spawnQueueIndex % _birdsPerWave == 0)
                {
                    _inInterWaveGap = true;
                    _interWaveGapTimer = 2.5f;
                }
            }
        }

        // ───────── Level init ─────────

        /// <summary>
        /// Recompute difficulty, HP multiplier and the wave schedule, then prime either
        /// the boss countdown or the regular spawn queue.
        /// </summary>
        public void InitLevel(int chapter, int level, int plays)
        {
            _currentChapter = chapter;
            _currentLevel = level;

            _currentHM = Mathf.Pow(1.10f, chapter) * (1f + level / 40f);

            float cw = Mathf.Pow(1.08f, chapter);
            float lw = 1f + (level / 20f) * 0.5f;
            float pc = 1f + Mathf.Log(Mathf.Max(1, plays), 2f) * 0.1f;
            _currentD = cw * lw * pc;

            float ttc = TargetTimeToClear(chapter, level);
            _totalBirds = TotalBirdsForLevel(chapter, level);
            _waveCount = WaveCount(chapter, level);

            float activeTime = ttc - 5f;
            float rawInterval = (activeTime - (_waveCount - 1) * 2.5f) / Mathf.Max(1, _totalBirds);
            _currentInterval = Mathf.Clamp(rawInterval, 0.35f, 3.5f);

            elapsedTime = 0f;
            _spawnTimer = 0f;
            _spawnQueueIndex = 0;
            _inInterWaveGap = false;
            _interWaveGapTimer = 0f;
            _eggSortingCounter = 0;
            _spawnQueue.Clear();

            Debug.Log(
                $"[SpawnEngine] InitLevel Ch{chapter} L{level} plays={plays} " +
                $"D={_currentD:F2} HM={_currentHM:F2} TTC={ttc:F1}s " +
                $"N={_totalBirds} waves={_waveCount} interval={_currentInterval:F2}s");

            bool isBossLevel = levelProfile != null && levelProfile.isBossLevel;
            if (isBossLevel)
            {
                _bossTimerRunning = true;
                _bossSpawnTimer = levelProfile.bossSpawnDelay;
                Debug.Log($"[SpawnEngine] Boss level — spawning boss in {_bossSpawnTimer:F1}s");
            }
            else
            {
                _bossTimerRunning = false;
                BuildSpawnQueue(chapter, level);
                _birdsPerWave = Mathf.Max(1, Mathf.CeilToInt(_spawnQueue.Count / (float)_waveCount));
            }
        }

        void BuildSpawnQueue(int chapter, int level)
        {
            _spawnQueue.Clear();

            if (chapter == 1)
            {
                int[] table = null;
                if (level >= 1 && level <= _chapter1Spawns.Length)
                    table = _chapter1Spawns[level - 1];

                if (table != null)
                {
                    for (int i = 0; i < table.Length; i++)
                        _spawnQueue.Add(table[i]);
                }

                _totalBirds = _spawnQueue.Count;
            }
            else
            {
                float[] weights = ComputeTierWeights(_currentD);
                for (int i = 0; i < _totalBirds; i++)
                    _spawnQueue.Add(RollBirdTier(weights));
            }
        }

        // ───────── TTC / wave generation ─────────

        /// <summary>Target time to clear (seconds) for a given chapter and level.</summary>
        public float TargetTimeToClear(int chapter, int level)
        {
            float baseSec, rampSec;

            if (chapter == 1) { baseSec = 30f; rampSec = 0.5f; }
            else if (chapter == 2) { baseSec = 45f; rampSec = 3.5f; }
            else if (chapter <= 5) { baseSec = Mathf.Lerp(45f, 80f, (chapter - 2) / 3f); rampSec = 2.0f; }
            else if (chapter <= 10) { baseSec = Mathf.Lerp(80f, 110f, (chapter - 5) / 5f); rampSec = 1.5f; }
            else if (chapter <= 20) { baseSec = Mathf.Lerp(110f, 130f, (chapter - 10) / 10f); rampSec = 1.5f; }
            else { baseSec = Mathf.Lerp(130f, 150f, (chapter - 20) / 10f); rampSec = 1.5f; }

            return baseSec + level * rampSec;
        }

        /// <summary>Birds-per-second density for a given chapter.</summary>
        public float Density(int chapter) => 0.15f + chapter * 0.02f;

        /// <summary>Total bird count for a level, derived from TTC and density.</summary>
        public int TotalBirdsForLevel(int chapter, int level)
        {
            float ttc = TargetTimeToClear(chapter, level);
            float activeTime = ttc - 5f;
            int total = Mathf.RoundToInt(activeTime * Density(chapter));
            return Mathf.Clamp(total, 6, 80);
        }

        /// <summary>Number of waves in a level, derived from TTC.</summary>
        public int WaveCount(int chapter, int level)
        {
            float ttc = TargetTimeToClear(chapter, level);
            return Mathf.Clamp(Mathf.FloorToInt(ttc / 20f), 2, 7);
        }

        // ───────── Tier weights (chapters 2+) ─────────

        /// <summary>D-weighted tier distribution with minimum-weight floors for variety.</summary>
        public float[] ComputeTierWeights(float D)
        {
            float w1 = Mathf.Max(0.10f, 1f - D / 8f);
            float w2 = Mathf.Clamp(D / 6f, 0.10f, 0.45f);
            float w3 = Mathf.Clamp(D / 14f, 0.08f, 0.35f);
            float w4 = Mathf.Clamp(D / 22f, 0.05f, 0.25f);
            float total = w1 + w2 + w3 + w4;
            return new[] { w1 / total, w2 / total, w3 / total, w4 / total };
        }

        /// <summary>Rolls a 0..3 tier index using the provided normalised weights.</summary>
        public int RollBirdTier(float[] weights)
        {
            float roll = Random.value;
            float cumulative = 0f;
            for (int i = 0; i < 4; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return i;
            }
            return 3;
        }

        // ───────── Bird spawn ─────────

        void SpawnNextBird()
        {
            if (_spawnQueueIndex >= _spawnQueue.Count) return;
            int tier = _spawnQueue[_spawnQueueIndex++];
            SpawnBird(tier);
        }

        void SpawnBird(int tier)
        {
            if (birds == null || tier < 0 || tier >= birds.Length) return;
            BirdConfig cfg = birds[tier];
            if (cfg == null || cfg.birdPrefab == null) return;

            Transform spawnPoint = PickBirdSpawnPoint();
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            BaseBird bird = GetBirdPool(cfg).Get();
            if (bird == null) return;

            Transform t = bird.transform;
            t.SetPositionAndRotation(pos, rot);

            int hp = Mathf.Max(1, Mathf.RoundToInt(BirdBaseHp[tier] * _currentHM));
            bird.Init(cfg, hp);

            bird.OnLayEgg += HandleBirdLayEgg;
            bird.OnDespawnReady += HandleBirdDespawnReady;

            _activeBirds.Add(bird);
        }

        IObjectPool<BaseBird> GetBirdPool(BirdConfig cfg)
        {
            if (_birdPools.TryGetValue(cfg, out var existing)) return existing;

            if (_birdPoolParent == null)
            {
                var parent = new GameObject("[BirdPool]");
                parent.transform.SetParent(transform, false);
                _birdPoolParent = parent.transform;
            }

            var pool = new ObjectPool<BaseBird>(
                createFunc: () =>
                {
                    var go = Instantiate(cfg.birdPrefab, _birdPoolParent);
                    var b = go.GetComponent<BaseBird>();
                    if (b == null)
                    {
                        Debug.LogWarning($"[SpawnEngine] Bird prefab '{cfg.birdId}' missing BaseBird component.");
                        Destroy(go);
                    }
                    return b;
                },
                actionOnGet: b =>
                {
                    if (b != null) b.gameObject.SetActive(true);
                },
                actionOnRelease: b =>
                {
                    if (b == null) return;
                    b.transform.SetParent(_birdPoolParent, false);
                    b.gameObject.SetActive(false);
                },
                actionOnDestroy: b =>
                {
                    if (b != null) Destroy(b.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 64);

            _birdPools[cfg] = pool;
            return pool;
        }

        void ReleaseBird(BaseBird bird)
        {
            if (bird == null || bird.config == null) return;
            bird.OnLayEgg -= HandleBirdLayEgg;
            bird.OnDespawnReady -= HandleBirdDespawnReady;

            if (_birdPools.TryGetValue(bird.config, out var pool))
                pool.Release(bird);
            else
                Destroy(bird.gameObject);
        }

        void HandleBirdDespawnReady(BaseBird bird)
        {
            if (bird == null) return;
            _activeBirds.Remove(bird);
            ReleaseBird(bird);
        }

        Transform PickBirdSpawnPoint()
        {
            if (birdSpawnPoints == null || birdSpawnPoints.Length == 0) return null;
            return birdSpawnPoints[Random.Range(0, birdSpawnPoints.Length)];
        }

        // ───────── Egg lay ─────────

        /// <summary>Called when a bird lays its egg. Skipped while a boss is on the field.</summary>
        public void HandleBirdLayEgg(BaseBird bird)
        {
            if (IsBossAlive) return;
            if (bird == null || bird.config == null) return;

            int tier = ResolveBirdTier(bird);
            if (tier < 0) return;

            EggTierConfig eggCfg = bird.config.eggTier;
            if (eggCfg == null || eggCfg.eggPrefab == null) return;

            Vector3 spawnPos = bird.EggSpawnPoint != null ? bird.EggSpawnPoint.position : bird.transform.position;
            SpawnEgg(eggCfg, tier, spawnPos, applyIframes: false);
        }

        int ResolveBirdTier(BaseBird bird)
        {
            if (bird == null || bird.config == null || birds == null) return -1;
            for (int i = 0; i < birds.Length; i++)
                if (birds[i] == bird.config) return i;
            return -1;
        }

        /// <summary>Returns the HP value for a freshly spawned egg of the given tier.</summary>
        public int EvaluateEggHealth(int tier)
        {
            if (tier < 0 || tier >= EggBaseHp.Length) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(EggBaseHp[tier] * _currentHM));
        }

        Egg SpawnEgg(EggTierConfig cfg, int tier, Vector3 position, bool applyIframes)
        {
            if (cfg == null || cfg.eggPrefab == null) return null;

            Egg egg = GetEggPool(cfg).Get();
            if (egg == null) return null;

            egg.transform.SetPositionAndRotation(position, Quaternion.identity);

            int hp = EvaluateEggHealth(tier);
            egg.Init(cfg, hp, _eggSortingCounter++);

            egg.OnTrySplit += HandleEggSplit;
            egg.OnDestroyed += HandleEggDestroyed;

            _activeEggs.Add(egg);

            Transform t = egg.transform;
            Vector3 finalScale = t.localScale == Vector3.zero ? Vector3.one : t.localScale;
            t.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(t.DOScale(finalScale, 0.2f).SetEase(Ease.OutBack));
            seq.OnComplete(() =>
            {
                if (egg != null) ApplyPhysics(egg);
            });

            if (applyIframes)
                StartCoroutine(IframeRoutine(egg, 0.15f));

            return egg;
        }

        IObjectPool<Egg> GetEggPool(EggTierConfig cfg)
        {
            if (_eggPools.TryGetValue(cfg, out var existing)) return existing;

            if (_eggPoolParent == null)
            {
                var parent = new GameObject("[EggPool]");
                parent.transform.SetParent(transform, false);
                _eggPoolParent = parent.transform;
            }

            var pool = new ObjectPool<Egg>(
                createFunc: () =>
                {
                    var go = Instantiate(cfg.eggPrefab, _eggPoolParent);
                    var e = go.GetComponent<Egg>();
                    if (e == null)
                    {
                        Debug.LogWarning($"[SpawnEngine] Egg prefab '{cfg.tierId}' missing Egg component.");
                        Destroy(go);
                    }
                    return e;
                },
                actionOnGet: e =>
                {
                    if (e != null) e.gameObject.SetActive(true);
                },
                actionOnRelease: e =>
                {
                    if (e == null) return;
                    e.transform.SetParent(_eggPoolParent, false);
                    e.gameObject.SetActive(false);
                },
                actionOnDestroy: e =>
                {
                    if (e != null) Destroy(e.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: 16,
                maxSize: 128);

            _eggPools[cfg] = pool;
            return pool;
        }

        void ReleaseEgg(Egg egg)
        {
            if (egg == null || egg.config == null) return;
            egg.OnTrySplit -= HandleEggSplit;
            egg.OnDestroyed -= HandleEggDestroyed;

            if (_eggPools.TryGetValue(egg.config, out var pool))
                pool.Release(egg);
            else
                Destroy(egg.gameObject);
        }

        /// <summary>Re-enables rigidbody physics and gives the egg a small randomised impulse.</summary>
        public void ApplyPhysics(Egg egg)
        {
            if (egg == null) return;
            Rigidbody2D rb = egg.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-0.5f, 0.5f));
        }

        IEnumerator IframeRoutine(Egg egg, float duration)
        {
            if (egg == null) yield break;
            Collider2D col = egg.GetComponent<Collider2D>();
            if (col == null) yield break;

            col.enabled = false;
            yield return new WaitForSeconds(duration);
            col.enabled = true;
        }

        // ───────── Egg split ─────────

        /// <summary>Called when an egg is destroyed and must split into 2 lower-tier eggs.</summary>
        public void HandleEggSplit(Egg egg)
        {
            if (IsBossAlive) return;
            if (egg == null || egg.config == null) return;

            EggTierConfig childCfg = egg.config.splitInto;
            if (childCfg == null) return;   // E1 cannot split further

            int parentTier = ResolveEggTier(egg);
            int childTier = parentTier > 0 ? parentTier - 1 : 0;

            Vector3 origin = egg.transform.position;
            _activeEggs.Remove(egg);

            StartCoroutine(DelayedSplit(childCfg, childTier, origin, 0.3f));
        }

        int ResolveEggTier(Egg egg)
        {
            if (egg == null || egg.config == null || birds == null) return -1;
            EggTierConfig cfg = egg.config;
            for (int i = 0; i < birds.Length; i++)
            {
                if (birds[i] != null && birds[i].eggTier == cfg) return i;
            }
            return -1;
        }

        IEnumerator DelayedSplit(EggTierConfig childCfg, int childTier, Vector3 origin, float delay)
        {
            yield return new WaitForSeconds(delay);

            Vector3 leftPos = origin + new Vector3(-0.4f, 0f, 0f);
            Vector3 rightPos = origin + new Vector3(0.4f, 0f, 0f);

            Egg left = SpawnEgg(childCfg, childTier, origin, applyIframes: true);
            if (left != null)
                left.transform.DOJump(leftPos, 1.2f, 1, 0.35f);

            Egg right = SpawnEgg(childCfg, childTier, origin, applyIframes: true);
            if (right != null)
                right.transform.DOJump(rightPos, 1.2f, 1, 0.35f);
        }

        void HandleEggDestroyed(Egg egg)
        {
            if (egg == null) return;
            _activeEggs.Remove(egg);
            ReleaseEgg(egg);
        }

        // ───────── Boss ─────────

        /// <summary>HP budget helper (scales with chapter).</summary>
        public float BossBudget(int chapter) => 25f + chapter * 2.5f;

        /// <summary>Act 1 boss HP (level 10). HM-scaled.</summary>
        public float BossHPAct1(int chapter) => (500f + 30f * chapter) * _currentHM;

        /// <summary>Act 2 boss HP (level 20). HM-scaled with 1.4× rage multiplier.</summary>
        public float BossHPAct2(int chapter) => (500f + 30f * chapter) * _currentHM * 1.4f;

        /// <summary>Clears the field, instantiates the chapter boss, and applies act-specific HP.</summary>
        public void TrySpawnBoss()
        {
            int idx = _currentChapter - 1;
            if (chapterBossConfigs == null || bossPrefabs == null
                || idx < 0 || idx >= chapterBossConfigs.Length || idx >= bossPrefabs.Length)
            {
                Debug.LogWarning($"[SpawnEngine] No boss config/prefab for chapter {_currentChapter}.");
                return;
            }

            BossBirdConfig srcConfig = chapterBossConfigs[idx];
            GameObject prefab = bossPrefabs[idx];
            if (srcConfig == null || prefab == null)
            {
                Debug.LogWarning($"[SpawnEngine] Null boss config or prefab for chapter {_currentChapter}.");
                return;
            }

            ClearRegularEnemies();

            Vector3 pos = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
            Quaternion rot = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
            _activeBossGO = Instantiate(prefab, pos, rot);
            _activeBossController = _activeBossGO.GetComponent<BossBirdController>();
            if (_activeBossController == null)
            {
                Debug.LogWarning($"[SpawnEngine] Boss prefab for chapter {_currentChapter} missing BossBirdController.");
                Destroy(_activeBossGO);
                _activeBossGO = null;
                return;
            }

            bool isAct2 = _currentLevel == 20;
            float hp = isAct2 ? BossHPAct2(_currentChapter) : BossHPAct1(_currentChapter);

            BossBirdConfig runtimeConfig = Instantiate(srcConfig);
            runtimeConfig.maxHealth = hp;

            _activeBossController.Initialize(runtimeConfig, isAct2);

            Debug.Log($"[SpawnEngine] Boss spawned — Ch{_currentChapter} L{_currentLevel} " +
                      $"act{(isAct2 ? 2 : 1)} HP={hp:F0} budget={BossBudget(_currentChapter):F1}");
        }

        /// <summary>Returns all active birds and eggs to their pools.</summary>
        public void ClearRegularEnemies()
        {
            for (int i = _activeBirds.Count - 1; i >= 0; i--)
                ReleaseBird(_activeBirds[i]);
            _activeBirds.Clear();

            for (int i = _activeEggs.Count - 1; i >= 0; i--)
                ReleaseEgg(_activeEggs[i]);
            _activeEggs.Clear();
        }

        void HandleBossDefeated(string bossId, int chapter)
        {
            Debug.Log($"[SpawnEngine] Boss defeated — id={bossId} chapter={chapter}");
            _activeBossGO = null;
            _activeBossController = null;
            StartDrain();
        }

        void HandleBossRetreated(string bossId, float hpNorm)
        {
            Debug.Log($"[SpawnEngine] Boss retreated — id={bossId} hpNorm={hpNorm:F2} (partial reward, no gem)");
            _activeBossGO = null;
            _activeBossController = null;
            StartDrain();
        }

        // ───────── Drain ─────────

        /// <summary>Kicks off the end-of-level drain sequence after a short delay.</summary>
        public void StartDrain()
        {
            if (_drainingCoroutine != null)
            {
                Debug.LogWarning("[SpawnController] Double drain attempted");
                return;
            }

            _drainCount++;
            Debug.Log($"[SpawnController] Drain starting. Drain #{_drainCount}");
            _drainingCoroutine = StartCoroutine(DelayedDrain());
        }

        IEnumerator DelayedDrain()
        {
            yield return new WaitForSeconds(0.5f);
            ExecuteDrain();
        }

        void ExecuteDrain()
        {
            ClearRegularEnemies();

            if (_activeBossGO != null)
            {
                Destroy(_activeBossGO);
                _activeBossGO = null;
                _activeBossController = null;
            }

            GameEvents.FireLevelCompletedEarly(0f);
            GameEvents.FireAllEggsCleared();
            Debug.Log($"[SpawnEngine] Level drained. Drain #{_drainCount}");

            _drainingCoroutine = null;
        }
    }
}
