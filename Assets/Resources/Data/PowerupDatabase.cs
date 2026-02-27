using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Gameplay.PowerUps
{
    [CreateAssetMenu(menuName = "BirdHunter/Powerup Database")]
    public class PowerupDatabase : ScriptableObject
    {
        [Header("Master Collection")]
        [Tooltip("Drop all 29 PowerupConfig assets here.")]
        public List<PowerupConfig> allPowerups = new List<PowerupConfig>();

        /// <summary>
        /// Finds a powerup by its unique ID string.
        /// </summary>
        public PowerupConfig GetPowerupById(string id)
        {
            return allPowerups.FirstOrDefault(p => p.id == id);
        }

        // This allows the Editor script to inject the list
        public void SetPowerups(List<PowerupConfig> newList)
        {
            allPowerups = newList;
        }
        /// <summary>
        /// Filters powerups based on rarity.
        /// </summary>
        public List<PowerupConfig> GetByRarity(PowerupRarity rarity)
        {
            return allPowerups.Where(p => p.rarity == rarity).ToList();
        }

        /// <summary>
        /// Returns powerups that are unlocked for a specific chapter and level.
        /// </summary>
        public List<PowerupConfig> GetAvailablePowerups(int currentChapter, int chapterLevel)
        {
            return allPowerups.Where(p => 
                p.unlockFromChapter <= currentChapter && 
                p.spinUnlockLevel <= chapterLevel
            ).ToList();
        }
    }
}