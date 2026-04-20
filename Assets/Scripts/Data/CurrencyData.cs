using System;
using System.Collections.Generic;

[Serializable]
public class CurrencyData
{
    public Dictionary<string, long> balances;

    public CurrencyData()
    {
        balances = new Dictionary<string, long>();
    }

    public long GetBalance(string currencyKey)
    {
        return balances.TryGetValue(currencyKey, out var value) ? value : 0;
    }

    public void SetBalance(string currencyKey, long amount)
    {
        balances[currencyKey] = amount;
    }
}