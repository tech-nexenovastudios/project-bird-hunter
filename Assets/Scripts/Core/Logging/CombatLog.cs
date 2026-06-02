/// <summary>
/// Structured combat/scoring events on top of <see cref="GameLogger"/>'s <see cref="LogCategory.Combat"/>
/// channel. Produces aligned, greppable lines so a single combat.log file tells the whole story of how
/// fast the player is scoring and how much damage they're putting out:
///
///   SCORE  +20    level=340    egg_destroyed
///   DPS    142.0  dmg=142  window=1.0s  peak=210.0
///   SUMMARY  level=12  score=4200  totalDmg=8600  peakDps=210.0  avgDps=118.4  time=23.4s  endHp=72%  hits=3  dmgTaken=28
///
/// SCORE lines fire on every score gain; DPS lines fire once per sampling window (only when damage
/// landed); SUMMARY fires once when a level completes. The defensive fields (time / endHp / hits /
/// dmgTaken) feed the per-level star rating (see docs/Chapter_Unlock_Panel_Design.md §3.1, §3.6):
/// endHp drives ★2 (survive ≥50%), time drives ★3 (speed clear under par).
/// </summary>
public static class CombatLog
{
    public static void Score(int levelScore, int delta, string reason)
        => GameLogger.Log(LogCategory.Combat,
            $"SCORE  {Signed(delta)} level={levelScore,-8} {reason}");

    public static void Dps(float dps, int damageInWindow, float windowSeconds, float peakDps)
        => GameLogger.Log(LogCategory.Combat,
            $"DPS    {dps,-6:0.0} dmg={damageInWindow,-6} window={windowSeconds:0.0}s  peak={peakDps:0.0}");

    public static void Summary(int level, int score, int totalDamage, float peakDps, float avgDps,
                               float timeSeconds, int endHpPercent, int hitsTaken, int damageTaken)
        => GameLogger.Log(LogCategory.Combat,
            $"SUMMARY  level={level}  score={score}  totalDmg={totalDamage}  peakDps={peakDps:0.0}  avgDps={avgDps:0.0}  time={timeSeconds:0.0}s  endHp={endHpPercent}%  hits={hitsTaken}  dmgTaken={damageTaken}");

    private static string Signed(int v) => ((v >= 0 ? "+" : "") + v).PadRight(6);
}
