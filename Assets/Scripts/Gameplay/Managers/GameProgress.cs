using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public class GameProgress
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
            // IDs already equipped in slots this chapter — don't offer duplicates
            var equippedThisChapter = new HashSet<string>(
                chapterSlots.Where(s => !s.IsEmpty).Select(s => s.equippedPowerupId)
            );

            // All powerups unlocked so far = unlockFromChapter <= current chapter
            // Exclude already equipped ones this chapter
            var candidates = allPowerups
                .Where(p => p != null
                            && p.unlockFromChapter <= chapter
                            && !equippedThisChapter.Contains(p.id))
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

            // Single-active-powerup rule: selecting a new powerup displaces whatever was
            // previously equipped. The displaced powerup is NOT re-applied on later levels —
            // the player must pick it again at a future spin to bring it back.
            for (int i = 0; i < chapterSlots.Length; i++)
            {
                if (i == slotIndex) continue;
                var other = chapterSlots[i];
                if (other == null || string.IsNullOrEmpty(other.equippedPowerupId)) continue;

                Debug.Log($"[GameProgress] EquipPowerup — clearing displaced slot[{i}] = '{other.equippedPowerupId}'");
                other.equippedPowerupId = null;
                other.isOnCooldown = false;
                other.cooldownRemaining = 0f;
                other.hasBeenApplied = false;
            }

            chapterSlots[slotIndex].equippedPowerupId = powerup.id;
            chapterSlots[slotIndex].isOnCooldown = false;
            chapterSlots[slotIndex].cooldownRemaining = 0f;
            chapterSlots[slotIndex].hasBeenApplied = false;
            // NOTE: playerSpins++ removed here — GameProgressManager.PlayerSelectedPowerup handles it

            if (!globalUnlockedPowerupIds.Contains(powerup.id))
                globalUnlockedPowerupIds.Add(powerup.id);

            Debug.Log($"[GameProgress] EquipPowerup — slot[{slotIndex}] = '{powerup.id}'");
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
