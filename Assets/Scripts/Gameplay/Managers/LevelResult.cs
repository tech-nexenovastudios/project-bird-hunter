namespace Gameplay.Managers
{
    /// <summary>
    /// Why a level ended. Lets the single completion entry point branch on intent instead of
    /// the scattered booleans the flow used to rely on.
    /// </summary>
    public enum LevelCompletionReason
    {
        Unspecified,    // legacy / unknown (back-compat shim only)
        TargetReached,  // normal level: score target hit, then the play area cleared
        BossRetreated,  // level-10 mid-boss fled at its retreat threshold
        BossDefeated,   // level-20 chapter-end boss killed outright
        PlayerFinished, // player tapped "Finish" during self-clear
        Debug,          // editor-only LevelAutoCompleter
    }

    /// <summary>
    /// The outcome of finishing a level. Built by whichever system detects completion and handed
    /// to the single GameManager.CompleteCurrentLevel(LevelResult) entry point, which resolves the
    /// final score (awarding the boss's share when <see cref="BossScored"/> is set).
    /// </summary>
    public readonly struct LevelResult
    {
        public readonly LevelCompletionReason Reason;

        /// <summary>True when the score must be resolved from the boss fields at completion time.</summary>
        public readonly bool BossScored;

        /// <summary>Already-final level score (used when <see cref="BossScored"/> is false).</summary>
        public readonly int ExplicitScore;

        /// <summary>Fraction of the boss's worth to award (0..1) — used when <see cref="BossScored"/>.</summary>
        public readonly float BossDamageFraction;

        /// <summary>Boss's base score; the larger of this and the level target is its worth.</summary>
        public readonly int BossBaseScore;

        private LevelResult(LevelCompletionReason reason, bool bossScored,
                            int explicitScore, float bossDamageFraction, int bossBaseScore)
        {
            Reason = reason;
            BossScored = bossScored;
            ExplicitScore = explicitScore;
            BossDamageFraction = bossDamageFraction;
            BossBaseScore = bossBaseScore;
        }

        /// <summary>The level's final score is already known (normal target-clear, debug).</summary>
        public static LevelResult FromScore(int finalScore, LevelCompletionReason reason)
            => new LevelResult(reason, false, finalScore, 0f, 0);

        /// <summary>A boss ended the level; its score share is awarded at completion time.</summary>
        public static LevelResult FromBoss(float damageFraction, int bossBaseScore, LevelCompletionReason reason)
            => new LevelResult(reason, true, 0, damageFraction, bossBaseScore);
    }
}
