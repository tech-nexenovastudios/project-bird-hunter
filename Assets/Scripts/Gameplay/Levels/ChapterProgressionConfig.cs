using Gameplay.Birds;
using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "BirdHunter/Chapter Progression Config")]
    public class ChapterProgressionConfig : ScriptableObject
    {
        [Header("Identity")]
        public int chapter;
        public int totalLevels = 20;

        [Header("Pressure")]
        [Tooltip("Constant across all levels in this chapter.")]
        public int pressureMax = 20;
        [Tooltip("Pressure average at level 1.")]
        public float pressureAvgStart = 6f;
        [Tooltip("Increment added to pressureAvg per level.")]
        public float pressureAvgStep = 0.5f;
        [Tooltip("Absolute cap for pressureAvg. Defaults to 0.9 * pressureMax when <= 0.")]
        public float pressureAvgCap = 0f;

        [Header("Target Score (exponential min→max across the chapter)")]
        public int targetScoreMin = 500;
        public int targetScoreMax = 2000;

        [Header("HP Multiplier (exponential min→max across the chapter)")]
        public float hpMultMin = 1.0f;
        public float hpMultMax = 1.1f;

        [Header("Spawn Interval (lerped early→late across the chapter)")]
        [Tooltip("Center spawn interval at level 1.")]
        public float spawnIntervalEarly = 4.0f;
        [Tooltip("Center spawn interval at the final level of the chapter.")]
        public float spawnIntervalLate = 1.0f;
        [Tooltip("± jitter applied to the per-level center, producing [min,max] used at spawn.")]
        public float spawnIntervalJitter = 0.25f;

        [Header("Duration (lerped across the chapter)")]
        public float minDurationStart = 30f;
        public float minDurationEnd = 45f;
        public float maxDurationStart = 55f;
        public float maxDurationEnd = 80f;

        [Header("Egg Tier Caps (lerped across the chapter)")]
        public int maxE4Start, maxE4End;
        public int maxE3Start = 1, maxE3End = 2;
        public int maxE2Start = 2, maxE2End = 3;
        [Tooltip("Concurrent E1 (smallest egg) cap — the main anti-clutter lever. Split children beyond this are skipped. 0 or negative = uncapped (legacy behaviour).")]
        public int maxE1Start = 5, maxE1End = 5;

        [Header("Bird Mix Weights (lerped start→end across the chapter; auto-normalized)")]
        [Range(0f, 1f)] public float b1WeightStart = 0.55f;
        [Range(0f, 1f)] public float b1WeightEnd   = 0.35f;
        [Range(0f, 1f)] public float b2WeightStart = 0.30f;
        [Range(0f, 1f)] public float b2WeightEnd   = 0.30f;
        [Range(0f, 1f)] public float b3WeightStart = 0.12f;
        [Range(0f, 1f)] public float b3WeightEnd   = 0.22f;
        [Range(0f, 1f)] public float b4WeightStart = 0.03f;
        [Range(0f, 1f)] public float b4WeightEnd   = 0.13f;
        [Tooltip("Per-spawn random jitter applied to each bird's weight (± fraction). Adds organic variation.")]
        [Range(0f, 1f)] public float perSpawnWeightJitter = 0.25f;

        [Header("Live Pressure Variance")]
        [Tooltip("Live pressureMax oscillates by this fraction (e.g. 0.15 = ±15%) using perlin noise. Capped at 0.20 so dips never starve the screen.")]
        [Range(0f, 0.20f)] public float pressureVariancePercent = 0.15f;
        [Tooltip("Frequency of the pressureMax oscillation in Hz.")]
        public float pressureNoiseFrequency = 0.18f;

        [Header("Replay Difficulty (DISABLED — retries are identical; you clear by UPGRADING)")]
        [Tooltip("Inert by design. Retries play at identical difficulty — a level becomes clearable through cannon upgrades, not by handicapping egg HP. See design doc §5.4.")]
        [Range(0f, 0.25f)] public float replayHpStep = 0f;
        [Tooltip("Inert by design. See replayHpStep.")]
        [Range(0f, 0.25f)] public float replayPressureStep = 0f;
        [Tooltip("Inert by design. See replayHpStep.")]
        public int replayMaxBumps = 0;
        [Tooltip("Random jitter applied per retry bump (± fraction of the step).")]
        [Range(0f, 1f)] public float replayJitterPercent = 0.5f;

        [Header("Boss (applies at the chapter's final level only)")]
        public BossBirdConfig bossBirdConfig;
        public float bossSpawnDelay = 12f;

        [Header("Attacking Birds (chapter-wide)")]
        public AttackingBirdConfig[] attackingBirdPool;
        public float attackingBirdSpawnInterval = 20f;
        [Range(0f, 1f)] public float attackingBirdSpawnChance = 0.6f;

        [Header("Spin Levels (0-based indices within this chapter)")]
        public int[] spinLevelIndices = { 0, 4, 9, 14 };

#if UNITY_EDITOR
        void OnValidate()
        {
            totalLevels = Mathf.Max(1, totalLevels);
            pressureMax = Mathf.Max(1, pressureMax);
            pressureAvgStart = Mathf.Max(0f, pressureAvgStart);
            pressureAvgStep = Mathf.Max(0f, pressureAvgStep);

            targetScoreMin = Mathf.Max(1, targetScoreMin);
            targetScoreMax = Mathf.Max(targetScoreMin, targetScoreMax);

            hpMultMin = Mathf.Max(0.01f, hpMultMin);
            hpMultMax = Mathf.Max(hpMultMin, hpMultMax);

            spawnIntervalEarly = Mathf.Max(0.3f, spawnIntervalEarly);
            spawnIntervalLate = Mathf.Max(0.3f, spawnIntervalLate);
            spawnIntervalJitter = Mathf.Max(0f, spawnIntervalJitter);

            minDurationStart = Mathf.Max(1f, minDurationStart);
            minDurationEnd = Mathf.Max(1f, minDurationEnd);
            maxDurationStart = Mathf.Max(minDurationStart + 1f, maxDurationStart);
            maxDurationEnd = Mathf.Max(minDurationEnd + 1f, maxDurationEnd);

            maxE4Start = Mathf.Max(0, maxE4Start);
            maxE4End = Mathf.Max(0, maxE4End);
            maxE3Start = Mathf.Max(0, maxE3Start);
            maxE3End = Mathf.Max(0, maxE3End);
            maxE2Start = Mathf.Max(0, maxE2Start);
            maxE2End = Mathf.Max(0, maxE2End);
            maxE1Start = Mathf.Max(0, maxE1Start);
            maxE1End = Mathf.Max(0, maxE1End);

            bossSpawnDelay = Mathf.Max(0f, bossSpawnDelay);
            attackingBirdSpawnInterval = Mathf.Max(0f, attackingBirdSpawnInterval);

            pressureNoiseFrequency = Mathf.Max(0f, pressureNoiseFrequency);
            replayMaxBumps = Mathf.Max(0, replayMaxBumps);
        }
#endif
    }
}
