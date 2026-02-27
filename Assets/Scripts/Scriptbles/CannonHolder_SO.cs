using UnityEngine;

[CreateAssetMenu(fileName = "CannonHolder", menuName = "BirdHunter/Cannon/Holder")]
public class CannonHolder_SO : ScriptableObject
{
    [System.Serializable]
    public class CannonData
    {
        [Header("Identity")]
        public int    cannonIndex;
        public string cannonName;
        public string cannonDescription;
        public Sprite cannonSprite;
        public bool   cannonUnlock;

        [Header("Prefabs")]
        public GameObject cannonPrefab;
        public GameObject bulletPrefab;

        [Header("Stats")]
        public CannonStats cannonStats;
        
        [Header("Upgrade Economy")]
        public int   cannonLevel;
        public float coinsRequired;
        public float cardsRequired;
        public float coinIncrement              = 1f;
        public int   normalCardIncrement;
        public int   specificNumberCardIncrement;
        public int   moduloForCardUpgrade       = 5;

        // ── Base value cache ──
        [HideInInspector] public float baseCoinsRequired;
        [HideInInspector] public float baseCoinIncrement;
        [HideInInspector] public float baseCardsRequired;
        [HideInInspector] public int   baseCannonLevel;
        private bool _baseCached;

        public void CacheBaseValues()
        {
            if (_baseCached) return;
            baseCoinsRequired = coinsRequired;
            baseCoinIncrement = coinIncrement;
            baseCardsRequired = cardsRequired;
            baseCannonLevel   = cannonLevel;
            cannonStats?.CacheBaseValues();
            _baseCached = true;
        }

        public void ResetToBase()
        {
            if (!_baseCached) { CacheBaseValues(); return; }
            coinsRequired = baseCoinsRequired;
            coinIncrement = baseCoinIncrement;
            cardsRequired = baseCardsRequired;
            cannonLevel   = baseCannonLevel;
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
