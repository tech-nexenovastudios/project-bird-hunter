using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Gameplay.Health;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using Random = UnityEngine.Random;

namespace Gameplay.Birds
{
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class BaseBird : MonoBehaviour, IEntity, IFreezable
    {
        [Header("Movement Points")]
        [SerializeField] private Vector3[] movePoints;
        public Vector3[] MovePoints => movePoints;

        public BirdConfig config { get; private set; }

        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsAlive => !_isDead && CurrentHp > 0;

        public event Action<BaseBird> OnLayEgg;
        public event Action<BaseBird> OnDestroyed;

        protected void InvokeLayEgg() => OnLayEgg?.Invoke(this);

        protected bool _isDead;
        protected bool _isInScreen;

        protected float _layTimer;
        private float _remainingLifetime;
        private float _noiseOffset;
        private BoxCollider2D _collider;
        private float _halfWidth;

        private float _currentSpeedMultiplier = 1f;
        public float SpeedMultiplier => _currentSpeedMultiplier;

        private IBirdMovementStrategy _movementStrategy;
        private readonly List<IEffect<IEntity>> activeEffects = new();

        public virtual void Init(BirdConfig birdConfig, int hp)
        {
            config = birdConfig;
            MaxHp = hp;
            CurrentHp = hp;
            _isDead = false;
            _currentSpeedMultiplier = 1f;

            var birdHealth = GetComponent<BirdHealth>();
            if (birdHealth != null)
                birdHealth.Init(birdConfig, hp);

            _layTimer = Random.Range(config.layIntervalMin, config.layIntervalMax);
            _remainingLifetime = config.lifetime;
            _noiseOffset = Random.Range(0f, 100f);

            _collider = GetComponent<BoxCollider2D>();
            _halfWidth = _collider.bounds.extents.x;

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
            float x = transform.position.x;
            _isInScreen = (x + _halfWidth) >= ScreenBounds.minX
                       && (x - _halfWidth) <= ScreenBounds.maxX;
        }

        private void HandleTimers()
        {
            _layTimer -= Time.deltaTime;

            if (_layTimer <= 0f)
            {
                if (_isInScreen)
                    OnLayEgg?.Invoke(this);

                float noise = Mathf.PerlinNoise(_noiseOffset, Time.time * 0.5f);
                float nextDelay = Mathf.Lerp(config.layIntervalMin, config.layIntervalMax, noise);

                if (Random.value < 0.15f)
                    nextDelay *= 0.25f;

                _layTimer = nextDelay;
                _noiseOffset += 1.73f;
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
            if (CurrentHp <= 0) Die(true);
        }

        public void Heal(int amount)
        {
            if (_isDead || amount <= 0) return;
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        }

        public void ApplyFreeze(float slowPercent)
        {
            _currentSpeedMultiplier = 1f - Mathf.Clamp01(slowPercent);
        }

        public void RemoveFreeze()
        {
            _currentSpeedMultiplier = 1f;
        }

        public void ApplyEffect(IEffect<IEntity> effect)
        {
            if (_isDead) return;
            effect.OnCompleted += RemoveEffect;
            activeEffects.Add(effect);
            effect.Apply(this);
        }

        private void RemoveEffect(IEffect<IEntity> effect)
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