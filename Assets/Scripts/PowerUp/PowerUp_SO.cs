//using UnityEngine;

///// <summary>
///// Abstract base class for defining power-up abilities.
///// Each specific power-up will inherit from this and implement its own effect.
///// </summary>
//public abstract class PowerUp_SO : ScriptableObject
//{

//    // Unique identifier for the power-up
//    public int id;

//    // Display name for the power-up
//    public string Name;

//    // Icon to be shown in the UI for this power-up
//    public Sprite iconSprite;

//    public int powerLevel = -1;

//    [HideInInspector] public readonly int maxPowerLever = 4;

//    public int tempPowerLevel;

//    public bool alreadyActive;

//    /// <summary>
//    /// Apply the power-up's effect to the given CannonPower instance.
//    /// This method must be overridden by each specific power-up.
//    /// </summary>
//    /// <param name="power">The CannonPower instance to modify.</param>
//    public virtual void GrantAbility(CannonPower power)
//    {
//        if (alreadyActive)
//        {
//            tempPowerLevel = Mathf.Clamp(tempPowerLevel += 1, 0, maxPowerLever);
//            Debug.Log(maxPowerLever);
//        }
//        else
//        {
//            tempPowerLevel = Mathf.Clamp(powerLevel, 0, maxPowerLever);
//            alreadyActive = true;
//        }
//    }

//    public virtual void UpgradePower()
//    {
//        if (powerLevel < maxPowerLever)
//        {
//            powerLevel++;
//        }
//    }
//}

using UnityEngine;

// 1. Define the Rarity Enum (Global or inside the class)
public enum PowerRarity
{
    Common,
    Rare,
    Legendary
}

public abstract class PowerUp_SO : ScriptableObject
{
    [Header("General Settings")]
    public int id;
    public string Name;
    public Sprite iconSprite;

    // 2. Add the Rarity Selection
    public PowerRarity rarity;

    [Header("Power Settings")]
    public int powerLevel = -1;
    [HideInInspector] public readonly int maxPowerLever = 4;
    public int tempPowerLevel;
    public bool alreadyActive;

    public virtual void GrantAbility(CannonPower power)
    {
        if (alreadyActive)
        {
            tempPowerLevel = Mathf.Clamp(tempPowerLevel += 1, 0, maxPowerLever);
        }
        else
        {
            tempPowerLevel = Mathf.Clamp(powerLevel, 0, maxPowerLever);
            alreadyActive = true;
        }
    }

    public virtual void UpgradePower()
    {
        if (powerLevel < maxPowerLever)
        {
            powerLevel++;
        }
    }
}
