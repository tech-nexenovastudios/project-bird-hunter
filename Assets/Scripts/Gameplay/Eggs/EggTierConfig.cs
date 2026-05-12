using UnityEngine;

namespace Gameplay.Eggs
{
    [CreateAssetMenu(menuName = "BirdHunter/Egg Tier Config")]
    public class EggTierConfig : ScriptableObject
    {
        [Header("Identity")]
        public string tierId;
        public int baseHpMin;
        public int baseHpMax;
        public int pressureValue;
        public int scorePerHit;
        public int scoreOnDestroy;
        public GameObject eggPrefab;

        [Header("Physics")]
        public float bounceIncrease = 1.5f;
        public float horizontalIncrease = 1.0f;
        public float maxSpeed = 12f;
        public float gravityScale = 1f;
        [Range(0.05f, 1f)] public float bounceHeightPercent = 0.5f;

        [Header("Split")]
        public float splitForce = 4f;
        public EggTierConfig splitInto;
        public int splitCount = 2;
        public int cannonDamage = 10;

        [Header("Personality (HP-modulated)")]
        public Vector2 sizeRange = new Vector2(0.95f, 1.05f);
        public Vector2 bounceFrequencyRange = new Vector2(2.0f, 4.5f);
        public Vector2 bounceAmplitudeRange = new Vector2(0.04f, 0.10f);

        [Header("Hit Reaction")]
        public float squashOnHit = 1.15f;
        public float stretchOnHit = 0.85f;

        [Header("Ground Bounce")]
        public float groundSquashX = 1.30f;
        public float groundSquashY = 0.65f;
        public float groundSquashDuration = 0.18f;
        [Range(0f, 1f)] public float bounceHeightDecay = 0.15f;

        [Header("Flight Lean")]
        public float maxLeanDegrees = 15f;
        public float leanSmoothing = 8f;

        [Header("Bullet Impulse Accumulator")]
        public float bulletImpulsePerHit = 1.0f;
        public float bulletImpulseDecay = 1.5f;
        public float evasiveLaunchThreshold = 4.0f;
        public float cannonDangerRadius = 4.0f;
        public float cannonProximityMultiplier = 2.0f;

        public Vector2 GetSplitImpulse(int splitIndex)
        {
            Vector2 dir = splitIndex == 0 ? Vector2.left : Vector2.right;
            return new Vector2(dir.x * splitForce * horizontalIncrease, splitForce);
        }
    }
}
