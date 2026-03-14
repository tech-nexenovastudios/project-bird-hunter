using UnityEngine;

namespace Gameplay.Player
{
    [CreateAssetMenu(fileName = "BulletConfig", menuName = "BirdHunter/Cannon/Bullets/BulletConfig")]
    public class BulletConfig : ScriptableObject
    {
        [Header("Stats")]
        public float baseDamage  = 10f;
        public float baseSpeed   = 15f;
        public float baseSize    = 1f;
        public int   pierceCount = 0;
        public int   bounceCount = 0;

        [Header("Lifetime")]
        public float lifetime = 5f;

        [Header("Physics")]
        public float mass         = 1f;
        public float impulseScale = 1f;

        [Header("Visual")]
        public GameObject    bulletPrefab;
        public TrailRenderer trail;
        public ParticleSystem hitEffect;

        [Header("Upgrade Increments")]
        public float damageIncrement = 1f;
        public float speedIncrement  = 0.5f;

        [Header("Upgrade Limits")]
        public float maxDamage = 200f;
        public float maxSpeed  = 30f;

        // ─────────────────────────────────────────
        // Runtime values
        // ─────────────────────────────────────────
        [HideInInspector] public float currentDamage;
        [HideInInspector] public float currentSpeed;
        [HideInInspector] public float currentSize;

        // ─────────────────────────────────────────
        // Multipliers
        // ─────────────────────────────────────────
        [HideInInspector] public float damageMultiplier = 1f;
        [HideInInspector] public float speedMultiplier  = 1f;

        // Base cache
        private float _baseDamage;
        private float _baseSpeed;
        private bool  _baseCached;

        public void InitRuntime()
        {
            currentDamage = baseDamage;
            currentSpeed  = baseSpeed;
            currentSize   = baseSize;
        }

        public void ApplyProgression(int globalLevel)
        {
            float t = Mathf.Clamp01(globalLevel / 600f);
            damageMultiplier = 1f + 8.5f * t;
            speedMultiplier  = 1f + 1.0f * t;
            UpdateRuntime();
        }

        public void UpdateRuntime()
        {
            currentDamage = Mathf.Min(baseDamage * damageMultiplier, maxDamage);
            currentSpeed  = Mathf.Min(baseSpeed  * speedMultiplier,  maxSpeed);
            currentSize   = baseSize;
        }

        public void CacheBaseValues()
        {
            if (_baseCached) return;
            _baseDamage = baseDamage;
            _baseSpeed  = baseSpeed;
            InitRuntime();
            _baseCached = true;
        }

        public void ResetToBase()
        {
            if (!_baseCached) { CacheBaseValues(); return; }
            baseDamage = _baseDamage;
            baseSpeed  = _baseSpeed;
            InitRuntime();
        }

        public void Upgrade()
        {
            baseDamage = Mathf.Min(baseDamage + damageIncrement, maxDamage);
            baseSpeed  = Mathf.Min(baseSpeed  + speedIncrement,  maxSpeed);
            InitRuntime();
        }
    }
}
