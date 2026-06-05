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
        [Tooltip("Multiplier on how far sideways the egg flings itself during an evasive escape leap (launch speed = maxSpeed × this × 0.5). 1.0 = a normal sideways dash away from the cannon; raise to 2.0 for eggs that rocket across the screen to dodge, lower toward 0 for ones that mostly jump straight up.")]
        public float horizontalIncrease = 1.0f;

        [Tooltip("Top horizontal speed (world units/sec) the egg's sideways drift is clamped to, and the reference used for flight lean. e.g. 12 lets an egg skim briskly across the play area; drop to 6 for sluggish, easy-to-track eggs in early levels, raise for frantic late-game ones.")]
        public float maxSpeed = 12f;

        [Tooltip("Unity Rigidbody2D gravity multiplier. Higher = the egg falls and snaps back down faster (snappier, heavier-feeling bounces); lower = floaty, slow-falling eggs. e.g. 1 = normal weight, 2 = drops twice as fast, 0.5 = drifts down lazily.")]
        public float gravityScale = 1f;

        [Tooltip("How high the egg bounces, as a fraction of the screen height. 0.5 = bounces halfway up the screen; 1.0 = nearly to the top; 0.2 = low, shallow hops. The actual launch velocity is derived from this and gravityScale so the apex stays consistent.")]
        [Range(0.05f, 1f)] public float bounceHeightPercent = 0.5f;

        [Tooltip("Seconds the egg hovers at its bounce apex before gravity resumes. 0 = no hang.")]
        [Min(0f)] public float apexHangDuration = 0f;

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
        [Tooltip("ON: bullet hits shake the egg upward + can trigger an evasive vertical launch. OFF: bullet hits halt the egg's vertical motion entirely (no upward push, no downward drop from the collision) — the egg appears to absorb the hit in place.")]
        public bool enableBulletUpwardPush = true;

        [Tooltip("Charge each bullet hit adds to the egg's evasion meter. Higher = the egg flinches harder and panics sooner. e.g. 1.0 with a threshold of 4.0 means ~4 clean hits to provoke an escape leap; raise to 2.0 and just 2 hits set it off — good for jumpy late-game eggs.")]
        public float bulletImpulsePerHit = 1.0f;

        [Tooltip("How fast the evasion meter drains per second when the egg is NOT being hit (units/sec). Higher = the egg calms down quickly, so you must keep up sustained fire to make it bolt. e.g. 1.5 means a fully panicked egg settles in a few seconds of no fire. Set 0 to make charge permanent until it launches.")]
        public float bulletImpulseDecay = 1.5f;

        [Tooltip("Evasion meter value that triggers an evasive vertical launch (egg leaps up and away from the cannon). Lower = twitchier, escapes from just a couple of hits; higher = tanky, only bolts under heavy fire. e.g. 4.0 ≈ 4 base hits before it jumps.")]
        public float evasiveLaunchThreshold = 4.0f;

        [Tooltip("Horizontal distance (world units) from the cannon within which an egg feels 'threatened' and gains a proximity bonus to its charge. e.g. 4.0 means an egg drifting directly overhead charges fastest, while one near the screen edge gets no bonus. Set 0 to disable the proximity bonus entirely.")]
        public float cannonDangerRadius = 4.0f;

        [Tooltip("How strongly being right above the cannon amplifies charge per hit. Final deposit = bulletImpulsePerHit × (1 + proximity × thisValue), where proximity is 1 directly over the cannon and 0 at the danger radius edge. e.g. 2.0 means a hit landed on an egg sitting on the cannon counts ~3× — it scrambles away almost instantly when cornered.")]
        public float cannonProximityMultiplier = 2.0f;
    }
}
