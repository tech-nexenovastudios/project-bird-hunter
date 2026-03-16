using UnityEngine;

/// <summary>
///   Index 0 – Single Shot  : unlockChapterRequired=0 (Ch1), coinCost=0,      gemCost=0
///   Index 1 – Rapid Fire   : unlockChapterRequired=1 (Ch2), coinCost=1500,   gemCost=40
///   Index 2 – Shot Gun     : unlockChapterRequired=3 (Ch4), coinCost=4000,   gemCost=80
///   Index 3 – Lucky Cannon : unlockChapterRequired=6 (Ch7), coinCost=8000,   gemCost=120
///   Index 4 – Big Bartha   : unlockChapterRequired=10(Ch11),coinCost=15000,  gemCost=240
///   Index 5 – Double Bullet: unlockChapterRequired=15(Ch16),coinCost=25000,  gemCost=360
///   Index 6 – Triple Bullet: unlockChapterRequired=21(Ch22),coinCost=40000,  gemCost=520
/// </summary>
[CreateAssetMenu(fileName = "CannonHolder", menuName = "BirdHunter/Cannon/Holder")]
public class CannonHolder_SO : ScriptableObject
{
    [System.Serializable]
    public class CannonData
    {
        // ── Identity ──────────────────────────────────────────────────────────
        [Header("Identity")]
        public int cannonIndex;
        public string cannonName;
        public string cannonDescription;
        public Sprite cannonSprite;
        public bool cannonUnlock;

        // ── Prefabs ───────────────────────────────────────────────────────────
        [Header("Prefabs")]
        public GameObject cannonPrefab;
        public GameObject bulletPrefab;

        // ── Stats ─────────────────────────────────────────────────────────────
        [Header("Stats")]
        public CannonStats cannonStats;

        // ── Upgrade Economy ───────────────────────────────────────────────────
        [Header("Upgrade Economy")]
        public int cannonLevel = 1;
        public float coinsRequired;
        public float cardsRequired;
        public float coinIncrement = 1f;
        public int normalCardIncrement;
        public int specificNumberCardIncrement;
        public int moduloForCardUpgrade = 5;

        // ── Unlock Requirements ───────────────────────────────────────────────
        /// <summary>
        /// The chapter index (0-based) that must be unlocked before this cannon
        /// can even be purchased. Example: Rapid Fire requires Ch2 → set to 1.
        /// First cannon (Single Shot) is always free → set to 0.
        /// </summary>
        [Header("Unlock Requirements")]
        [Tooltip("0-based chapter index that must be unlocked first (Ch1=0, Ch2=1, …)")]
        public int unlockChapterRequired;

        [Tooltip("Cost in Gold coins. Set 0 for free cannons.")]
        public int coinCost;

        [Tooltip("Cost in Gems (alternative to coins). Set 0 for free cannons.")]
        public int gemCost;

        // ── Base value cache (runtime, not saved) ─────────────────────────────
        [HideInInspector] public float baseCoinsRequired;
        [HideInInspector] public float baseCoinIncrement;
        [HideInInspector] public float baseCardsRequired;
        [HideInInspector] public int baseCannonLevel;
        private bool _baseCached;

        // ─────────────────────────────────────────────────────────────────────
        // Base-value helpers (used by upgrade system)
        // ─────────────────────────────────────────────────────────────────────

        public void CacheBaseValues()
        {
            if (_baseCached) return;
            baseCoinsRequired = coinsRequired;
            baseCoinIncrement = coinIncrement;
            baseCardsRequired = cardsRequired;
            baseCannonLevel = cannonLevel;
            cannonStats?.CacheBaseValues();
            _baseCached = true;
        }

        public void ResetToBase()
        {
            if (!_baseCached) { CacheBaseValues(); return; }
            coinsRequired = baseCoinsRequired;
            coinIncrement = baseCoinIncrement;
            cardsRequired = baseCardsRequired;
            cannonLevel = baseCannonLevel;
            cannonStats?.ResetToBase();
        }

        public void ReplayToLevel(int targetLevel)
        {
            ResetToBase();
            for (int i = 0; i < targetLevel; i++)
                UpgradeCannon();
        }

        public void UpgradeCannon()
        {
            if (cannonStats == null)
                cannonStats = ScriptableObject.CreateInstance<CannonStats>();

            coinsRequired += coinIncrement;
            coinIncrement += 1f;

            int levelCheck = cannonLevel % Mathf.Max(1, moduloForCardUpgrade);
            cardsRequired += (levelCheck == 0)
                ? specificNumberCardIncrement
                : normalCardIncrement;

            cannonStats.Upgrade(cannonLevel);
            cannonLevel++;
        }
    }

    public CannonData[] cannonsData;
}