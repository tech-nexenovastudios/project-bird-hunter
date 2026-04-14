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
        public int scorePerHit;
        public int scoreOnDestroy;
        
        [Header("Damage Settings")]
        public SpriteRenderer spriteRenderer;
        public ParticleSystem hitEffect;
        public ParticleSystem destroyEffect;
        public AudioClip hitSound;
        public AudioClip[] destroySounds;
        
        public GameObject eggPrefab;
        
        // [Header("Bounce Settings")]
        // [Tooltip("Base Y velocity applied on ground bounce")]
        // public float bounceVelocity = 8f;
        
        [Tooltip("Extra bounce velocity for smaller eggs (multiplicative)")]
        public float bounceIncrease = 1.5f;
        
        [Tooltip("Horizontal velocity multiplier during bounces")]
        public float horizontalIncrease = 1.2f;
        
        [Tooltip("Maximum velocity clamp for safety")]
        public float maxSpeed = 12f;
        
        [Header("Physics Settings")]
        public float gravityScale = 1f;
        public float mass;
        public float linearDamping;
        [Space(10)]
        public float desiredBounceHeight;
        
        
        [Header("Split Settings")]
        public float splitForce = 4f;
        public EggTierConfig splitInto;  // null for E1
        public int splitCount = 2;       // 2 for E4→E3×2, etc.
        public int cannonDamage = 10;

        public Vector2 GetSplitImpulse(int splitIndex)
        {
            Vector2 dir = splitIndex == 0 ? Vector2.left : Vector2.right;
            return new Vector2(dir.x * splitForce * horizontalIncrease, splitForce);
        }
        
        // public float GetBounceVelocityForSplit()
        // {
        //     return bounceVelocity + bounceIncrease;
        // }
    }
}