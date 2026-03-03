using System;

// ───────────────────────────────────────────────────────────
// PURPOSE: Central event hub. Any system can fire or listen.
//          NO script needs a direct reference to another.
//
// PATTERN: Observer Pattern via C# events.
//
// HOW TO LISTEN (from any script):
//     void OnEnable()  => GameEvents.OnCoinArrived += HandleCoinArrived;
//     void OnDisable() => GameEvents.OnCoinArrived -= HandleCoinArrived;
//     void HandleCoinArrived(int amount) { /* update UI */ }
//
// HOW TO FIRE (from any script):
//     GameEvents.CoinArrived(1);
//
// FUTURE-PROOFING: Add new events here as your game grows.
//     public static event Action OnBossDefeated;
//     public static event Action<string> OnAchievementUnlocked;
//     ...and so on. Nothing else changes.
// ───────────────────────────────────────────────────────────

public static class GameEvent
{
    // ── Coin Events ──────────────────────────────────────

    /// <summary>
    /// Fired when ANY system wants to trigger the coin fly effect.
    /// Parameters: (screenPosition where coins burst from, total coin value)
    /// </summary>
    public static event Action<UnityEngine.Vector2, int> OnCoinCollected;

    /// <summary>
    /// Fired each time ONE coin icon reaches the counter UI.
    /// Parameter: coin value this single coin represents.
    /// </summary>
    public static event Action<int> OnCoinArrived;

    /// <summary>
    /// Fired after ALL coins from a single burst have arrived.
    /// Useful for: playing a final "cha-ching" sound, saving, etc.
    /// </summary>
    public static event Action OnCoinBurstComplete;


    // ── Safe Invoke Methods ──────────────────────────────
    // These null-check so callers never crash if nobody is listening.

    public static void CoinCollected(UnityEngine.Vector2 screenPos, int totalValue)
    {
        OnCoinCollected?.Invoke(screenPos, totalValue);
    }

    public static void CoinArrived(int value)
    {
        OnCoinArrived?.Invoke(value);
    }

    public static void CoinBurstComplete()
    {
        OnCoinBurstComplete?.Invoke();
    }
}