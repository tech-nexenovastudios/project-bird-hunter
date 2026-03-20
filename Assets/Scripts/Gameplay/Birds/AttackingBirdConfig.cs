using UnityEngine;

namespace Gameplay.Birds
{
    public enum AttackingBirdType
    {
        Laser,          // fires a laser straight down
        Bomb,           // drops a bomb — AoE on impact (12.5% screen-width radius)
        Gun,            // fires bullets at 45° angles, random movement
        Arrow,          // fires arrow directly at cannon
        Suicide,        // generic bird; dives at cannon when HP ≤ 50%
        Juggernaut,     // folds wings, spins on own axis, zig-zags 45° top→bottom
        Ice,            // drops ice eggs — cannon freezes on hit
        Venom,          // throws venom packets — covers 20% of ground, DoT
        Lightning,      // strikes straight down lightning bolt, random position
        IceFrost,       // covers 20% of ground with ice — slows cannon
        SoulThrower,    // releases souls that fall on random paths
        Spinning,       // rotates on axis, dives 45° at high speed
        FireThrower,    // drops fire eggs — cannon burns for a few seconds on hit
        BeeHurdle,      // 3×3 bee formation, 30° descent, damages cannon on contact
    }
    
    [CreateAssetMenu(menuName = "BirdHunter/Attacking Bird Config")]
    public class AttackingBirdConfig : ScriptableObject
    {
        [Header("Identity")]
        public string           attackingBirdId;
        public AttackingBirdType type;
        public GameObject       prefab;
 
        [Header("Health & Movement")]
        public int   baseHp        = 60;
        public float moveSpeed     = 2.5f;
        public float lifetime      = 12f;
 
        [Header("Attack Parameters")]
        [Tooltip("Seconds between attacks / shots / drops.")]
        public float attackInterval = 3f;
 
        [Tooltip("Projectile / effect prefab (laser beam, bomb, venom packet, etc.).")]
        public GameObject projectilePrefab;
 
        [Tooltip("AoE radius as a fraction of screen width (used by Bomb, IceFrost, Venom).")]
        [Range(0f, 0.5f)]
        public float aoeScreenFraction = 0.125f;   // 12.5% default (Bomb spec)
 
        [Tooltip("Duration of status effect in seconds (freeze, burn, slow).")]
        public float statusDuration = 3f;
 
        [Tooltip("DoT damage per second while status is active.")]
        public float dotDamagePerSecond = 5f;
 
        [Header("Bee Hurdle (BeeHurdle type only)")]
        [Tooltip("Total bees in the 3×3 grid.")]
        public int   beeCount        = 9;
        [Tooltip("Descent angle in degrees from vertical.")]
        public float beeDescentAngle = 30f;
        public float beeGroupSpeed   = 3.5f;
 
        [Header("Spawn Chance")]
        [Tooltip("Relative weight when the spawn system picks from the attacking-bird pool.")]
        [Range(0f, 1f)]
        public float spawnWeight = 0.5f;
 
        [Header("Score")]
        public int scoreOnDefeat = 75;
    }
}