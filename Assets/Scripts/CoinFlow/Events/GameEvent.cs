using System;

public static class GameEvent
{
    // ── Changed: now carries CurrencyType ────────────────
    // PARAMS: CurrencyType = which currency to animate
    //         Vector2      = screen position to spawn FROM
    //         int          = total value earned

    // ── Balance Sync (fired after CurrencyManager refreshes) ──
    public static event Action<CurrencyType, int> OnBalanceSynced;

    public static void BalanceSynced(CurrencyType type, int balance)
    {
        OnBalanceSynced?.Invoke(type, balance);
    }
    public static event Action<CurrencyType, UnityEngine.Vector2, int> OnCurrencyCollected;

    // Each icon arrives at its counter
    // CurrencyType tells the correct counter to increment
    public static event Action<CurrencyType, int> OnCurrencyArrived;

    // All icons from one burst finished
    public static event Action<CurrencyType> OnCurrencyBurstComplete;


    public static void CurrencyCollected(CurrencyType type, UnityEngine.Vector2 screenPos, int totalValue)
    {
        OnCurrencyCollected?.Invoke(type, screenPos, totalValue);
    }

    public static void CurrencyArrived(CurrencyType type, int value)
    {
        OnCurrencyArrived?.Invoke(type, value);
    }

    public static void CurrencyBurstComplete(CurrencyType type)
    {
        OnCurrencyBurstComplete?.Invoke(type);
    }
}
