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
        
        private const float EGG_LAY_COOLDOWN = 1.5f;
        private const float CANNON_ALIGN_THRESHOLD = 0.5f;

        protected bool _isDead;
        protected bool _isInScreen;

        protected float _layTimer;
        private float _remainingLifetime;
        private BoxCollider2D _collider;
        private float _halfWidth;
        
        private float _lastLayX;
        private Transform _eggSpawnPoint;
        public Transform EggSpawnPoint => _eggSpawnPoint ??= transform.GetChild(0);

        private float _currentSpeedMultiplier = 1f;
        public float SpeedMultiplier => _currentSpeedMultiplier;

        protected IBirdMovementStrategy _movementStrategy;
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
            
            _layTimer = EGG_LAY_COOLDOWN;
            _remainingLifetime = config.lifetime;

            _collider = GetComponent<BoxCollider2D>();
            _halfWidth = _collider.bounds.extents.x;
            _lastLayX = -999f;

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

        protected void HandleScreenTime()
        {
            float x = transform.position.x;
            _isInScreen = (x + _halfWidth) >= ScreenBounds.minX
                       && (x - _halfWidth) <= ScreenBounds.maxX;
        }

        private void HandleTimers()
        {
            if (_layTimer > 0f)
                _layTimer -= Time.deltaTime;

            if (_isInScreen && _layTimer <= 0f)
            {
                var gm = Managers.GameManager.Instance;
                if (gm != null && gm.currentCannon != null)
                {
                    float cannonX = gm.currentCannon.transform.position.x;
                    float birdX = transform.position.x;

                    if (Mathf.Abs(birdX - cannonX) < CANNON_ALIGN_THRESHOLD)
                    {
                        if (Mathf.Abs(birdX - _lastLayX) > CANNON_ALIGN_THRESHOLD * 2f)
                        {
                            OnLayEgg?.Invoke(this);
                            _layTimer = EGG_LAY_COOLDOWN;
                            _lastLayX = birdX;
                        }
                    }
                }
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