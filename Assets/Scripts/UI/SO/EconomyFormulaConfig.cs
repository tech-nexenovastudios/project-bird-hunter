using UnityEngine;

[CreateAssetMenu(
    fileName = "EconomyFormulaConfig",
    menuName = "Game/Economy/EconomyFormulaConfig")]
public class EconomyFormulaConfig : ScriptableObject
{
    // =========================
    //  GLOBAL HELPERS
    // =========================

    public int GetGlobalLevel(int chapter, int levelInChapter)
    {
        return (chapter - 1) * 20 + levelInChapter;
    }

    // =========================
    //  COINS
    // =========================

    // 1. In-level drops  (≈30-80 early → 250-400 late)
    public float GetCoinsDropMin(int chapter, int levelInChapter)
    {
        float baseDropMin = 30f;
        float mc = 1f + 0.03f * Mathf.Pow(chapter - 1, 1.1f);
        float lf = 1f + 0.10f * ((levelInChapter - 1) / 19f);
        return baseDropMin * mc * lf;
    }

    public float GetCoinsDropMax(int chapter, int levelInChapter)
    {
        float baseDropMax = 80f;
        float mc = 1f + 0.03f * Mathf.Pow(chapter - 1, 1.1f);
        float lf = 1f + 0.10f * ((levelInChapter - 1) / 19f);
        return baseDropMax * mc * lf;
    }

    public int RollCoinsFromDrops(int chapter, int levelInChapter)
    {
        int min = Mathf.RoundToInt(GetCoinsDropMin(chapter, levelInChapter));
        int max = Mathf.RoundToInt(GetCoinsDropMax(chapter, levelInChapter)) + 1;
        return Random.Range(min, max);
    }

    // 2. Level completion (≈50-100 → 120-240 → 250-500)
    public float GetCompletionBaseCoins(int chapter)
    {
        float baseComp = 50f;
        float mc = 1f + 0.04f * Mathf.Pow(chapter - 1, 1.15f);
        return baseComp * mc;
    }

    public float GetCompletionPerfMax(int chapter)
    {
        float basePerf = 50f;
        float mc = 1f + 0.04f * Mathf.Pow(chapter - 1, 1.15f);
        return basePerf * mc;
    }

    /// <summary>
    /// performance01 = 0..1 (e.g., 0, 0.5, 1 based on stars)
    /// </summary>
    public int GetCompletionCoins(int chapter, float performance01)
    {
        float baseCoins = GetCompletionBaseCoins(chapter);
        float perfMax   = GetCompletionPerfMax(chapter);
        float reward    = baseCoins + performance01 * perfMax;

        float jitter = Random.Range(0.95f, 1.05f);
        return Mathf.RoundToInt(reward * jitter);
    }

    // =========================
    //  GEMS
    // =========================

    // 3. First-time clear gems (gentle ramp)
    public int GetFirstClearGems(
        int chapter,
        int levelInChapter,
        bool isMilestone,
        bool isBoss)
    {
        int globalLevel = GetGlobalLevel(chapter, levelInChapter);
        float mg = 1f + 0.0004f * Mathf.Pow(globalLevel - 1, 1.05f);
        float expected = 0.9f * mg;

        int baseGems = Mathf.Clamp(Mathf.RoundToInt(expected), 1, 2);

        if (isMilestone) baseGems += 1;
        if (isBoss)      baseGems += 2;

        return baseGems;
    }

    // 4. In-level gem drop chance & amount
    public float GetGemDropChance(int chapter)
    {
        float t = (chapter - 1) / 29f;           // 0..1 over 30 chapters
        return 0.03f + 0.07f * Mathf.Pow(t, 1.05f); // 3% → 10%, gentle
    }

    public int RollGemDropAmount(int chapter)
    {
        if (chapter <= 10)
            return 1;

        float r = Random.value;

        if (chapter <= 20)
        {
            // 80%:1, 20%:2
            return r < 0.8f ? 1 : 2;
        }
        else
        {
            // 70%:2, 30%:3
            return r < 0.7f ? 2 : 3;
        }
    }

    // =========================
    //  POWER (ENERGY)
    // =========================

    // 5. Refund chance on completion (20% → 30%)
    public float GetPowerRefundChance(int chapter)
    {
        return 0.20f + 0.10f * ((chapter - 1) / 29f);
    }

    public int RollPowerRefund(int chapter)
    {
        return Random.value < GetPowerRefundChance(chapter) ? 1 : 0;
    }

    // 6. In-level power drops (4% → 10%)
    public float GetPowerDropChance(int chapter)
    {
        return 0.04f + 0.06f * ((chapter - 1) / 29f);
    }

    public int RollPowerDropAmount(int chapter)
    {
        if (chapter < 21)
            return 1;

        // Late game: 80% +1, 20% +2
        return Random.value < 0.8f ? 1 : 2;
    }

    // =========================
    //  CANNON UNLOCK / UPGRADE
    // =========================

    // Multipliers can be set from inspector per cannon ID
    [System.Serializable]
    public class CannonCostMultiplier
    {
        public string cannonId;       // e.g. "CANNON_01"
        public float coinMultiplier;  // 1.0, 1.2, 1.5, ...
        public float matMultiplier;   // 1.0, 1.2, 1.5, ...
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

    // 7. Unlock cost (optional formula-based)
    public float GetBaseUnlockCoinsForChapter(int unlockChapter)
    {
        float baseUnlock = 1500f;
        float mu = 1f + 0.10f * Mathf.Pow(unlockChapter - 1, 1.1f);
        return baseUnlock * mu;
    }

    public int GetCannonUnlockCoins(string cannonId, int unlockChapter)
    {
        float baseCoins = GetBaseUnlockCoinsForChapter(unlockChapter);
        float mult      = GetCannonCostMultiplier(cannonId);
        return Mathf.RoundToInt(baseCoins * mult);
    }

    public int GetCannonUnlockGems(int coinCost)
    {
        float coinsPerGem = 110f; // soft→hard rate, tunable
        return Mathf.CeilToInt(coinCost / coinsPerGem);
    }

    // 8. Upgrade cost curve (Single Shot template)
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

    // =========================
    //  CHEST SCALING
    // =========================

    public float GetChestCoinsMin(int chapter)
    {
        float baseMin = 3000f;
        float mc = 1f + 0.06f * Mathf.Pow(chapter - 1, 1.15f);
        return baseMin * mc;
    }

    public float GetChestCoinsMax(int chapter)
    {
        return GetChestCoinsMin(chapter) * 1.5f;
    }

    public int RollChestCoins(int chapter)
    {
        int min = Mathf.RoundToInt(GetChestCoinsMin(chapter));
        int max = Mathf.RoundToInt(GetChestCoinsMax(chapter)) + 1;
        return Random.Range(min, max);
    }
}
