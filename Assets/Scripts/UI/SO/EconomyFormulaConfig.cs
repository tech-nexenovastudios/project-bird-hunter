using UnityEngine;

// Cannon upgrade cost curve. Formula-driven because all cannons share the
// same shape; per-cannon scaling is via `cannonMultipliers[]`.
//
// Unlock prices are NOT here — they live in Cloud Save `cannon_stats`
// (CannonBaseStatsDto.baseCoinsRequired / baseGemsRequired) and are read
// at runtime via CannonInventoryService.GetUnlockCoinCost.
//
// In-level coin/gem drops, completion bonuses, first-clear gems, and power
// refunds are NOT here either — they live in `GameplayRewardConfig` and are
// the only runtime path for in-game rewards. See docs/Reward_Tuning.md.
[CreateAssetMenu(
    fileName = "EconomyFormulaConfig",
    menuName = "Game/Economy/EconomyFormulaConfig")]
public class EconomyFormulaConfig : ScriptableObject
{
    [System.Serializable]
    public class CannonCostMultiplier
    {
        public string cannonId;       // e.g. "CANNON_01"
        public float coinMultiplier;  // upgrade coin multiplier
        public float matMultiplier;   // upgrade material multiplier
    }

    public CannonCostMultiplier[] cannonMultipliers;

    float GetCannonCostMultiplier(string cannonId)
    {
        foreach (var m in cannonMultipliers)
            if (m.cannonId == cannonId)
                return m.coinMultiplier;
        return 1f;
    }

    float GetCannonMatMultiplier(string cannonId)
    {
        foreach (var m in cannonMultipliers)
            if (m.cannonId == cannonId)
                return m.matMultiplier;
        return 1f;
    }

    float GetSingleShotUpgradeCost(int toLevel)
    {
        // toLevel: 2..10
        float baseUp = 400f;
        float x = toLevel - 0.5f;
        return baseUp * Mathf.Pow(x, 1.25f);
    }

    public int GetUpgradeCostCoins(string cannonId, int toLevel)
    {
        float baseCost = GetSingleShotUpgradeCost(toLevel);
        float mult     = GetCannonCostMultiplier(cannonId);
        return Mathf.RoundToInt(baseCost * mult);
    }

    public int GetUpgradeMaterialsRequired(string cannonId, int toLevel)
    {
        // Levels 2-4: 0 mats, 5-10: gentle ramp
        int fromLevel = toLevel - 1;
        int baseMat = 0;

        if (fromLevel >= 5)
        {
            float alpha = 0.8f;
            float pm    = 1.2f;
            baseMat = Mathf.FloorToInt(alpha * Mathf.Pow(fromLevel - 4, pm));
        }

        float mult = GetCannonMatMultiplier(cannonId);
        return Mathf.RoundToInt(baseMat * mult);
    }
}
