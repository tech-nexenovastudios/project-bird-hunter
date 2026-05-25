using System;
using System.Collections.Generic;
using UnityEngine;

namespace BirdHunter.Inventory
{
    [Serializable]
    public class CannonVisualEntry
    {
        public string cannonKey;
        public Sprite icon;
        public GameObject cannonPrefab;
        public GameObject bulletPrefab;
        [Tooltip("Simultaneous bullets per shot (matches the prefab's gunTips count). Used by the inventory DPS readout: DPS = damage × fire rate × bulletCount.")]
        public int bulletCount = 1;
    }

    [CreateAssetMenu(fileName = "CannonDatabase", menuName = "BirdHunter/Inventory/CannonDatabase")]
    public class CannonDatabase : ScriptableObject
    {
        public List<CannonVisualEntry> entries = new();

        public CannonVisualEntry GetEntry(string cannonKey)
        {
            foreach (var e in entries)
                if (e.cannonKey == cannonKey) return e;
            return null;
        }
    }
}
