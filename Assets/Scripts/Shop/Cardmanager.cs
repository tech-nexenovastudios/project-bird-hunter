using System;
using UnityEngine;

public enum CardType { Cannon, SlotPower }

public class CardManager : MonoBehaviour
{
    public static CardManager instance;

    // Notify listeners when a card total changes (type, newCount)
    public static event Action<CardType, int> OnCardChanged;

    // Internal counts
    [SerializeField] private int cannon;
    [SerializeField] private int slotPower;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Push initial values to any UI/listeners
        OnCardChanged?.Invoke(CardType.Cannon, cannon);
        OnCardChanged?.Invoke(CardType.SlotPower, slotPower);
    }

    // -------- Public API --------

    public int GetCount(CardType type)
    {
        return type == CardType.Cannon ? cannon : slotPower;
    }

    public void Add(CardType type, int amount)
    {
        if (amount <= 0) return;

        if (type == CardType.Cannon)
        {
            cannon += amount;
            OnCardChanged?.Invoke(CardType.Cannon, cannon);
        }
        else
        {
            slotPower += amount;
            OnCardChanged?.Invoke(CardType.SlotPower, slotPower);
        }
    }

    public bool Spend(CardType type, int amount)
    {
        if (amount <= 0) return true;

        if (type == CardType.Cannon)
        {
            if (cannon < amount) return false;
            cannon -= amount;
            OnCardChanged?.Invoke(CardType.Cannon, cannon);
            return true;
        }
        else
        {
            if (slotPower < amount) return false;
            slotPower -= amount;
            OnCardChanged?.Invoke(CardType.SlotPower, slotPower);
            return true;
        }
    }
}
