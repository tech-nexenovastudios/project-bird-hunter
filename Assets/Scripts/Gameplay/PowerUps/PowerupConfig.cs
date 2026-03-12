using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.PowerUps
{
    public enum PowerupCategory { Attack, Defense, Utility }
    public enum PowerupRarity { Common, Rare, Epic, Legendary }

    [CreateAssetMenu(menuName = "BirdHunter/Powerup Config")]
    public class PowerupConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;        // e.g. "RapidFireEffect"
        
        [Tooltip("e.g. 'Rapid Fire'")]
        public string displayName;      // e.g. "Rapid Fire"
        public string description;
        public Sprite icon;

        [Header("Classification")]
        public PowerupCategory category;
        public PowerupRarity rarity;

        [Header("Spin Rules")]
        [Tooltip("Unlocks at chapter level 0, 6, 11, 16")]
        public int spinUnlockLevel;     // 0,6,11,16 (per chapter)
        
        [Tooltip("becomes available in spin pool from which chapter")]
        public int unlockFromChapter;   // becomes available in spin pool from this chapter
        public bool initiallyUnlocked;  // true = available from chapter 1

        [Header("Effect")]
        public string effectType;       // matches your effect class name
        public float baseValue;
        public float duration;
        public float cooldown;
        public bool stackable;

        [Header("Reset")]
        public string resetRule = "Resets at next Chapter Level 0";
    }
}