using UnityEngine;

namespace Gameplay.Levels
{
    public static class LevelProfileResolver
    {
        public static LevelProfile Resolve(ChapterProgressionConfig cfg, int levelIndex)
        {
            if (cfg == null) return null;

            int n = Mathf.Max(1, cfg.totalLevels);
            int clampedIndex = Mathf.Clamp(levelIndex, 0, n - 1);
            float t = n > 1 ? clampedIndex / (float)(n - 1) : 0f;

            var profile = ScriptableObject.CreateInstance<LevelProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.name = $"Ch{cfg.chapter}_L{clampedIndex + 1:D2} (runtime)";

            profile.chapter = cfg.chapter;
            profile.globalLevel = (cfg.chapter - 1) * n + (clampedIndex + 1);

            profile.pressureMax = cfg.pressureMax;
            float cap = cfg.pressureAvgCap > 0f ? cfg.pressureAvgCap : cfg.pressureMax * 0.9f;
            float rawAvg = cfg.pressureAvgStart + cfg.pressureAvgStep * clampedIndex;
            profile.pressureAvg = Mathf.RoundToInt(Mathf.Min(rawAvg, cap));

            profile.targetScore = BucketRound(Mathf.RoundToInt(Geom(cfg.targetScoreMin, cfg.targetScoreMax, t)));
            profile.hpMultiplier = SnapToStep(Geom(cfg.hpMultMin, cfg.hpMultMax, t), 0.05f);

            float center = Mathf.Lerp(cfg.spawnIntervalEarly, cfg.spawnIntervalLate, t);
            float j = Mathf.Max(0f, cfg.spawnIntervalJitter);
            profile.spawnIntervalMin = Mathf.Max(0.3f, center - j);
            profile.spawnIntervalMax = Mathf.Max(profile.spawnIntervalMin + 0.1f, center + j);

            profile.minDuration = Mathf.Lerp(cfg.minDurationStart, cfg.minDurationEnd, t);
            profile.maxDuration = Mathf.Max(profile.minDuration + 5f,
                Mathf.Lerp(cfg.maxDurationStart, cfg.maxDurationEnd, t));

            profile.maxE4 = Mathf.RoundToInt(Mathf.Lerp(cfg.maxE4Start, cfg.maxE4End, t));
            profile.maxE3 = Mathf.RoundToInt(Mathf.Lerp(cfg.maxE3Start, cfg.maxE3End, t));
            profile.maxE2 = Mathf.RoundToInt(Mathf.Lerp(cfg.maxE2Start, cfg.maxE2End, t));

            profile.isBossLevel = clampedIndex == n - 1;
            profile.bossBirdConfig = cfg.bossBirdConfig;
            profile.bossSpawnDelay = cfg.bossSpawnDelay;

            profile.attackingBirdPool = cfg.attackingBirdPool;
            profile.attackingBirdSpawnInterval = cfg.attackingBirdSpawnInterval;
            profile.attackingBirdSpawnChance = cfg.attackingBirdSpawnChance;

            return profile;
        }

        // value = min * (max / min) ^ t, with t in [0,1]. Safe when min == 0 or max == 0.
        static float Geom(float min, float max, float t)
        {
            if (min <= 0f || max <= 0f) return Mathf.Lerp(min, max, t);
            return min * Mathf.Pow(max / min, t);
        }

        static float SnapToStep(float value, float step)
        {
            if (step <= 0f) return value;
            return Mathf.Round(value / step) * step;
        }

        static int BucketRound(int score)
        {
            int bucket;
            if (score < 1000) bucket = 50;
            else if (score < 5000) bucket = 100;
            else if (score < 20000) bucket = 250;
            else if (score < 50000) bucket = 500;
            else bucket = 1000;

            int rounded = (score / bucket) * bucket;
            return Mathf.Max(50, rounded);
        }
    }
}
