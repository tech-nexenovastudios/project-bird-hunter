using Gameplay.Birds;
using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "BirdHunter/Level Profile")]
    public class LevelProfile : ScriptableObject
    {
        public int chapter;
        public int globalLevel;           // 1–600
        public int targetScore;
        public float minDuration = 45f;
        public float maxDuration = 90f;
        public int pressureMax;
        public int pressureAvg;
        public int maxE4;
        public int maxE3;
        public int maxE2;
        public float spawnIntervalMin;
        public float spawnIntervalMax;
        public int expectedDps;
        public float hpMultiplier = 1f;

        [Header("Boss")]
        public bool isBossLevel;

        // ── CHANGED: was BossConfig (old system), now points directly to BossBirdConfig ──
        // Leave this null on non-boss levels. SpawnController reads it at spawn time.
        // The actual boss prefab lives inside BossBirdConfig, so no separate BossConfig needed.
        public BossBirdConfig bossBirdConfig;

        // How many seconds after level start before the boss appears.
        // Gives the player time to clear a first wave of normal birds first.
        [Tooltip("Seconds after level start before boss spawns. Only used when isBossLevel = true.")]
        public float bossSpawnDelay = 12f;

        // ── REMOVED: bossConfig (BossConfig) — replaced by bossBirdConfig above ──
        // If you have existing LevelProfile assets that had bossConfig assigned,
        // reassign them via the new bossBirdConfig field in the Inspector.

        public AttackingBirdConfig[] attackingBirdPool;
        public float attackingBirdSpawnInterval = 20f;

        [Range(0f, 1f)]
        public float attackingBirdSpawnChance = 0.6f;
    }
}