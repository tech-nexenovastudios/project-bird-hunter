using UnityEngine;

namespace Gameplay.Eggs
{
    [CreateAssetMenu(menuName = "BirdHunter/Egg Tier Config")]
    public class EggTierConfig : ScriptableObject
    {
        public string tierId;            // "E1", "E2", ...
        public int baseHpMin;
        public int baseHpMax;
        public int pressureValue;        // 1,2,4,8
        public EggTierConfig splitInto;  // null for E1
        public int splitCount = 2;       // 2 for E4→E3×2, etc.
        public int scorePerHit;
        public int scoreOnDestroy;
        public float jumpImpulse;
        
        //public Sprite sprite;
        public GameObject eggPrefab;
    }
}