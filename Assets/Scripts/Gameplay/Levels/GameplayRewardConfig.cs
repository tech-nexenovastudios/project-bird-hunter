using UnityEngine;

namespace Gameplay.Rewards
{
    /// <summary>
    /// Gameplay Reward Tables from Economy Spec (Section 4).
    /// 
    /// Defines:
    /// - 4.1 In-Level Coin & Gem Spawns (expected per level)
    /// - 4.2 Level Completion Rewards (successful clear only)
    /// - 4.3 Power (Energy) Refunds on Completion
    /// 
    /// Used by RewardManager to award coins, gems, and power during gameplay.
    /// </summary>
    [CreateAssetMenu(menuName = "BirdHunter/Gameplay Reward Config")]
    public class GameplayRewardConfig : ScriptableObject
    {
        [Header("═══ 4.1 IN-LEVEL COIN DROPS (Expected Per Level) ═══")]
        
        [SerializeField] 
        private CoinDropPhase[] coinDropPhases = new CoinDropPhase[3]
        {
            new() { phase = "Early", chapters = "1–10", minCoins = 30,  maxCoins = 80,  notes = "Mostly kills/combos" },
            new() { phase = "Mid",   chapters = "11–20", minCoins = 100, maxCoins = 200, notes = "Denser spawns" },
            new() { phase = "Late",  chapters = "21–30", minCoins = 250, maxCoins = 400, notes = "More elite enemies sources" }
        };

        [Header("═══ 4.1 GEM RARITY (In-Level Drops Only) ═══")]
        [SerializeField] 
        private GemDropPhase[] gemDropPhases = new GemDropPhase[3]
        {
            new() { phase = "Early", chapters = "1–10",  dropChancePercent = 3f, minGemsPerDrop = 1, maxGemsPerDrop = 1 },
            new() { phase = "Mid",   chapters = "11–20", dropChancePercent = 5f, minGemsPerDrop = 1, maxGemsPerDrop = 2 },
            new() { phase = "Late",  chapters = "21–30", dropChancePercent = 8f, minGemsPerDrop = 2, maxGemsPerDrop = 3 }
        };

        [Header("═══ 4.2 LEVEL COMPLETION REWARDS ═══")]
        [SerializeField] 
        private LevelCompletionPhase[] completionPhases = new LevelCompletionPhase[3]
        {
            new() { phase = "Early", chapters = "1–10",  baseCoins = 550,  performanceBonusMin = 50,  performanceBonusMax = 100 },
            new() { phase = "Mid",   chapters = "11–20", baseCoins = 1320, performanceBonusMin = 120, performanceBonusMax = 240 },
            new() { phase = "Late",  chapters = "21–30", baseCoins = 2750, performanceBonusMin = 250, performanceBonusMax = 500 }
        };

        [Header("═══ 4.2 FIRST-TIME CLEAR GEMS ═══")]
        [SerializeField] private int gemsOnFirstTimeClear = 10;

        [Header("═══ 4.3 POWER (ENERGY) REFUNDS ═══")]
        public int powerCostPerAttempt = 5;
        [Range(0.1f, 0.5f)] public float powerRefundChance = 0.25f; // 20–30% chance
        public int powerRefundAmount = 5;
        public float expectedPowerReturnRatio = 0.25f; // 0.2–0.3 per level

        [Header("═══ SPECIAL CASES ═══")]
        public int coinMultiplierForAd = 2; // 2x coins for watching ad
        public int powerPerAdRefill = 5;
        public int gemsPerAdReward = 10;

        // ────────────────────────────────────────────────────────────────
        // PUBLIC API — In-Level Coin Drops
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Get the coin drop range for a given chapter.
        /// Returns (min, max) coins expected from normal drops.
        /// </summary>
        public Vector2Int GetCoinDropRange(int chapter)
        {
            var phase = GetCoinPhaseForChapter(chapter);
            return new Vector2Int(phase.minCoins, phase.maxCoins);
        }

        /// <summary>
        /// Random coin amount from the expected drop range for this chapter.
        /// Call whenever an egg/bird is destroyed.
        /// </summary>
        public int GetRandomCoinDrop(int chapter)
        {
            var range = GetCoinDropRange(chapter);
            return Random.Range(range.x, range.y + 1);
        }

        // ────────────────────────────────────────────────────────────────
        // PUBLIC API — In-Level Gem Drops
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Get gem drop chance and amount for a given chapter.
        /// Returns (dropChance 0–1, gemAmount min–max).
        /// </summary>
        public (float dropChance, Vector2Int gemRange) GetGemDropInfo(int chapter)
        {
            var phase = GetGemPhaseForChapter(chapter);
            float chance = phase.dropChancePercent / 100f;
            return (chance, new Vector2Int(phase.minGemsPerDrop, phase.maxGemsPerDrop));
        }

        /// <summary>
        /// Probabilistic check: should we award gems this drop?
        /// </summary>
        public bool ShouldDropGem(int chapter)
        {
            var (chance, _) = GetGemDropInfo(chapter);
            return Random.value < chance;
        }

        /// <summary>
        /// Random gem amount if a gem drop occurs.
        /// </summary>
        public int GetRandomGemDrop(int chapter)
        {
            var (_, gemRange) = GetGemDropInfo(chapter);
            return Random.Range(gemRange.x, gemRange.y + 1);
        }

        // ────────────────────────────────────────────────────────────────
        // PUBLIC API — Level Completion Rewards
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculate coin reward for level completion.
        /// performanceRatio = 0..1 (or higher) based on score achieved vs. target.
        /// Returns: base coins + performance bonus.
        /// </summary>
        public int GetLevelCompletionCoins(int chapter, float performanceRatio)
        {
            var phase = GetCompletionPhaseForChapter(chapter);
            float clampedRatio = Mathf.Clamp01(performanceRatio);
            int bonus = Mathf.RoundToInt(Mathf.Lerp(
                phase.performanceBonusMin,
                phase.performanceBonusMax,
                clampedRatio
            ));
            return phase.baseCoins + bonus;
        }

        /// <summary>
        /// Gems awarded on first-time clear (one-time per level).
        /// </summary>
        public int GetFirstTimeClearGems()
        {
            return gemsOnFirstTimeClear;
        }

        // ────────────────────────────────────────────────────────────────
        // PUBLIC API — Power Refunds
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Check if power should be refunded on level completion.
        /// Returns 0 if no refund, or powerRefundAmount if it procs.
        /// </summary>
        public int GetPowerRefund()
        {
            return Random.value < powerRefundChance ? powerRefundAmount : 0;
        }

        // ────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ────────────────────────────────────────────────────────────────

        private CoinDropPhase GetCoinPhaseForChapter(int chapter)
        {
            if (chapter <= 10) return coinDropPhases[0];
            if (chapter <= 20) return coinDropPhases[1];
            return coinDropPhases[2];
        }

        private GemDropPhase GetGemPhaseForChapter(int chapter)
        {
            if (chapter <= 10) return gemDropPhases[0];
            if (chapter <= 20) return gemDropPhases[1];
            return gemDropPhases[2];
        }

        private LevelCompletionPhase GetCompletionPhaseForChapter(int chapter)
        {
            if (chapter <= 10) return completionPhases[0];
            if (chapter <= 20) return completionPhases[1];
            return completionPhases[2];
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // SERIALIZABLE DATA STRUCTURES
    // ════════════════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public class CoinDropPhase
    {
        public string phase;        // "Early", "Mid", "Late"
        public string chapters;     // "1–10", "11–20", "21–30"
        public int minCoins;
        public int maxCoins;
        [TextArea(1, 2)] public string notes;
    }

    [System.Serializable]
    public class GemDropPhase
    {
        public string phase;                    // "Early", "Mid", "Late"
        public string chapters;                 // "1–10", "11–20", "21–30"
        [Range(0f, 100f)] public float dropChancePercent; // % chance per level
        public int minGemsPerDrop;
        public int maxGemsPerDrop;
    }

    [System.Serializable]
    public class LevelCompletionPhase
    {
        public string phase;                // "Early", "Mid", "Late"
        public string chapters;             // "1–10", "11–20", "21–30"
        public int baseCoins;               // Base reward
        public int performanceBonusMin;     // Min bonus from performance
        public int performanceBonusMax;     // Max bonus from performance (at 100% score)
    }
}
