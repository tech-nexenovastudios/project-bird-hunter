using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.PowerUps;
using Newtonsoft.Json;
using Unity.Services.CloudSave.Models;
using UnityEngine;
using UnityUtils;

namespace Gameplay.Managers
{
    public class GameProgressManager : Singleton<GameProgressManager>
    {
        [Header("Powerup Pool")]
        public PowerupConfig[] allPowerups;

        [Header("Level Data")]
        public string levelResourcesPath = "Data/GeneratedLevels";
        [Tooltip("Resources path for ChapterProgressionConfig assets (Chapter{n}.asset). If a chapter config is found here, per-level profiles are resolved from it and the legacy per-level assets are ignored.")]
        public string chapterConfigResourcesPath = "Data/ChapterProgressions";

        private GameProgress _progress;
        private bool _progressLoaded;

        public static event Action<int, PowerupConfig[]> OnSpinTriggered;
        public static event Action<GameProgress> OnProgressChanged;
        public static event Action<LevelProfile> OnLevelLoaded;

        public GameProgress Data => _progress;
        public int CurrentChapter => _progress?.currentChapter ?? 1;
        public int CurrentLevel => _progress?.currentLevel ?? 1;
        public int GlobalLevel => (CurrentChapter - 1) * 20 + CurrentLevel;
        public int HighScore => _progress?.highScore ?? 0;
        public int TotalScore => _progress?.totalScore ?? 0;

        public int FurthestChapter
        {
            get
            {
                int furthest = CurrentChapter;
                var chapters = _progress?.chapters;
                if (chapters == null) return furthest;
                foreach (var cp in chapters)
                    if (cp != null && cp.cleared && cp.chapter + 1 > furthest)
                        furthest = cp.chapter + 1;
                return furthest;
            }
        }
        public PowerUpSlot[] CurrentSlots => _progress?.chapterSlots ?? new PowerUpSlot[0];

        public PowerupConfig LastSelectedPowerup { get; private set; }
        private int _currentSpinSlotIndex = 0;

        // Power Surge (PowerupConfig.id = "8") buffs the cannon's attack and must
        // remain equipped across every remaining chapter level — i.e. all 5 levels
        // after the spin that unlocked it — rather than being consumed after the
        // first apply. Re-equip flows through CannonPowerUpCaster.Equip, which
        // calls UnequipAll first, so the buff is rebound (not stacked) each level.
        // The slot is cleared naturally on chapter rollover via ResetSlotsForNewChapter.
        private const string PowerSurgeId = "8";

        // Session-only retry counters (not persisted). Resets on app restart and on chapter rollover.
        private readonly Dictionary<string, int> _sessionLevelAttempts = new();

        private static string AttemptKey(int chapter, int level) => $"Ch{chapter}_L{level}";

        public int GetLevelAttempts(int chapter, int level)
            => _sessionLevelAttempts.TryGetValue(AttemptKey(chapter, level), out var v) ? v : 0;

        /// <summary>
        /// Returns the number of *prior* attempts before this one (0 on first entry, 1 on first replay, ...) and
        /// increments the counter for the given level. Use the returned value to drive replay-difficulty bumps.
        /// </summary>
        public int RegisterLevelAttempt(int chapter, int level)
        {
            string key = AttemptKey(chapter, level);
            int prior = _sessionLevelAttempts.TryGetValue(key, out var v) ? v : 0;
            _sessionLevelAttempts[key] = prior + 1;
            return prior;
        }

        public void ResetAttemptsForChapter(int chapter)
        {
            string prefix = $"Ch{chapter}_L";
            var keys = new List<string>();
            foreach (var k in _sessionLevelAttempts.Keys)
                if (k.StartsWith(prefix)) keys.Add(k);
            foreach (var k in keys) _sessionLevelAttempts.Remove(k);
        }

        private static bool IsSpinLevel(int completedLevel)
            => completedLevel == 5 || completedLevel == 10 || completedLevel == 15;

        private static int GetSpinSlotIndex(int completedLevel)
        {
            return completedLevel switch
            {
                5 => 1,
                10 => 2,
                15 => 3,
                _ => -1
            };
        }

        protected override void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePowerupsLoaded();
        }

        private void EnsurePowerupsLoaded()
        {
            if (allPowerups != null && allPowerups.Length > 0) return;
            var db = PowerupGate.Database;
            if (db != null && db.allPowerups != null)
                allPowerups = db.allPowerups.ToArray();
        }

        private void OnEnable()
        {
            GameEvents.OnPreBossRecoveryActivated += OnPreBossRecoveryActivated;
        }

        private void OnDisable()
        {
            GameEvents.OnPreBossRecoveryActivated -= OnPreBossRecoveryActivated;
        }

        // The deferred Pre-Boss Recovery heal just fired (boss entered a boss level). Consume the
        // slot so it isn't re-equipped on later levels and can't heal again at the chapter's 2nd boss.
        private void OnPreBossRecoveryActivated()
        {
            if (_progress?.chapterSlots == null) return;
            bool changed = false;
            for (int i = 0; i < _progress.chapterSlots.Length; i++)
            {
                var slot = _progress.chapterSlots[i];
                if (slot == null || string.IsNullOrEmpty(slot.equippedPowerupId)) continue;
                if (!IsPreBossPowerup(slot.equippedPowerupId) || slot.hasBeenApplied) continue;
                slot.hasBeenApplied = true;
                changed = true;
            }
            if (changed) SaveProgress();
        }

        private bool IsPreBossPowerup(string powerupId)
        {
            if (allPowerups == null || string.IsNullOrEmpty(powerupId)) return false;
            for (int i = 0; i < allPowerups.Length; i++)
                if (allPowerups[i] != null && allPowerups[i].id == powerupId)
                    return allPowerups[i].effectType == PreBossHealModifier.ConfigEffectType;
            return false;
        }


        public LevelProfile GetCurrentLevelProfile()
            => LoadLevelProfile(CurrentChapter, CurrentLevel);

        public ChapterProgressionConfig GetCurrentChapterConfig()
            => LoadChapterConfig(CurrentChapter);

        public ChapterProgressionConfig LoadChapterConfig(int chapter)
        {
            var cfg = Resources.Load<ChapterProgressionConfig>($"{chapterConfigResourcesPath}/Chapter{chapter}");
            if (cfg == null)
                Debug.LogWarning($"[ProgressManager] ❌ ChapterProgressionConfig missing: {chapterConfigResourcesPath}/Chapter{chapter}");
            return cfg;
        }

        public LevelProfile LoadLevelProfile(int chapter, int level)
        {
            var chapterCfg = Resources.Load<ChapterProgressionConfig>($"{chapterConfigResourcesPath}/Chapter{chapter}");
            if (chapterCfg != null)
            {
                var resolved = LevelProfileResolver.Resolve(chapterCfg, level - 1);
                if (resolved != null)
                {
                    Debug.Log($"[ProgressManager] ✅ LevelProfile resolved from ChapterProgressionConfig: Ch{chapter} L{level}");
                    OnLevelLoaded?.Invoke(resolved);
                    GameEvents.FireGameLevelUpdated(level);
                    return resolved;
                }
            }

            string path = $"{levelResourcesPath}/Chapter{chapter}/Levels/Ch{chapter}_L{level:D2}";
            var profile = Resources.Load<LevelProfile>(path);

            if (profile == null) Debug.LogWarning($"[ProgressManager] ❌ LevelProfile missing: {path}");
            else
            {
                Debug.Log($"[ProgressManager] ✅ LevelProfile loaded (legacy): {path}");
                OnLevelLoaded?.Invoke(profile);
                GameEvents.FireGameLevelUpdated(level);
            }
            return profile;
        }

        // Persist the player's chapter choice from the main-menu carousel. A menu-selected
        // chapter always starts at level 1 with cleared powerup slots so the chapter-start
        // spin fires — level progress within a chapter is never resumed across selection.
        public void SelectChapter(int chapter)
        {
            if (_progress == null) _progress = new GameProgress();
            chapter = Mathf.Max(1, chapter);

            _progress.currentChapter = chapter;
            _progress.currentLevel = 1;
            _progress.ResetSlotsForNewChapter();
            SaveProgress();
        }


        public void SubmitRunScore(int runScore)
        {
            if (_progress == null) _progress = new GameProgress();
            var cp = _progress.GetOrCreateChapter(_progress.currentChapter);
            bool changed = false;
            if (runScore > cp.highScore)        { cp.highScore = runScore;        changed = true; }
            if (runScore > _progress.highScore) { _progress.highScore = runScore; changed = true; }
            if (changed) SaveProgress();
        }

        public ChapterProgress GetChapterProgress(int chapter)
            => _progress?.GetOrCreateChapter(chapter);

        public void RegisterChapterDeath()
        {
            if (_progress == null) _progress = new GameProgress();
            _progress.GetOrCreateChapter(_progress.currentChapter).attempts++;
            SaveProgress();
        }

        public void CompleteLevel(int scoreAchieved)
        {
            if (_progress == null) _progress = new GameProgress();

            _progress.totalScore += scoreAchieved;
            _progress.MarkFirstTimeClear(_progress.currentChapter, _progress.currentLevel);

            int completedLevel = _progress.currentLevel;

            var chap = _progress.GetOrCreateChapter(_progress.currentChapter);
            chap.highestLevelReached = Mathf.Max(chap.highestLevelReached, completedLevel);
            if (completedLevel >= 20) chap.cleared = true;

            if (IsSpinLevel(completedLevel))
            {
                int slotIndex = GetSpinSlotIndex(completedLevel);
                TriggerSpin(slotIndex);
            }

            if (_progress.currentLevel < 20)
            {
                _progress.currentLevel++;
            }
            else
            {
                int chapScore = ScoreManager.Instance != null ? ScoreManager.Instance.ChapterScore : 0;
                if (chapScore > chap.highScore) chap.highScore = chapScore;
                if (chapScore > _progress.highScore) _progress.highScore = chapScore;
                ScoreManager.Instance?.ResetChapterScore();

                ResetAttemptsForChapter(_progress.currentChapter);
                _progress.currentChapter++;
                _progress.currentLevel = 1;
                _progress.ResetSlotsForNewChapter();

                // Tell the cloud-backed unlock service the new chapter is available so the
                // main-menu carousel reflects it on next return. ChapterUnlockService uses
                // 0-based indices; _progress.currentChapter is 1-based and was just bumped
                // to the newly entered chapter, so subtract 1.
                ServiceLocator.Get<ChapterUnlockService>()
                    ?.UnlockChapterAsync(_progress.currentChapter - 1).Forget();

                // Play the chapter-end animation first; trigger the chapter-start
                // spin only after the transition signals it has finished.
                Action onTransitionDone = null;
                onTransitionDone = () =>
                {
                    GameEvents.OnChapterTransitionFinished -= onTransitionDone;
                    TriggerSpin(0);
                };
                GameEvents.OnChapterTransitionFinished += onTransitionDone;
                GameEvents.FireChapterCompleted(_progress.currentChapter);
            }

            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }

        public void CompleteFinalChapterLevel(int scoreAchieved)
        {
            if (_progress == null) _progress = new GameProgress();

            _progress.totalScore += scoreAchieved;
            _progress.MarkFirstTimeClear(_progress.currentChapter, _progress.currentLevel);

            int completedLevel = _progress.currentLevel;

            var chap = _progress.GetOrCreateChapter(_progress.currentChapter);
            chap.highestLevelReached = Mathf.Max(chap.highestLevelReached, completedLevel);
            if (completedLevel >= 20) chap.cleared = true;

            int chapScore = ScoreManager.Instance != null ? ScoreManager.Instance.ChapterScore : 0;
            if (chapScore > chap.highScore) chap.highScore = chapScore;
            if (chapScore > _progress.highScore) _progress.highScore = chapScore;
            ScoreManager.Instance?.ResetChapterScore();

            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }

        public void TriggerSpin(int slotIndex)
        {
            _currentSpinSlotIndex = slotIndex;

            var options = _progress.GetAvailablePowerUpForSpin(
                _progress.currentChapter, slotIndex, allPowerups, maxOptions: 3);

            for (int i = 0; i < options.Length; i++)

            OnSpinTriggered?.Invoke(slotIndex, options);
        }


        public void PlayerSelectedPowerup(PowerupConfig powerup)
        {
            if (powerup == null)
            {
                return;
            }


            LastSelectedPowerup = powerup;
            _progress.EquipPowerup(powerup, _currentSpinSlotIndex);  // ← pass slot
            _progress.playerSpins++;
            SaveProgress();


            for (int i = 0; i < _progress.chapterSlots.Length; i++)
            {
                var slot = _progress.chapterSlots[i];
            }
        }

        public void ClearLastSelectedPowerup()
        {
            LastSelectedPowerup = null;
        }


        public void ApplyPowerUpsToCurrentCannon(GameObject cannon)
        {

            if (cannon == null)
            {
                return;
            }

            if (CannonPowerUpCaster.Instance == null)
            {
                return;
            }

            CannonPowerUpCaster.Instance.UnequipAll();

            int appliedCount = 0;
            for (int i = 0; i < _progress.chapterSlots.Length; i++)
            {
                var slot = _progress.chapterSlots[i];

                if (string.IsNullOrEmpty(slot?.equippedPowerupId))
                {
                    continue;
                }

                if (slot.isOnCooldown)
                {
                    continue;
                }

                // Determine if this powerup should persist across levels.
                // Persistent = (config.cooldown > 0) OR (any effect implements IProjectileModifier).
                // Non-persistent (one-shot) powerups apply only on the level immediately after selection.
                PowerupConfig config = null;
                if (allPowerups != null)
                {
                    for (int p = 0; p < allPowerups.Length; p++)
                    {
                        if (allPowerups[p] != null && allPowerups[p].id == slot.equippedPowerupId)
                        {
                            config = allPowerups[p];
                            break;
                        }
                    }
                }
                bool hasCooldown = config != null && config.cooldown > 0f;

                // Pre-Boss Recovery is deferred: it must stay equipped (pending) every level
                // until the boss spawns, so it is never flagged hasBeenApplied on equip. The
                // OnPreBossRecoveryActivated handler consumes the slot once the heal fires.
                bool isPreBoss = config != null && config.effectType == PreBossHealModifier.ConfigEffectType;

                // Power Surge persists across all 5 chapter levels after its spin. Same
                // treatment as Pre-Boss: never flag hasBeenApplied so ApplyEquippedSlots
                // re-equips it every level. ResetSlotsForNewChapter clears it on chapter
                // rollover, which is the natural "expires when player gets a new spin".
                bool isPowerSurge = config != null && config.id == PowerSurgeId;

                var runtimePowerUp = CannonPowerUpCaster.Instance.FindByID(slot.equippedPowerupId);

                if (runtimePowerUp == null)
                {
                    continue;
                }

                bool hasProjectileModifier = false;
                if (runtimePowerUp.effects != null)
                {
                    for (int e = 0; e < runtimePowerUp.effects.Count; e++)
                    {
                        if (runtimePowerUp.effects[e] is IProjectileModifier)
                        {
                            hasProjectileModifier = true;
                            break;
                        }
                    }
                }

                bool isPersistent = hasCooldown || hasProjectileModifier;

                if (!isPersistent && slot.hasBeenApplied)
                {
                    continue;
                }

                CannonPowerUpCaster.Instance.Equip(runtimePowerUp);
                appliedCount++;
                if (!isPersistent && !isPreBoss && !isPowerSurge) slot.hasBeenApplied = true;
            }

        }


        public void ResetProgress()
        {
            _progress = new GameProgress();
            _progressLoaded = true;
            LastSelectedPowerup = null;
            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }


        [Serializable]
        private class SaveData
        {
            public int currentChapter;
            public int currentLevel;
            public int highScore;
            public int totalScore;
            public string[] slotPowerupIds = new string[4];
            public List<string> globalUnlocked = new();
            public int playerXP;
            public int playerLevel;
            public int lastLevelUpXP;
            public int playerSpins;
            public int totalCoins;
            public int totalGems;
            public int totalPower;
            public List<string> firstTimeClearedLevels = new();
            public List<ChapterProgress> chapters = new();
        }

        public async void SaveProgress()
        {
            if (_progress == null) return;
            if (!_progressLoaded)
            {
                Debug.LogWarning("[ProgressManager] Save skipped — progress was never successfully loaded this session; refusing to overwrite cloud data.");
                return;
            }

            try
            {
                var data = new SaveData
                {
                    currentChapter = _progress.currentChapter,
                    currentLevel = _progress.currentLevel,
                    highScore = _progress.highScore,
                    totalScore = _progress.totalScore,
                    globalUnlocked = _progress.globalUnlockedPowerupIds ?? new(),
                    playerXP = _progress.playerXP,
                    playerLevel = _progress.playerLevel,
                    lastLevelUpXP = _progress.lastLevelUpXP,
                    playerSpins = _progress.playerSpins,
                    totalCoins = _progress.totalCoins,
                    totalGems = _progress.totalGems,
                    totalPower = _progress.totalPower,
                    firstTimeClearedLevels = _progress.firstTimeClearedLevels ?? new(),
                    chapters = _progress.chapters ?? new()
                };

                if (_progress.chapterSlots != null)
                {
                    int slotCount = Mathf.Min(data.slotPowerupIds.Length, _progress.chapterSlots.Length);
                    for (int i = 0; i < slotCount; i++)
                        data.slotPowerupIds[i] = _progress.chapterSlots[i]?.equippedPowerupId ?? "";
                }

                await CloudSaveManager.Instance.SaveAsync(new Dictionary<string, object>
                {
                    { "chapter_progress",          data.currentChapter },
                    { "level_progress",            data.currentLevel },
                    { "high_score",                data.highScore },
                    { "total_score",               data.totalScore },
                    { "global_unlocked",           JsonConvert.SerializeObject(data.globalUnlocked) },
                    { "player_xp",                 data.playerXP },
                    { "player_level",              data.playerLevel },
                    { "last_level_up_xp",          data.lastLevelUpXP },
                    { "player_spins",              data.playerSpins },
                    { "total_coins",               data.totalCoins },
                    { "total_gems",                data.totalGems },
                    { "total_power",               data.totalPower },
                    { "slot_powerup_ids",          JsonConvert.SerializeObject(data.slotPowerupIds) },
                    { "first_time_cleared_levels", JsonConvert.SerializeObject(data.firstTimeClearedLevels) },
                    { "chapter_progress_data",     JsonConvert.SerializeObject(data.chapters) },
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[ProgressManager] Save error: " + ex.Message);
            }
        }

        public async UniTask LoadProgress()
        {
            try
            {
                var res = await CloudSaveManager.Instance.LoadAsync(new HashSet<string>
                {
                    "chapter_progress", "level_progress", "high_score", "total_score",
                    "global_unlocked", "player_xp", "player_level", "last_level_up_xp",
                    "player_spins", "total_coins", "total_gems", "total_power",
                    "slot_powerup_ids", "first_time_cleared_levels", "chapter_progress_data"
                });

                if (CloudSaveManager.Instance.LastLoadFailed)
                {
                    Debug.LogError("[ProgressManager] Cloud load failed — keeping in-memory progress; saving stays disabled until a successful load.");
                    if (_progress == null) _progress = new GameProgress();
                    OnProgressChanged?.Invoke(_progress);
                    return;
                }

                if (res.Count == 0)
                {
                    _progress = new GameProgress();
                    _progressLoaded = true;
                    OnProgressChanged?.Invoke(_progress);
                    return;
                }

                T Get<T>(string key, T fallback = default)
                {
                    if (!res.TryGetValue(key, out Item item)) return fallback;
                    try { return item.Value.GetAs<T>(); }
                    catch
                    {
                        try { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item.Value)); }
                        catch { return fallback; }
                    }
                }

                var data = new SaveData
                {
                    currentChapter = Get("chapter_progress", 1),
                    currentLevel = Get("level_progress", 1),
                    highScore = Get<int>("high_score"),
                    totalScore = Get<int>("total_score"),
                    playerXP = Get<int>("player_xp"),
                    playerLevel = Get("player_level", 1),
                    lastLevelUpXP = Get<int>("last_level_up_xp"),
                    playerSpins = Get<int>("player_spins"),
                    totalCoins = Get<int>("total_coins"),
                    totalGems = Get<int>("total_gems"),
                    totalPower = Get<int>("total_power"),
                    globalUnlocked = JsonConvert.DeserializeObject<List<string>>(Get("global_unlocked", "[]")) ?? new(),
                    slotPowerupIds = JsonConvert.DeserializeObject<string[]>(Get("slot_powerup_ids", "[\"\",\"\",\"\",\"\"]")) ?? new string[4],
                    firstTimeClearedLevels = JsonConvert.DeserializeObject<List<string>>(Get("first_time_cleared_levels", "[]")) ?? new(),
                    chapters = JsonConvert.DeserializeObject<List<ChapterProgress>>(Get("chapter_progress_data", "[]")) ?? new(),
                };

                // Always resume the chapter at level 1 — level progress within a chapter
                // is not carried across sessions (matches pre-revert behaviour).
                _progress = new GameProgress
                {
                    currentChapter = data.currentChapter,
                    currentLevel = 1,
                    highScore = data.highScore,
                    totalScore = data.totalScore,
                    globalUnlockedPowerupIds = data.globalUnlocked,
                    playerXP = data.playerXP,
                    playerLevel = Mathf.Max(1, data.playerLevel),
                    lastLevelUpXP = data.lastLevelUpXP,
                    playerSpins = data.playerSpins,
                    totalCoins = data.totalCoins,
                    totalGems = data.totalGems,
                    totalPower = data.totalPower,
                    firstTimeClearedLevels = data.firstTimeClearedLevels,
                    chapters = data.chapters ?? new()
                };

                MigrateLegacyChapterProgress();

                if (data.slotPowerupIds != null && _progress.chapterSlots != null)
                {
                    int slotCount = Mathf.Min(data.slotPowerupIds.Length, _progress.chapterSlots.Length);
                    for (int i = 0; i < slotCount; i++)
                        if (!string.IsNullOrEmpty(data.slotPowerupIds[i]) && _progress.chapterSlots[i] != null)
                            _progress.chapterSlots[i].equippedPowerupId = data.slotPowerupIds[i];
                }

                _progressLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[ProgressManager] Load error: " + ex.Message);
                if (_progress == null) _progress = new GameProgress();
            }

            OnProgressChanged?.Invoke(_progress);
        }

        private void MigrateLegacyChapterProgress()
        {
            if (_progress == null) return;
            if (_progress.chapters != null && _progress.chapters.Count > 0) return;
            _progress.chapters ??= new();

            if (_progress.firstTimeClearedLevels != null)
            {
                foreach (var key in _progress.firstTimeClearedLevels)
                {
                    var m = System.Text.RegularExpressions.Regex.Match(key, @"Ch(\d+)_L(\d+)");
                    if (!m.Success) continue;
                    int c = int.Parse(m.Groups[1].Value);
                    int l = int.Parse(m.Groups[2].Value);
                    var cp = _progress.GetOrCreateChapter(c);
                    if (!cp.firstClearedLevels.Contains(l)) cp.firstClearedLevels.Add(l);
                    cp.highestLevelReached = Mathf.Max(cp.highestLevelReached, l);
                    if (l >= 20) cp.cleared = true;
                }
            }

            if (_progress.highScore > 0)
            {
                var cur = _progress.GetOrCreateChapter(_progress.currentChapter);
                cur.highScore = Mathf.Max(cur.highScore, _progress.highScore);
            }
        }
    }
}