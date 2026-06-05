/// <summary>
/// Structured economy events on top of <see cref="GameLogger"/>'s <see cref="LogCategory.Economy"/>
/// channel. Produces aligned, greppable lines so a single economy.log file tells the whole story of
/// where currency came from and went:
///
///   EARN  GOLD   +50    bal=1250   egg_destroyed
///   EARN  GEM    +5     bal=12     first_time_clear
///   SPEND POWER  -5     bal=10     level_entry
///   SPEND GOLD   -200   bal=1050   cannon_upgrade
///   SPEND GOLD   -800   FAILED(insufficient)   cannon_unlock
///
/// Call these from currency mutation points; <c>reason</c> is a short stable token (snake_case) so
/// you can grep/aggregate by source (e.g. all "level_completion" earns across a session).
/// </summary>
public static class EconomyLog
{
    public static void Earn(CurrencyType type, long amount, long balanceAfter, string reason)
        => GameLogger.Log(LogCategory.Economy,
            $"EARN  {Col(type)} {Amount(+amount)} bal={balanceAfter,-8} {reason}");

    public static void Spend(CurrencyType type, long amount, long balanceAfter, string reason)
        => GameLogger.Log(LogCategory.Economy,
            $"SPEND {Col(type)} {Amount(-amount)} bal={balanceAfter,-8} {reason}");

    /// <summary>A spend/earn that did not land (insufficient funds, network/economy error).</summary>
    public static void Failed(string action, CurrencyType type, long amount, string cause, string reason)
        => GameLogger.LogWarning(LogCategory.Economy,
            $"{action,-5} {Col(type)} {Amount(action == "SPEND" ? -amount : amount)} FAILED({cause})   {reason}");

    public static void Insufficient(CurrencyType type, long amount, long balance, string reason)
        => GameLogger.LogWarning(LogCategory.Economy,
            $"DENY  {Col(type)} need {amount} have {balance}   {reason}");

    // ==================== Formatting ====================

    private static string Col(CurrencyType type) => type.ToString().ToUpperInvariant().PadRight(6);

    private static string Amount(long signed)
    {
        string s = (signed >= 0 ? "+" : "") + signed;
        return s.PadRight(6);
    }
}
