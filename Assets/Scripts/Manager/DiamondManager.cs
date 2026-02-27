using System;
using UnityEngine;
public class DiamondManager : MonoBehaviour
{
    public static DiamondManager instance;
    public static event Action<float> OnDiamondChanged;

    private float Diamonds;
    public float totalDiamonds
    {
        get { return Diamonds; }
        set { Diamonds = value;
            OnDiamondChanged?.Invoke(totalDiamonds);
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
        OnDiamondChanged?.Invoke(totalDiamonds);
    }

    public void Add(float addDiamonds)
    {
        totalDiamonds += addDiamonds;
    }

    public bool Spend(float spendDiamonds)
    {
        if (totalDiamonds >= spendDiamonds)
        {
            totalDiamonds -= Diamonds;
            return true;
        }
        else
        {
            Debug.Log("You don't have enough coins");
            return false;
        }
    }
}
