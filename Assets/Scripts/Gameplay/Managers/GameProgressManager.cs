using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.PowerUps;
using Newtonsoft.Json;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Gameplay.Managers
{
    public class GameProgressManager : MonoBehaviour
    {
        public static GameProgressManager Instance { get; private set; }

        [Header("Powerup Pool")]
        public PowerupConfig[] allPowerups;

        private GameProgress _progress;

        public static event Action<int, PowerupConfig[]> OnSpinTriggered;
        public static event Action<GameProgress> OnProgressChanged;
        public static event Action<LevelProfile> OnLevelLoaded;

        public GameProgress Data => _progress;
        public int CurrentChapter => _progress?.currentChapter ?? 1;
        public int CurrentLevel => _progress?.currentLevel ?? 1;
        public int GlobalLevel => (CurrentChapter - 1) * 20 + CurrentLevel;
        public int HighScore => _progress?.highScore ?? 0;
        public int TotalScore => _progress?.totalScore ?? 0;
        public PowerUpSlot[] CurrentSlots => _progress?.chapterSlots ?? new PowerUpSlot[0];

        public PowerupConfig LastSelectedPowerup { get; private set; }
        private int _currentSpinSlotIndex = 0;

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

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ─────────────────────────────────────────────
        // Level Loading
        // ─────────────────────────────────────────────

        public LevelProfile LoadLevelProfile(int index) => PersistantData.Instance.ChapterData.levelProfiles[index];
        
        public LevelProfile GetCurrentLevelProfile()
            => LoadLevelProfile(CurrentChapter, CurrentLevel);

        public LevelProfile LoadLevelProfile(int chapter, int level)
        {
            var profile = LoadLevelProfile(level);
            OnLevelLoaded?.Invoke(profile);
            GameEvents.FireGameLevelUpdated(profile, level);
            return profile;
        }

        // ─────────────────────────────────────────────
        // Level Completion
        // ─────────────────────────────────────────────

        public void CompleteLevel(int scoreAchieved)
        {
            if (_progress == null) _progress = new GameProgress();

            _progress.highScore = Mathf.Max(_progress.highScore, scoreAchieved);
            _progress.totalScore += scoreAchieved;
            _progress.MarkFirstTimeClear(_progress.currentChapter, _progress.currentLevel);

            int completedLevel = _progress.currentLevel;

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
                int completedChapter = _progress.currentChapter;
                _progress.currentChapter++;
                _progress.currentLevel = 1;
                _progress.ResetSlotsForNewChapter();
                LastSelectedPowerup = null;
                GameEvents.FirePowerupUnequipped();
                GameEvents.FireChapterCompleted(_progress.currentChapter);

                // Unlock the next chapter in cloud
                ChapterUnlockManager.Instance?.SetUnlock(_progress.currentChapter - 1, true).Forget();

                // Delay spin until transition finishes
                StartCoroutine(DelayedTriggerSpin(4.5f));
            }

            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }

        private System.Collections.IEnumerator DelayedTriggerSpin(float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerSpin(0);
        }

        public void TriggerSpin(int slotIndex)
        {
            _currentSpinSlotIndex = slotIndex;

            var options = _progress.GetAvailablePowerUpForSpin(
                _progress.currentChapter, slotIndex, allPowerups, maxOptions: 3);

            OnSpinTriggered?.Invoke(slotIndex, options);
        }

        // ─────────────────────────────────────────────
        // Powerup Selection
        // ─────────────────────────────────────────────

        public void PlayerSelectedPowerup(PowerupConfig powerup)
        {
            if (powerup == null) return;

            LastSelectedPowerup = powerup;
            _progress.EquipPowerup(powerup, _currentSpinSlotIndex);
            _progress.playerSpins++;
            SaveProgress();
        }

        public void ClearLastSelectedPowerup()
        {
            LastSelectedPowerup = null;
        }

        // ─────────────────────────────────────────────
        // Apply Powerups to Cannon
        // ─────────────────────────────────────────────

        public void ApplyPowerUpsToCurrentCannon(GameObject cannon)
        {
            if (cannon == null) return;
            if (CannonPowerUpCaster.Instance == null) return;

            CannonPowerUpCaster.Instance.UnequipAll();

            int appliedCount = 0;
            for (int i = 0; i < _progress.chapterSlots.Length; i++)
            {
                var slot = _progress.chapterSlots[i];

                if (string.IsNullOrEmpty(slot?.equippedPowerupId)) continue;
                if (slot.isOnCooldown) continue;

                var runtimePowerUp = CannonPowerUpCaster.Instance.FindByID(slot.equippedPowerupId);

                if (runtimePowerUp != null)
                {
                    CannonPowerUpCaster.Instance.Equip(runtimePowerUp);
                    appliedCount++;
                }
                else
                {
                    Debug.LogError($"[ProgressManager] Slot[{i}]: ❌ No CannonPowerUp found for id '{slot.equippedPowerupId}'.");
                }
            }

            Debug.Log($"[ProgressManager] ApplyPowerUpsToCurrentCannon done — {appliedCount} powerup(s) applied.");
        }

        // ─────────────────────────────────────────────
        // Reset
        // ─────────────────────────────────────────────
        public void ResetProgress()
        {
            _progress = new GameProgress();
            LastSelectedPowerup = null;
            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }

        // ─────────────────────────────────────────────
        // Save / Load
        // ─────────────────────────────────────────────

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
        }

        public async void SaveProgress()
        {
            if (_progress == null) return;

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
                    firstTimeClearedLevels = _progress.firstTimeClearedLevels ?? new()
                };

                for (int i = 0; i < 4; i++)
                    data.slotPowerupIds[i] = _progress.chapterSlots[i]?.equippedPowerupId ?? "";

                await CloudSaveManager.Instance.SaveAsync(new Dictionary<string, object>
                {
                    { "chapter_progress",          data.currentChapter },
                    { "level_progress",             data.currentLevel },
                    { "high_score",                 data.highScore },
                    { "total_score",                data.totalScore },
                    { "global_unlocked",            JsonConvert.SerializeObject(data.globalUnlocked) },
                    { "player_xp",                  data.playerXP },
                    { "player_level",               data.playerLevel },
                    { "last_level_up_xp",           data.lastLevelUpXP },
                    { "player_spins",               data.playerSpins },
                    { "total_coins",                data.totalCoins },
                    { "total_gems",                 data.totalGems },
                    { "total_power",                data.totalPower },
                    { "slot_powerup_ids",           JsonConvert.SerializeObject(data.slotPowerupIds) },
                    { "first_time_cleared_levels",  JsonConvert.SerializeObject(data.firstTimeClearedLevels) },
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[ProgressManager] Save error: " + ex.Message);
            }
        }

        // ── FIXED: was async void — now returns UniTask so callers can await it ──
        public async UniTask LoadProgress()
        {
            try
            {
                var res = await CloudSaveManager.Instance.LoadAsync(new HashSet<string>
                {
                    "chapter_progress", "level_progress", "high_score", "total_score",
                    "global_unlocked", "player_xp", "player_level", "last_level_up_xp",
                    "player_spins", "total_coins", "total_gems", "total_power",
                    "slot_powerup_ids", "first_time_cleared_levels"
                });

                if (res.Count == 0)
                {
                    _progress = new GameProgress();
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
                };

                _progress = new GameProgress
                {
                    currentChapter = data.currentChapter,
                    currentLevel = data.currentLevel,
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
                    firstTimeClearedLevels = data.firstTimeClearedLevels
                };

                for (int i = 0; i < 4 && i < data.slotPowerupIds.Length; i++)
                    if (!string.IsNullOrEmpty(data.slotPowerupIds[i]))
                        _progress.chapterSlots[i].equippedPowerupId = data.slotPowerupIds[i];
            }
            catch (Exception ex)
            {
                Debug.LogError("[ProgressManager] Load error: " + ex.Message);
                _progress = new GameProgress();
            }

            OnProgressChanged?.Invoke(_progress);
        }
    }
}