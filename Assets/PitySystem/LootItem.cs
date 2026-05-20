using UnityEngine;

namespace PitySystem
{
    /// <summary>Rarity tiers. Order matters: higher index = rarer.</summary>
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>A single droppable item with a base weight.</summary>
    [System.Serializable]
    public class LootItem
    {
        public string id;
        public Rarity rarity;
        [Tooltip("Relative weight within its rarity tier.")]
        public float weight = 1f;
    }
}
