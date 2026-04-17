using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.Managers
{
    public class GameProgressManager : MonoBehaviour
    {
        public static GameProgressManager Instance { get; private set; }

        [Header("Powerup Pool")]
        public PowerupConfig[] allPowerups;

        [Header("Level Data")]
        public string levelResourcesPath = "Data/GeneratedLevels";

        [Header("Save")]
        public string saveFileName = "birdhunter_progress.dat";

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
            DontDestroyOnLoad(gameObject);
            //Debug.Log("[ProgressManager] Awake — Instance set.");
        }

        // ─────────────────────────────────────────────
        // Level Loading
        // ─────────────────────────────────────────────

        public LevelProfile GetCurrentLevelProfile()
            => LoadLevelProfile(CurrentChapter, CurrentLevel);

        public LevelProfile LoadLevelProfile(int chapter, int level)
        {
            string path = $"{levelResourcesPath}/Chapter{chapter}/Levels/Ch{chapter}_L{level:D2}";
            var profile = Resources.Load<LevelProfile>(path);
            if (profile == null) Debug.LogWarning($"[ProgressManager] ❌ LevelProfile missing: {path}");
            else
            {
                Debug.Log($"[ProgressManager] ✅ LevelProfile loaded: {path}");
                OnLevelLoaded?.Invoke(profile);
                GameEvents.FireGameLevelUpdated(profile, level);
            }
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
                //Debug.Log($"[ProgressManager] 🎰 Mid-chapter spin after L{completedLevel} → Slot {slotIndex}");
                TriggerSpin(slotIndex);
            }

            if (_progress.currentLevel < 20)
            {
                _progress.currentLevel++;
            }
            else
            {
                _progress.currentChapter++;
                _progress.currentLevel = 1;
                _progress.ResetSlotsForNewChapter();
                //Debug.Log($"[ProgressManager] 🎰 Chapter start spin → Ch{_progress.currentChapter}");
                TriggerSpin(0);
            }

            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }

        public void TriggerSpin(int slotIndex)
        {
            _currentSpinSlotIndex = slotIndex;
            //Debug.Log($"[ProgressManager] TriggerSpin — storing slotIndex: {_currentSpinSlotIndex}");

            var options = _progress.GetAvailablePowerUpForSpin(
                _progress.currentChapter, slotIndex, allPowerups, maxOptions: 3);

            //Debug.Log($"[ProgressManager] 🎰 TriggerSpin — Slot {slotIndex} | Options count: {options.Length}");
            for (int i = 0; i < options.Length; i++)
                //Debug.Log($"[ProgressManager]   Option[{i}]: {options[i]?.displayName ?? "NULL"} (id: {options[i]?.id ?? "NULL"})");

            OnSpinTriggered?.Invoke(slotIndex, options);
        }

        // ─────────────────────────────────────────────
        // Powerup Selection
        // ─────────────────────────────────────────────

        public void PlayerSelectedPowerup(PowerupConfig powerup)
        {
            if (powerup == null)
            {
                //Debug.LogWarning("[ProgressManager] ⚠️ PlayerSelectedPowerup called with NULL powerup.");
                return;
            }

            //Debug.Log($"[ProgressManager] PlayerSelectedPowerup — '{powerup.displayName}' (id: {powerup.id}) → slot {_currentSpinSlotIndex}");

            LastSelectedPowerup = powerup;
            _progress.EquipPowerup(powerup, _currentSpinSlotIndex);  // ← pass slot
            _progress.playerSpins++;
            SaveProgress();

            //Debug.Log($"[ProgressManager] ✅ LastSelectedPowerup set. playerSpins now: {_progress.playerSpins}");

            for (int i = 0; i < _progress.chapterSlots.Length; i++)
            {
                var slot = _progress.chapterSlots[i];
                //Debug.Log($"[ProgressManager]   Slot[{i}]: equippedPowerupId = '{slot?.equippedPowerupId ?? "empty"}' | isOnCooldown = {slot?.isOnCooldown}");
            }
        }

        public void ClearLastSelectedPowerup()
        {
            //Debug.Log($"[ProgressManager] ClearLastSelectedPowerup — was: '{LastSelectedPowerup?.displayName ?? "NULL"}'");
            LastSelectedPowerup = null;
        }

        // ─────────────────────────────────────────────
        // Apply Powerups to Cannon
        // ─────────────────────────────────────────────

        public void ApplyPowerUpsToCurrentCannon(GameObject cannon)
        {
            //Debug.Log($"[ProgressManager] ApplyPowerUpsToCurrentCannon — cannon: {cannon?.name ?? "NULL"}");

            if (cannon == null)
            {
                //Debug.LogError("[ProgressManager] ❌ Cannot apply powerups — cannon GameObject is null.");
                return;
            }

            if (CannonPowerUpCaster.Instance == null)
            {
                //Debug.LogError("[ProgressManager] ❌ CannonPowerUpCaster.Instance is null.");
                return;
            }

            CannonPowerUpCaster.Instance.UnequipAll();
            //Debug.Log("[ProgressManager] UnequipAll called — starting fresh.");

            int appliedCount = 0;
            for (int i = 0; i < _progress.chapterSlots.Length; i++)
            {
                var slot = _progress.chapterSlots[i];

                if (string.IsNullOrEmpty(slot?.equippedPowerupId))
                {
                    //Debug.Log($"[ProgressManager]   Slot[{i}]: empty — skipping.");
                    continue;
                }

                if (slot.isOnCooldown)
                {
                    //Debug.Log($"[ProgressManager]   Slot[{i}]: '{slot.equippedPowerupId}' is on cooldown — skipping.");
                    continue;
                }

                //Debug.Log($"[ProgressManager]   Slot[{i}]: looking up '{slot.equippedPowerupId}'...");
                var runtimePowerUp = CannonPowerUpCaster.Instance.FindByID(slot.equippedPowerupId);

                if (runtimePowerUp != null)
                {
                    CannonPowerUpCaster.Instance.Equip(runtimePowerUp);
                    appliedCount++;
                    //Debug.Log($"[ProgressManager]   Slot[{i}]: ✅ '{slot.equippedPowerupId}' equipped successfully.");
                }
                else
                {
                    //Debug.LogError($"[ProgressManager]   Slot[{i}]: ❌ No CannonPowerUp found for id '{slot.equippedPowerupId}'. Check hotbar assignments in Inspector.");
                }
            }

            //Debug.Log($"[ProgressManager] ApplyPowerUpsToCurrentCannon done — {appliedCount} powerup(s) applied.");
        }

        // ─────────────────────────────────────────────
        // Reset
        // ─────────────────────────────────────────────

        public void ResetProgress()
        {
            //Debug.Log("[ProgressManager] ResetProgress called.");
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

        public void SaveProgress()
        {
            if (_progress == null) return;

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

            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            try
            {
                var bf = new BinaryFormatter();
                using var fs = new FileStream(path, FileMode.Create);
                bf.Serialize(fs, data);
                //Debug.Log($"[ProgressManager] 💾 Saved: Ch{data.currentChapter} L{data.currentLevel}");
            }
            catch (Exception ex) 
            {
                Debug.LogError("[ProgressManager] Save error: " + ex.Message);
            }
        }

        public void LoadProgress()
        {
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            //Debug.Log($"[ProgressManager] LoadProgress — path: {path}");

            if (!File.Exists(path))
            {
                //Debug.Log("[ProgressManager] No save file found — starting fresh.");
                _progress = new GameProgress();
                OnProgressChanged?.Invoke(_progress);
                return;
            }

            try
            {
                var bf = new BinaryFormatter();
                using var fs = new FileStream(path, FileMode.Open);
                var data = (SaveData)bf.Deserialize(fs);

                _progress = new GameProgress
                {
                    currentChapter = data.currentChapter,
                    currentLevel = data.currentLevel,
                    highScore = data.highScore,
                    totalScore = data.totalScore,
                    globalUnlockedPowerupIds = data.globalUnlocked ?? new(),
                    playerXP = data.playerXP,
                    playerLevel = Mathf.Max(1, data.playerLevel),
                    lastLevelUpXP = data.lastLevelUpXP,
                    playerSpins = data.playerSpins,
                    totalCoins = data.totalCoins,
                    totalGems = data.totalGems,
                    totalPower = data.totalPower,
                    firstTimeClearedLevels = data.firstTimeClearedLevels ?? new()
                };

                for (int i = 0; i < 4; i++)
                    if (!string.IsNullOrEmpty(data.slotPowerupIds[i]))
                        _progress.chapterSlots[i].equippedPowerupId = data.slotPowerupIds[i];

                //Debug.Log($"[ProgressManager] 📂 Loaded: Ch{data.currentChapter} L{data.currentLevel} Spins:{data.playerSpins}");
            }
            catch (Exception ex)
            {
                //Debug.LogError("[ProgressManager] Load error: " + ex.Message);
                _progress = new GameProgress();
            }

            OnProgressChanged?.Invoke(_progress);
        }
    }
}