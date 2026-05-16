using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEngine;

/// <summary>
/// Production-ready Currency Manager using Unity Economy Service.
/// Currencies must be configured in Unity Dashboard: GOLD, GEM, POWER
/// Plain C# singleton — no MonoBehaviour needed.
/// 
/// Usage:
///   await CurrencyManager.Instance.LoadBalances();               // Call once after auth
///   await CurrencyManager.Instance.AddGold(100);                 // Increment
///   bool success = await CurrencyManager.Instance.SpendGems(50); // Decrement (false if insufficient)
///   long gold = CurrencyManager.Instance.Gold;                   // Read cached value
/// </summary>
public class CurrencyManager
{
    private static CurrencyManager _instance;
    public static CurrencyManager Instance => _instance ??= new CurrencyManager();

    // ==================== Economy Currency IDs (match Unity Dashboard) ====================

    private const string GOLD_ID = "GOLD";
    private const string GEM_ID = "GEM";
    private const string POWER_ID = "POWER";

    // ==================== Cached Balances ====================

    private long _gold;
    private long _gems;
    private long _power;

    public long Gold => _gold;
    public long Gems => _gems;
    public long Power => _power;

    // ==================== State ====================

    private bool _isLoaded = false;


    public bool IsLoaded => _isLoaded;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    public bool IsBusy => _lock.CurrentCount == 0;

    // ==================== Events ====================

    /// <summary>Fired whenever any currency value changes. Args: (CurrencyType, newValue)</summary>
    public static event Action<CurrencyType, long> OnCurrencyChanged;

    /// <summary>Fired when a spend attempt fails due to insufficient funds.</summary>
    public static event Action<CurrencyType, long> OnInsufficientFunds;

    /// <summary>Call from external scripts (e.g. PurchaseManager) to notify insufficient funds.</summary>
    public void NotifyInsufficientFunds(CurrencyType type, long amount)
    {
        OnInsufficientFunds?.Invoke(type, amount);
    }

    // ==================== Load Balances ====================

    /// <summary>
    /// Loads all currency balances from Unity Economy.
    /// Call once after authentication. Safe to call multiple times.
    /// </summary>
    public async UniTask LoadBalances(bool forceReload = false)
    {
        if (_isLoaded && !forceReload) return;

        try
        {
            var balancesResult = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();

            _gold = 0;
            _gems = 0;
            _power = 0;

            foreach (var balance in balancesResult.Balances)
            {
                switch (balance.CurrencyId)
                {
                    case GOLD_ID:
                        _gold = balance.Balance;
                        break;
                    case GEM_ID:
                        _gems = balance.Balance;
                        break;
                    case POWER_ID:
                        _power = balance.Balance;
                        break;
                }
            }

            _isLoaded = true;
            Debug.Log($"[Currency] Loaded — Gold: {_gold}, Gems: {_gems}, Power: {_power}");

            OnCurrencyChanged?.Invoke(CurrencyType.Gold, _gold);
            OnCurrencyChanged?.Invoke(CurrencyType.Gems, _gems);
            OnCurrencyChanged?.Invoke(CurrencyType.Power, _power);

            // Publish global event for UI that subscribes late
            EventBus.Publish(new CurrencyLoadedEvent
            {
                gold = _gold,
                gems = _gems,
                power = _power
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Currency] Failed to load balances: {ex.Message}");
            throw; // let BootController catch and handle it
        }
    }

    // ==================== Add (Increment) ====================

    // Returns true if the credit landed on the server, false otherwise. Callers that need
    // to keep mirrored counters in sync with the balance should await this.
    public async UniTask<bool> AddGold(long amount)
    {
        return await IncrementCurrency(CurrencyType.Gold, GOLD_ID, amount);
    }

    public async UniTask<bool> AddGems(long amount)
    {
        return await IncrementCurrency(CurrencyType.Gems, GEM_ID, amount);
    }

    public async UniTask<bool> AddPower(long amount)
    {
        return await IncrementCurrency(CurrencyType.Power, POWER_ID, amount);
    }

    // ==================== Spend (Decrement) ====================

    /// <summary>Returns true if spend was successful, false if insufficient funds.</summary>
    public async UniTask<bool> SpendGold(long amount)
    {
        return await DecrementCurrency(CurrencyType.Gold, GOLD_ID, amount);
    }

    /// <summary>Returns true if spend was successful, false if insufficient funds.</summary>
    public async UniTask<bool> SpendGems(long amount)
    {
        return await DecrementCurrency(CurrencyType.Gems, GEM_ID, amount);
    }

    /// <summary>Returns true if spend was successful, false if insufficient funds.</summary>
    public async UniTask<bool> SpendPower(long amount)
    {
        return await DecrementCurrency(CurrencyType.Power, POWER_ID, amount);
    }

    // ==================== Check Affordability ====================

    public bool CanAffordGold(long amount) => _gold >= amount;
    public bool CanAffordGems(long amount) => _gems >= amount;
    public bool CanAffordPower(long amount) => _power >= amount;

    public bool CanAfford(CurrencyType type, long amount)
    {
        return type switch
        {
            CurrencyType.Gold => _gold >= amount,
            CurrencyType.Gems => _gems >= amount,
            CurrencyType.Power => _power >= amount,
            _ => false
        };
    }

    // ==================== Set Balance (Admin/Override) ====================

    /// <summary>Directly sets a currency balance on the server. Use sparingly.</summary>
    public async UniTask SetBalance(CurrencyType type, long value)
    {
        string currencyId = GetCurrencyId(type);
        value = Math.Max(0, value);

        await _lock.WaitAsync();
        try
        {
            var result = await EconomyService.Instance.PlayerBalances.SetBalanceAsync(currencyId, value);
            UpdateLocalBalance(type, result.Balance);
            Debug.Log($"[Currency] Set {type} to {result.Balance}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Currency] Failed to set {type}: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    // ==================== Multi-Currency Spend ====================

    /// <summary>
    /// Checks all costs locally first. If all affordable, spends one by one on the server.
    /// NOT truly atomic on server — but prevents unnecessary calls if locally insufficient.
    /// </summary>
    public async UniTask<bool> SpendMultiple(params (CurrencyType type, long amount)[] costs)
    {
        // Local validation first
        foreach (var (type, amount) in costs)
        {
            if (!CanAfford(type, amount))
            {
                OnInsufficientFunds?.Invoke(type, amount);
                return false;
            }
        }

        // Deduct each on server
        foreach (var (type, amount) in costs)
        {
            string currencyId = GetCurrencyId(type);
            bool success = await DecrementCurrency(type, currencyId, amount);

            if (!success)
            {
                // Server-side balance was different — reload to sync
                Debug.LogWarning($"[Currency] Server rejected spend for {type}. Reloading balances.");
                await LoadBalances(true);
                return false;
            }
        }

        return true;
    }

    // ==================== Get by Type ====================

    public long GetCurrency(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Gold => _gold,
            CurrencyType.Gems => _gems,
            CurrencyType.Power => _power,
            _ => 0
        };
    }

    // ==================== Refresh ====================

    /// <summary>Force refresh all balances from server.</summary>
    public async UniTask Refresh()
    {
        await LoadBalances(true);
        // Inside CurrencyManager, at the END of Refresh() after balances are loaded:
        GameEvent.BalanceSynced(CurrencyType.Gold, (int)Gold);
        GameEvent.BalanceSynced(CurrencyType.Gems, (int)Gems);
        GameEvent.BalanceSynced(CurrencyType.Power, (int)Power);
    }

    // ==================== Internal ====================

    private async UniTask<bool> IncrementCurrency(CurrencyType type, string currencyId, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Currency] Cannot add negative or zero amount to {type}.");
            return false;
        }

        await _lock.WaitAsync();
        try
        {
            var result = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync(currencyId, (int)amount);
            UpdateLocalBalance(type, result.Balance);
            Debug.Log($"[Currency] Added {amount} {type}. New balance: {result.Balance}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Currency] Failed to add {type}: {ex.Message}");
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async UniTask<bool> DecrementCurrency(CurrencyType type, string currencyId, long amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[Currency] Cannot spend negative or zero amount of {type}.");
            return false;
        }

        if (!CanAfford(type, amount))
        {
            OnInsufficientFunds?.Invoke(type, amount);
            return false;
        }

        await _lock.WaitAsync();
        try
        {
            var result = await EconomyService.Instance.PlayerBalances.DecrementBalanceAsync(currencyId, (int)amount);
            UpdateLocalBalance(type, result.Balance);
            Debug.Log($"[Currency] Spent {amount} {type}. New balance: {result.Balance}");
            return true;
        }
        catch (EconomyException ex)
        {
            Debug.LogError($"[Currency] Economy error spending {type}: {ex.Message}");
            await LoadBalances(true);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Currency] Failed to spend {type}: {ex.Message}");
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void UpdateLocalBalance(CurrencyType type, long newValue)
    {
        switch (type)
        {
            case CurrencyType.Gold: _gold = newValue; break;
            case CurrencyType.Gems: _gems = newValue; break;
            case CurrencyType.Power: _power = newValue; break;
        }

        OnCurrencyChanged?.Invoke(type, newValue);
    }

    private string GetCurrencyId(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Gold => GOLD_ID,
            CurrencyType.Gems => GEM_ID,
            CurrencyType.Power => POWER_ID,
            _ => throw new ArgumentException($"Unknown currency type: {type}")
        };
    }
}

public enum CurrencyType
{
    Gold,
    Gems,
    Power
}