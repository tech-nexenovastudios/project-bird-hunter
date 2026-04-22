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
