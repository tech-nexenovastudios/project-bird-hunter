using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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

        GameProgress _progress;

        public static event Action<int, PowerupConfig[]> OnSpinTriggered;
        public static event Action<GameProgress>         OnProgressChanged;
        public static event Action<LevelProfile>         OnLevelLoaded;

        public GameProgress Data => _progress;

        public int CurrentChapter => _progress?.currentChapter ?? 1;
        public int CurrentLevel   => _progress?.currentLevel   ?? 1;
        public int GlobalLevel    => (CurrentChapter - 1) * 20 + CurrentLevel;
        public int HighScore      => _progress?.highScore      ?? 0;
        public int TotalScore     => _progress?.totalScore     ?? 0;
        public PowerupSlot[] CurrentSlots => _progress?.chapterSlots ?? new PowerupSlot[0];

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => LoadProgress();

        // ─────────────────────────────────────────────
        // Level Loading
        // ─────────────────────────────────────────────

        public LevelProfile GetCurrentLevelProfile()
            => LoadLevelProfile(CurrentChapter, CurrentLevel);

        public LevelProfile LoadLevelProfile(int chapter, int level)
        {
            string path    = $"{levelResourcesPath}/Chapter{chapter}/Levels/Ch{chapter}_L{level:D2}";
            var    profile = Resources.Load<LevelProfile>(path);

            if (profile == null) Debug.LogWarning($"❌ LevelProfile missing: {path}");
            else OnLevelLoaded?.Invoke(profile);

            return profile;
        }

        // ─────────────────────────────────────────────
        // Level Completion
        // ─────────────────────────────────────────────

        public void CompleteLevel(int scoreAchieved)
        {
            if (_progress == null) _progress = new GameProgress();

            _progress.highScore  = Mathf.Max(_progress.highScore, scoreAchieved);
            _progress.totalScore += scoreAchieved;

            int levelIndex = _progress.currentLevel - 1; // 0-based index of CURRENT level

            // Check spin BEFORE advancing (spin belongs to the level just finished)
            bool spinTriggered = false;
            if (_progress.IsSpinLevel(levelIndex))
            {
                int slotIndex = _progress.GetSpinSlotIndex(levelIndex);
                if (slotIndex >= 0)
                {
                    TriggerSpin(slotIndex);
                    spinTriggered = true;
                }
            }

            // Advance level
            if (_progress.currentLevel < 20)
            {
                _progress.currentLevel++;
            }
            else
            {
                // Chapter complete — advance to next chapter, reset slots
                _progress.currentChapter++;
                _progress.currentLevel = 1;
                _progress.ResetSlotsForNewChapter();

                // New chapter always gets a spin on L1 (slot 0)
                // Only trigger if not already triggered above (shouldn't overlap but guard anyway)
                if (!spinTriggered)
                {
                    TriggerSpin(0); // slot 0 = chapter start spin
                    spinTriggered = true;
                }
            }

            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }


        public void TriggerSpin(int slotIndex)
        {
            var options = _progress.GetAvailablePowerUpForSpin(
                _progress.currentChapter, slotIndex, allPowerups, maxOptions: 3);
            Debug.Log($"🎰 Spin! Slot {slotIndex} | Options: {options.Length}");
            OnSpinTriggered?.Invoke(slotIndex, options);
        }

        public void PlayerSelectedPowerup(PowerupConfig powerup)
        {
            if (powerup == null) return;
            _progress.EquipPowerup(powerup);
            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
            Debug.Log($"✅ Equipped [{powerup.rarity}] {powerup.displayName}");
        }

        public void ApplyPowerUpsToCurrentCannon(GameObject cannon)
        {
            foreach (var slot in _progress.chapterSlots)
            {
                if (string.IsNullOrEmpty(slot.equippedPowerupId) || slot.isOnCooldown) continue;
                var powerup = Array.Find(allPowerups, p => p.id == slot.equippedPowerupId);
                // powerup?.ApplyEffect(cannon);
            }
        }

        public void ResetProgress()
        {
            _progress = new GameProgress();
            SaveProgress();
            OnProgressChanged?.Invoke(_progress);
        }

        // ─────────────────────────────────────────────
        // Save / Load
        // ─────────────────────────────────────────────

        [Serializable]
        class SaveData
        {
            public int            currentChapter;
            public int            currentLevel;
            public int            highScore;
            public int            totalScore;
            public string[]       slotPowerupIds = new string[4];
            public List<string>   globalUnlocked = new();
            public int            playerXP;
            public int            playerLevel;
            public int            lastLevelUpXP;
        }

        public void SaveProgress()
        {
            if (_progress == null) return;

            var data = new SaveData
            {
                currentChapter = _progress.currentChapter,
                currentLevel   = _progress.currentLevel,
                highScore      = _progress.highScore,
                totalScore     = _progress.totalScore,
                globalUnlocked = _progress.globalUnlockedPowerupIds ?? new(),
                playerXP       = _progress.playerXP,
                playerLevel    = _progress.playerLevel,
                lastLevelUpXP  = _progress.lastLevelUpXP
            };

            for (int i = 0; i < 4; i++)
                data.slotPowerupIds[i] = _progress.chapterSlots[i]?.equippedPowerupId ?? "";

            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            try
            {
                var bf = new BinaryFormatter();
                using var fs = new FileStream(path, FileMode.Create);
                bf.Serialize(fs, data);
                Debug.Log($"💾 Saved: Ch{data.currentChapter} L{data.currentLevel}");
            }
            catch (Exception ex) { Debug.LogError("Save error: " + ex.Message); }
        }

        public void LoadProgress()
        {
            string path = Path.Combine(Application.persistentDataPath, saveFileName);

            if (!File.Exists(path))
            {
                _progress = new GameProgress();
                OnProgressChanged?.Invoke(_progress);
                return;
            }

            try
            {
                var bf = new BinaryFormatter();
                using var fs   = new FileStream(path, FileMode.Open);
                var        data = (SaveData)bf.Deserialize(fs);

                _progress = new GameProgress
                {
                    currentChapter           = data.currentChapter,
                    currentLevel             = data.currentLevel,
                    highScore                = data.highScore,
                    totalScore               = data.totalScore,
                    globalUnlockedPowerupIds = data.globalUnlocked ?? new(),
                    playerXP                 = data.playerXP,
                    playerLevel              = Mathf.Max(1, data.playerLevel),
                    lastLevelUpXP            = data.lastLevelUpXP
                };

                for (int i = 0; i < 4; i++)
                    if (!string.IsNullOrEmpty(data.slotPowerupIds[i]))
                        _progress.chapterSlots[i].equippedPowerupId = data.slotPowerupIds[i];

                Debug.Log($"📂 Loaded: Ch{data.currentChapter} L{data.currentLevel}");
            }
            catch (Exception ex)
            {
                Debug.LogError("Load error: " + ex.Message);
                _progress = new GameProgress();
            }

            OnProgressChanged?.Invoke(_progress);
        }
    }
}
