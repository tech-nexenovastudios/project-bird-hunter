using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Abilities;
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
        public int playerXP        = 0;
        public int playerLevel     = 1;
        public int lastLevelUpXP   = 0;

        // 4 powerup slots per chapter, reset on chapter start
        public PowerupSlot[] chapterSlots = new PowerupSlot[4]
        {
            new(0), new(1), new(2), new(3)
        };

        public List<string> globalUnlockedPowerupIds = new();

        private static readonly int[] SpinLevels = { 0, 6, 11, 16 };
        private static readonly Dictionary<int, int> SpinLevelToSlot = new()
        {
            { 0, 0 }, { 6, 1 }, { 11, 2 }, { 16, 3 }
        };

        public bool IsSpinLevel(int levelInChapter)
            => Array.IndexOf(SpinLevels, levelInChapter) >= 0;

        public int GetSpinSlotIndex(int levelInChapter)
            => SpinLevelToSlot.GetValueOrDefault(levelInChapter, -1);

        public PowerupConfig[] GetAvailablePowerUpForSpin(int chapter, int slotIndex, PowerupConfig[] allPowerups, int maxOptions = 3)
        {
            int spinLevel = SpinLevels[slotIndex];

            var equippedThisChapter = new HashSet<string>(
                chapterSlots.Where(s => !s.IsEmpty).Select(s => s.equippedPowerupId)
            );

            var candidates = allPowerups
                .Where(p => p != null
                         && p.spinUnlockLevel == spinLevel
                         && p.unlockFromChapter <= chapter
                         && !equippedThisChapter.Contains(p.id))
                .ToList();

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

        public void EquipPowerup(PowerupConfig powerup)
        {
            int slotIndex = GetSpinSlotIndex(currentLevel - 1);
            if (slotIndex < 0 || slotIndex > 3 || powerup == null) return;

            chapterSlots[slotIndex].equippedPowerupId  = powerup.id;
            chapterSlots[slotIndex].isOnCooldown       = false;
            chapterSlots[slotIndex].cooldownRemaining  = 0f;

            if (!globalUnlockedPowerupIds.Contains(powerup.id))
                globalUnlockedPowerupIds.Add(powerup.id);
        }

        public void ResetSlotsForNewChapter()
        {
            chapterSlots = new PowerupSlot[4] { new(0), new(1), new(2), new(3) };
        }
    }
}
