using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Gameplay.Health;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using Random = UnityEngine.Random;

namespace Gameplay.Birds
{
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class BaseBird : MonoBehaviour, IDamageable
    {
        [Header("Movement Points")]
        [SerializeField] private Vector3[] movePoints;
        public Vector3[] MovePoints => movePoints;

        public BirdConfig config { get; private set; }
        public int CurrentHp { get; private set; }
        
        public int MaxHp { get; }
        public bool IsAlive { get; }

        public event Action<BaseBird> OnLayEgg;
        public event Action<BaseBird> OnDestroyed;

        /// <summary>
        /// Raises OnLayEgg. Subclasses (e.g. BossBird) must call this instead of
        /// invoking OnLayEgg directly, because C# events can only be invoked from
        /// within the declaring class.
        /// </summary>
        protected void InvokeLayEgg() => OnLayEgg?.Invoke(this);

        protected bool _isDead;
        protected bool _isInScreen;

        private float _layTimer;
        private float _remainingLifetime;
        private float _noiseOffset;   // unique per bird — add to Init
        private BoxCollider2D _collider;

        private IBirdMovementStrategy _movementStrategy;
        readonly List<IEffect<IDamageable>> activeEffects = new();

        public virtual void Init(BirdConfig birdConfig, int hp)
        {
            config    = birdConfig;
            CurrentHp = hp;
            _isDead   = false;

            var birdHealth = GetComponent<BirdHealth>();
            if (birdHealth != null)
                birdHealth.Init(birdConfig, hp);

            // ✅ Random start offset — birds spawned together won't lay in sync
            _layTimer          = Random.Range(config.layIntervalMin, config.layIntervalMax);
            _remainingLifetime = config.lifetime;
            _noiseOffset       = Random.Range(0f, 100f);

            // ✅ Cache collider once
            _collider = GetComponent<BoxCollider2D>();

            _movementStrategy = BirdMovementFactory.Create(BirdMovementType.NormalMove);
            _movementStrategy.Initialize(this, config);
        }

        protected virtual void Update()
        {
            if (_isDead) return;

            HandleTimers();
            HandleScreenTime();
            _movementStrategy?.Tick();
        }

        private void HandleScreenTime()
        {
            // Use half the collider width so check aligns with visual edges not just pivot
            float halfWidth = GetComponent<BoxCollider2D>().bounds.extents.x;

            _isInScreen = (transform.position.x + halfWidth) >= ScreenBounds.minX
                          && (transform.position.x - halfWidth) <= ScreenBounds.maxX;
        }

        private void HandleTimers()
        {
            _layTimer -= Time.deltaTime;

            if (_layTimer <= 0f)
            {
                if (_isInScreen)
                    OnLayEgg?.Invoke(this);

                // Perlin noise gives smooth but unpredictable variance
                // Sample moves along time axis — no two birds feel the same
                float noise     = Mathf.PerlinNoise(_noiseOffset, Time.time * 0.5f); // 0..1
                float nextDelay = Mathf.Lerp(config.layIntervalMin, config.layIntervalMax, noise);

                // Occasional burst: 15% chance of a very short follow-up lay
                if (Random.value < 0.15f)
                    nextDelay *= 0.25f;

                _layTimer    = nextDelay;
                _noiseOffset += 1.73f;   // irrational step — avoids repeating pattern
            }

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
                Die(false);
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            CurrentHp -= damage;
            Debug.Log($"[Gameplay] Bird took {damage} damage. Health now {CurrentHp}");

            if (CurrentHp <= 0)
                Die(true);
        }
        public void ApplyEffect(IEffect<IDamageable> effect)
        {
            if (CurrentHp <= 0) return; // Dead enemies should't receive effects

            effect.OnCompleted += RemoveEffect;
            activeEffects.Add(effect);
            effect.Apply(this);
        }
        void RemoveEffect(IEffect<IDamageable> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            activeEffects.Remove(effect);
        }
        public void ForceKill()
        {
            if (_isDead) return;
            Die(false);
        }
        protected virtual void Die(bool killedByPlayer)
        {
            if (_isDead) return;

            _isDead = true;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel();
            }

            activeEffects.Clear();
            _movementStrategy?.Dispose();
            
            DOTween.Kill(transform);

            OnDestroyed?.Invoke(this);
        }

        protected virtual void OnDisable()
        {
            DOTween.Kill(transform);
            _movementStrategy?.Dispose();
        }
    }
}