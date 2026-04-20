using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public partial class GameProgress
    {
        public int currentChapter = 1;
        public int currentLevel   = 1;
        public int highScore      = 0;
        public int totalScore     = 0;

        // Meta XP progression
        public int playerXP      = 0;
        public int playerLevel   = 1;
        public int lastLevelUpXP = 0;
        public int playerSpins   = 0;
        
        public int totalCoins = 0;
        public int totalGems = 0;
        public int totalPower = 0;

        public List<string> firstTimeClearedLevels = new();

        // Per-chapter play count. Feeds the PC term of the Difficulty Index.
        // Stored per-chapter so grinding early chapters can't inflate later-chapter difficulty.
        public Dictionary<int, int> chapterPlays = new();

        public int GetChapterPlays(int chapter)
            => chapterPlays.TryGetValue(chapter, out var p) ? p : 0;

        public void IncrementChapterPlays(int chapter)
        {
            chapterPlays[chapter] = GetChapterPlays(chapter) + 1;
        }

        public bool HasFirstTimeClear(int chapter, int level)
        {
            string key = $"Ch{chapter}_L{level}";
            return firstTimeClearedLevels.Contains(key);
        }

        public void MarkFirstTimeClear(int chapter, int level)
        {
            string key = $"Ch{chapter}_L{level}";
            if (!firstTimeClearedLevels.Contains(key))
            {
                firstTimeClearedLevels.Add(key);
            }
        }

        // 4 slots per chapter, reset on new chapter
        public PowerUpSlot[] chapterSlots = new PowerUpSlot[4]
        {
            new(0), new(1), new(2), new(3)
        };

        public List<string> globalUnlockedPowerupIds = new();

        // FIXED: spin after L5(idx 4), L10(idx 9), L15(idx 14)
        // Level 1 (idx 0) spin is handled by GameManager.Start() as initial spin ONLY
        private static readonly int[] SpinLevels = { 4, 9, 14 };

        private static readonly Dictionary<int, int> SpinLevelToSlot = new() { { 4,  1 }, { 9,  2 }, { 14, 3 } };

        /// <summary>
        /// Pass levelInChapter = currentLevel - 1 (0-based).
        /// Returns true if a spin should trigger after completing this level.
        /// </summary>
        public bool IsSpinLevel(int levelInChapter)
            => Array.IndexOf(SpinLevels, levelInChapter) >= 0;

        public int GetSpinSlotIndex(int levelInChapter)
            => SpinLevelToSlot.GetValueOrDefault(levelInChapter, -1);

        public PowerupConfig[] GetAvailablePowerUpForSpin(int chapter, int slotIndex, PowerupConfig[] allPowerups, int maxOptions = 3)
        {
            // Only exclude the currently equipped one (if any) — player shouldn't re-pick same powerup
            string currentlyEquippedId = null;
            for (int i = 0; i < chapterSlots.Length; i++)
            {
                if (!string.IsNullOrEmpty(chapterSlots[i]?.equippedPowerupId))
                {
                    currentlyEquippedId = chapterSlots[i].equippedPowerupId;
                    break;
                }
            }

            var candidates = allPowerups
                .Where(p => p != null
                            && p.unlockFromChapter <= chapter
                            && p.id != currentlyEquippedId)
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[GameProgress] No powerup candidates for Ch{chapter} Slot{slotIndex}!");
                return Array.Empty<PowerupConfig>();
            }

            candidates = WeightedShuffle(candidates);
            return candidates.Take(maxOptions).ToArray();
        }


        private List<PowerupConfig> WeightedShuffle(List<PowerupConfig> candidates)
        {
            var rarityWeights = new Dictionary<PowerupRarity, float>
            {
                { PowerupRarity.Common,    4.0f },
                { PowerupRarity.Rare,      3.0f },
                { PowerupRarity.Epic,      2.0f },
                { PowerupRarity.Legendary, 1.0f }
            };

            var weighted = candidates.Select(p => (p, rarityWeights[p.rarity])).ToList();
            var result   = new List<PowerupConfig>();
            var rng      = new System.Random();

            while (weighted.Count > 0)
            {
                float totalWeight = weighted.Sum(w => w.Item2);
                float roll        = (float)(rng.NextDouble() * totalWeight);
                float cumulative  = 0f;

                for (int i = 0; i < weighted.Count; i++)
                {
                    cumulative += weighted[i].Item2;
                    if (!(roll <= cumulative)) continue;
                    result.Add(weighted[i].p);
                    weighted.RemoveAt(i);
                    break;
                }
            }
            return result;
        }
        // Change signature to accept slotIndex explicitly
        public void EquipPowerup(PowerupConfig powerup, int slotIndex)
        {
            if (powerup == null || slotIndex < 0 || slotIndex > 3) return;

            // Clear ALL slots first — only one powerup active at a time
            for (int i = 0; i < chapterSlots.Length; i++)
            {
                chapterSlots[i].equippedPowerupId = null;
                chapterSlots[i].isOnCooldown = false;
                chapterSlots[i].cooldownRemaining = 0f;
            }

            // Equip new powerup in the target slot
            chapterSlots[slotIndex].equippedPowerupId = powerup.id;
            chapterSlots[slotIndex].isOnCooldown = false;
            chapterSlots[slotIndex].cooldownRemaining = 0f;

            if (!globalUnlockedPowerupIds.Contains(powerup.id))
                globalUnlockedPowerupIds.Add(powerup.id);

            Debug.Log($"[GameProgress] EquipPowerup — cleared all slots, equipped '{powerup.id}' in slot[{slotIndex}]");
        }

        public void ResetSlotsForNewChapter()
        {
            chapterSlots = new PowerUpSlot[4] { new(0), new(1), new(2), new(3) };
        }
        
        
        public void AddCoins(int amount)
        {
            if (amount > 0) totalCoins += amount;
        }

        public bool SpendCoins(int amount)
        {
            if (totalCoins >= amount)
            {
                totalCoins -= amount;
                return true;
            }
            return false;
        }

        public void AddGems(int amount)
        {
            if (amount > 0) totalGems += amount;
        }

        public bool SpendGems(int amount)
        {
            if (totalGems >= amount)
            {
                totalGems -= amount;
                return true;
            }
            return false;
        }


        public void AddPower(int amount)
        {
            if (amount > 0) totalPower += amount;
        }

        public bool SpendPower(int amount)
        {
            if (totalPower >= amount)
            {
                totalPower -= amount;
                return true;
            }
            return false;
        }

        public int GetCoins() => totalCoins;

        public int GetGems() => totalGems;

        public int GetPower() => totalPower;
    }
}
