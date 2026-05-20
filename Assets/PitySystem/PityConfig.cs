using UnityEngine;
using System.Collections.Generic;

namespace PitySystem
{
    /// <summary>
    /// Designer-tunable pity settings. Create via
    /// Assets > Create > Pity System > Pity Config.
    /// </summary>
    [CreateAssetMenu(fileName = "PityConfig", menuName = "Pity System/Pity Config")]
    public class PityConfig : ScriptableObject
    {
        [Header("Target")]
        [Tooltip("The rarity the pity system protects against bad luck for.")]
        public Rarity pityRarity = Rarity.Legendary;

        [Header("Hard Pity")]
        [Tooltip("Guaranteed pity-rarity drop on this pull if not hit before.")]
        [Min(1)] public int hardPity = 90;

        [Header("Soft Pity")]
        [Tooltip("Pull count at which the rate starts ramping toward 100%.")]
        [Min(1)] public int softPityStart = 74;

        [Tooltip("Base chance per pull (0-1) before soft pity kicks in.")]
        [Range(0f, 1f)] public float baseRate = 0.006f;

        [Tooltip("Extra chance added per pull once past softPityStart (0-1).")]
        [Range(0f, 1f)] public float softPityIncrement = 0.06f;

        [Header("Loot Pool")]
        public List<LootItem> items = new List<LootItem>();

        /// <summary>Effective drop chance (0-1) for the pity rarity at a given pull count.</summary>
        public float GetRate(int pullsSinceLast)
        {
            int currentPull = pullsSinceLast + 1; // the pull about to happen
            if (currentPull >= hardPity) return 1f;
            if (currentPull < softPityStart) return baseRate;
            int rampSteps = currentPull - softPityStart + 1;
            return Mathf.Clamp01(baseRate + rampSteps * softPityIncrement);
        }
    }
}
