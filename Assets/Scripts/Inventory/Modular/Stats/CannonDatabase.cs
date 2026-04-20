using System;
using BirdHunter.Inventory.Stats;
using UnityEngine;

namespace BirdHunter.Inventory
{
    [CreateAssetMenu(fileName = "CannonDatabase", menuName = "BirdHunter/Inventory/CannonDatabase")]
    public class CannonDatabase : ScriptableObject
    {
        // Works out of the box — no custom inspector needed!
        public GenericDictionary<int, Sprite> cannonSprites;
        public GenericDictionary<int, GameObject> cannonPrefabs;
        public GenericDictionary<int, GameObject> cannonBullets;
        public GenericDictionary<int, StatSheet> cannonStats;
    }
}