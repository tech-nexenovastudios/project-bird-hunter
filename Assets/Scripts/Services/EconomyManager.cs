using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;


    public interface IEconomyManager
    {
        Task<int> SpendCurrencyAsync(string currencyId, int amount);
        Task<int> EarnCurrencyAsync(string currencyId, int amount);
        Task<int> GetBalanceAsync(string currencyId);
    }

    public class EconomyManager : IEconomyManager
    {
        public async Task<int> SpendCurrencyAsync(string currencyId, int amount)
        {
            await EnsureInitializedAsync();

            try
            {
                // Attempt to decrement balance
                PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.DecrementBalanceAsync(currencyId, amount);
                
                Debug.Log($"[Economy] Spent {amount} {currencyId}. New Balance: {newBalance.Balance}");
                return (int)newBalance.Balance;
            }
            catch (EconomyException e)
            {
                // 10504 is the universal code for Insufficient Balance in UGS Economy
                if (e.ErrorCode == 10504)
                {
                    Debug.LogWarning($"[Economy] Insufficient funds for {currencyId}.");
                    return -1; 
                }
                
                Debug.LogError($"[Economy] Error {e.ErrorCode}: {e.Message}");
                return -1;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Economy] General Error: {e.Message}");
                return -1;
            }
        }
        /* example Usage:
        
            int newBalance = await economyManager.SpendCurrencyAsync("GOLD", 500);
        */
        public async Task<int> EarnCurrencyAsync(string currencyId, int amount)
        {
            await EnsureInitializedAsync();

            try
            {
                PlayerBalance newBalance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync(currencyId, amount);
                Debug.Log($"[Economy] Earned {amount} {currencyId}. New Balance: {newBalance.Balance}");
                return (int)newBalance.Balance;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Economy] EarnCurrency failed: {e.Message}");
                return 0;
            }
        }

        public async Task<int> GetBalanceAsync(string currencyId)
        {
            await EnsureInitializedAsync();

            try
            {
                GetBalancesResult balances = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
                var playerBalance = balances.Balances.Find(b => b.CurrencyId == currencyId);
                
                return playerBalance != null ? (int)playerBalance.Balance : 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Economy] Failed to get balance: {e.Message}");
                return 0;
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
        }
    }
