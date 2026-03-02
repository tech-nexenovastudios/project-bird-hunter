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
    public int currentLevel = 1;
    public int highScore = 0;
    public int totalScore = 0;

    // XP / Player level (meta-progression)
    public int playerXP;
    public int playerLevel = 1;
    public int lastLevelUpXP;  // XP threshold for current level

    // 4 slots per chapter reset on each chapter start
    public PowerupSlot[] chapterSlots = new PowerupSlot[4]
    {
        new (0),
        new (1),
        new (2),
        new (3)
    };

    // All powerup IDs ever unlocked across the entire playthrough (for unlocked tracking)
    public List<string> globalUnlockedPowerupIds = new ();

    // Fixed spin levels per chapter (0-indexed level within chapter)
    private static readonly int[] SpinLevels = { 0, 6, 11, 16 };

    // Map spin level → slot index
    private static readonly Dictionary<int, int> SpinLevelToSlot = new Dictionary<int, int>
    {
        { 0, 0 }, { 6, 1 }, { 11, 2 }, { 16, 3 }
    };

    public bool IsSpinLevel(int levelInChapter)
    {
        return Array.IndexOf(SpinLevels, levelInChapter) >= 0;
    }

    public int GetSpinSlotIndex(int levelInChapter)
    {
        return SpinLevelToSlot.GetValueOrDefault(levelInChapter, -1);
    }

    /// <summary>
    /// Returns spin candidates for a given slot based on:
    /// - Matching spinUnlockLevel
    /// - unlockFromChapter <= currentChapter
    /// - Not already equipped in any slot this chapter
    /// </summary>
    public PowerupConfig[] GetAvailablePowerUpForSpin(int chapter, int slotIndex, PowerupConfig[] allPowerups, int maxOptions = 3)
    {
        // Get spin level for this slot
        int spinLevel = SpinLevels[slotIndex];

        // IDs already in slots this chapter
        var equippedThisChapter = new HashSet<string>(
            chapterSlots
                .Where(s => !s.IsEmpty)
                .Select(s => s.equippedPowerupId)
        );
        Debug.Log($"equippedThisChapter: {equippedThisChapter.Count}");

        // Filter eligible powerups
        var candidates = allPowerups
            .Where(p =>
                p != null &&
                p.spinUnlockLevel == spinLevel &&
                p.unlockFromChapter <= chapter &&
                !equippedThisChapter.Contains(p.id)
            )
            .ToList();
        Debug.Log($"candidates: {candidates.Count}");

        // Shuffle with weighted rarity
        candidates = WeightedShuffle(candidates);

        Debug.Log($"candidates (shuffled): {candidates.Count}");
        
        return candidates.Take(maxOptions).ToArray();
    }

    List<PowerupConfig> WeightedShuffle(List<PowerupConfig> candidates)
    {
        // Higher rarity = lower weight (rarer = less likely to appear)
        var rarityWeights = new Dictionary<PowerupRarity, float>
        {
            { PowerupRarity.Common, 4.0f },
            { PowerupRarity.Rare, 3.0f },
            { PowerupRarity.Epic, 2.0f },
            { PowerupRarity.Legendary, 1.0f }
        };

        List<(PowerupConfig config, float weight)> weighted = candidates
            .Select(p => (p, rarityWeights[p.rarity]))
            .ToList();

        var result = new List<PowerupConfig>();
        var rng = new System.Random();

        while (weighted.Count > 0)
        {
            var totalWeight = weighted.Sum(w => w.weight);
            var roll = (float)(rng.NextDouble() * totalWeight);
            var cumulative = 0f;

            for (int i = 0; i < weighted.Count; i++)
            {
                cumulative += weighted[i].weight;
                if (!(roll <= cumulative)) continue;
                result.Add(weighted[i].config);
                weighted.RemoveAt(i);
                break;
            }
        }

        return result;
    }

    public void EquipPowerup(PowerupConfig powerup)
    {
        var slotIndex = GetSpinSlotIndex(currentLevel - 1 );
        
        if (slotIndex < 0 || slotIndex > 3 || powerup == null) return;

        chapterSlots[slotIndex].equippedPowerupId = powerup.id;
        chapterSlots[slotIndex].isOnCooldown = false;
        chapterSlots[slotIndex].cooldownRemaining = 0f;

        // Track global unlock
        if (!globalUnlockedPowerupIds.Contains(powerup.id))
            globalUnlockedPowerupIds.Add(powerup.id);
    }

    public void ResetSlotsForNewChapter()
    {
        chapterSlots = new PowerupSlot[4]
        {
            new (0),
            new (1),
            new (2),
            new (3)
        };
    }
}
}