using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/New Cannon Upgrade")]
public class CannonUpgrade_SO : ScriptableObject
{
    public int cannonIndex;
    public GameObject cannonPrefab;
    public string cannonName;
    public string cannonDescription;
    public Sprite cannonSprite;
    public float cardsRequired;
    public float coinsRequired;
    public int cannonLevel;
    public bool cannonUnlock;
    public CannonStats cannonStats;

    public float coinIncrement = 1;

    public List<int> CardUpgradeInLevels = new();

    public int normalCardIncrement;
    public int specificNumberCardIncrement;

    private bool checkIfUpgradePossible;
    public int moduloForCardUpgrade;

    // ==================== Base Value Cache ====================

    [HideInInspector] public float baseCoinsRequired;
    [HideInInspector] public float baseCoinIncrement;
    [HideInInspector] public float baseCardsRequired;
    [HideInInspector] public int baseCannonLevel;
    private bool _baseCached = false;

    /// <summary>
    /// Call once at app start. Stores Inspector defaults for this SO and its CannonStats.
    /// </summary>
    public void CacheBaseValues()
    {
        if (_baseCached) return;

        baseCoinsRequired = coinsRequired;
        baseCoinIncrement = coinIncrement;
        baseCardsRequired = cardsRequired;
        baseCannonLevel = cannonLevel;

        if (cannonStats != null)
            cannonStats.CacheBaseValues();

        _baseCached = true;
    }

    /// <summary>
    /// Resets this SO and its CannonStats to base values.
    /// </summary>
    public void ResetToBase()
    {
        if (!_baseCached)
        {
            Debug.LogWarning($"[CannonUpgrade_SO] Base values not cached for {cannonName}. Call CacheBaseValues() first.");
            return;
        }

        coinsRequired = baseCoinsRequired;
        coinIncrement = baseCoinIncrement;
        cardsRequired = baseCardsRequired;
        cannonLevel = baseCannonLevel;

        if (cannonStats != null)
            cannonStats.ResetToBase();
    }

    /// <summary>
    /// Resets to base then replays upgrades to reach the target level.
    /// </summary>
    public void ReplayToLevel(int targetLevel)
    {
        ResetToBase();
        for (int i = 0; i < targetLevel; i++)
        {
            UpgradeCannon();
        }
    }

    public void UpgradeCannon()
    {
        checkIfUpgradePossible = false;

        if (cannonStats == null)
        {
            cannonStats = ScriptableObject.CreateInstance<CannonStats>();
            cannonStats.maxHealth = 1;
        }

        coinsRequired += coinIncrement;
        coinIncrement += 1;
        int tempLevelCheck = cannonLevel % moduloForCardUpgrade;

        if (CardUpgradeInLevels.Contains(tempLevelCheck))
        {
            cardsRequired += normalCardIncrement;
        }
        else
        {
            cardsRequired += specificNumberCardIncrement;
        }

        cannonStats.Upgrade(cannonLevel);
        cannonLevel++;
    }
}