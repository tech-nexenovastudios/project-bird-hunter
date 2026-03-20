using Gameplay.Eggs;
using UnityEngine;

namespace Gameplay.Birds
{
    /// <summary>
    /// Boss-specific configuration. Assign to a BirdConfig.birdId = "BOSS".
    /// Supports two-phase HP transitions and a special attack pool.
    /// </summary>
    [CreateAssetMenu(menuName = "BirdHunter/Boss Config")]
    public class BossConfig : ScriptableObject
    {
        [Header("Identity")]
        public string bossId;                   // e.g. "BOSS_CH1"
        public GameObject bossPrefab;

        [Header("HP & Phases")]
        [Tooltip("Total HP for the boss.")]
        public int totalHp = 500;

        [Tooltip("At this HP fraction (0-1) the boss enters phase 2.")]
        [Range(0f, 1f)]
        public float phase2Threshold = 0.5f;

        [Header("Phase 1 Behaviour")]
        public float phase1MoveSpeed   = 1.5f;
        public float phase1LayInterval = 2.5f;   // seconds between egg drops
        public EggTierConfig phase1EggTier;

        [Header("Phase 2 Behaviour")]
        public float phase2MoveSpeed   = 2.8f;
        public float phase2LayInterval = 1.4f;   // faster in phase 2
        public EggTierConfig phase2EggTier;       // can drop heavier tier

        [Header("Special Attacks")]
        [Tooltip("How often (seconds) the boss triggers a special attack.")]
        public float specialAttackInterval = 8f;
        [Tooltip("How many eggs are dropped in one burst.")]
        public int   burstEggCount = 3;

        [Header("Score & Reward")]
        public int scoreOnDefeat = 500;

        [Header("Spawn Points Override")]
        [Tooltip("If set, boss always spawns at this world position. Leave empty to use normal spawn points.")]
        public Transform fixedSpawnPoint;
    }
}