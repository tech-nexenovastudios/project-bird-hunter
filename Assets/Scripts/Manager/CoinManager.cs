using System;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager instance;
    public static event Action<float> OnCoinsChanged;
    private float Coins;
    public float totalCoins
    {
        get { return Coins; }
        set
        {
            Coins = value;
            OnCoinsChanged?.Invoke(totalCoins);
        }
    }


    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        OnCoinsChanged?.Invoke(totalCoins);
    }

    public void Add(int addCoins)
    {
        totalCoins += addCoins;
    }

    public bool Spend(int spendCoins)
    {
        if (totalCoins >= spendCoins)
        {
            totalCoins -= spendCoins;
            return true;
        }
        else
        {
            Debug.Log("You don't have enough coins");
            return false;
        }
    }
}
