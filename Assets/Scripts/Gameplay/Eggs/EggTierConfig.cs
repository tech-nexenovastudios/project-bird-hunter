using UnityEngine;

namespace Gameplay.Eggs
{
    [CreateAssetMenu(menuName = "BirdHunter/Egg Tier Config")]
    public class EggTierConfig : ScriptableObject
    {
        [Header("Basic Settings")]
        public string tierId;            // "E1", "E2", ...
        public int baseHpMin;
        public int baseHpMax;
        public int pressureValue;        // 1,2,4,8
        
        [Header("Damage Settings")]
        public int cannonDamage;
        
        [Header("Score Settings")]
        public int scorePerHit;
        public int scoreOnDestroy;
        
        [Header("Prefab")]
        public GameObject eggPrefab;
        public GameObject crackedEggPrefab;
        
        [Header("Bounce Settings")]
        public float bounceVelocity = 8f;       // Y velocity on ground hit
        public float bounceIncrease = 1.5f;     // extra bounce for smaller eggs
        public float horizontalIncrease = 1.2f; // horizontal push multiplier
        public float maxSpeed = 12f;            // clamp speed for safety
        
        [Header("Physics Settings")]
        public float gravityScale = 1f;
        public float mass;
        public float linearDamping;
        
        [Header("Split Settings")]
        public float splitForce = 4f;
        public EggTierConfig splitInto;  // null for E1
        public int splitCount = 2;       // 2 for E4→E3×2, etc.
        
        public Vector2 GetSplitImpulse(int splitIndex)
        {
            Vector2 dir = splitIndex == 0 ? Vector2.left : Vector2.right;
            return new Vector2(dir.x * splitForce * horizontalIncrease, splitForce);
        }
        
        public float GetBounceVelocityForSplit()
        {
            return bounceVelocity + bounceIncrease;
        }
    }
}