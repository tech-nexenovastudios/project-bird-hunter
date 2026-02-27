using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Birds
{
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class BaseBird : MonoBehaviour
    {
        [Header("Movement Points")]
        [SerializeField] private Vector3[] movePoints;
        public Vector3[] MovePoints => movePoints;

        public BirdConfig config { get; private set; }
        public int currentHp { get; private set; }

        public event Action<BaseBird> OnLayEgg;
        public event Action<BaseBird> OnDestroyed;

        protected bool _isDead;
        protected bool _isInScreen;

        private float _layTimer;
        private float _remainingLifetime;
        private float _noiseOffset;   // unique per bird — add to Init
        private BoxCollider2D _collider;

        private IBirdMovementStrategy _movementStrategy;

        public virtual void Init(BirdConfig birdConfig, int hp)
        {
            config    = birdConfig;
            currentHp = hp;
            _isDead   = false;

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

            currentHp -= damage;
            _ = PlayFX();

            if (currentHp <= 0)
                Die(true);
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

            _movementStrategy?.Dispose();
            DOTween.Kill(transform);

            OnDestroyed?.Invoke(this);
        }

        public virtual UniTask PlayFX() => UniTask.CompletedTask;

        protected virtual void OnDisable()
        {
            DOTween.Kill(transform);
            _movementStrategy?.Dispose();
        }
    }
}