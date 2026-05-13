using System;
using System.Collections;
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
        // Fired the moment HP reaches zero (or ForceKill). Subclasses subscribe here to trigger
        // their death visuals (e.g., NormalBird plays the Spine "death" clip). OnDestroyed fires
        // AFTER the death sequence completes — used by SpawnController for cleanup.
        public event Action<BaseBird> OnDeathStarted;

        protected void InvokeLayEgg() => OnLayEgg?.Invoke(this);

        [Header("Death Sequence")]
        [SerializeField] private float deathSequenceDuration = 1.0f;
        [SerializeField] private float deathFallDistance = 3.0f;
        [SerializeField] private float deathSpinMagnitude = 180f;

        // Once lifetime hits zero, give the bird up to this many extra seconds to fly fully
        // offscreen before force-despawning. Prevents the "bird disappears mid-flight" bug.
        private const float OffscreenGraceSeconds = 3.0f;

        private const float CANNON_CROSS_TOLERANCE = 1.0f;

        protected bool _isDead;
        protected bool _isInScreen;

        protected float _layCooldown;
        private float _fallbackTimer;
        private float _prevBirdX;
        private bool _hasPrevBirdX;
        private float _remainingLifetime;
        private BoxCollider2D _collider;
        private float _halfWidth;

        // One-lay model: each bird drops exactly one egg, then enters "flee" mode
        // (faster movement, short remaining lifetime). RewardManager reads HasLaid
        // to differentiate "kill-before-lay" (prevention) vs "kill-after-lay" (chase) rewards.
        public bool HasLaid { get; private set; }
        protected const float PostLaySpeedMultiplier = 1.6f;
        protected const float PostLayRemainingLifetime = 3f;

        private Transform _eggSpawnPoint;
        public Transform EggSpawnPoint => _eggSpawnPoint ??= transform.GetChild(0);

        private float _currentSpeedMultiplier = 1f;
        public float SpeedMultiplier => _currentSpeedMultiplier;

        protected IBirdMovementStrategy _movementStrategy;
        private readonly List<IEffect<IEntity>> activeEffects = new();
        private SpriteRenderer _spriteRenderer;

        // movementOverride lets SpawnController pick a movement type per-spawn (random/weighted)
        // without mutating the shared BirdConfig asset. Passing null falls back to the config value.
        public virtual void Init(BirdConfig birdConfig, int hp, BirdMovementType? movementOverride = null)
        {
            config = birdConfig;
            MaxHp = hp;
            CurrentHp = hp;
            _isDead = false;
            _currentSpeedMultiplier = 1f;
            HasLaid = false;

            var birdHealth = GetComponent<BirdHealth>();
            if (birdHealth != null)
                birdHealth.Init(birdConfig, hp);
            
            _layCooldown = config.layIntervalMin;
            _fallbackTimer = config.layIntervalMax;
            _hasPrevBirdX = false;
            _prevBirdX = 0f;
            _remainingLifetime = config.lifetime;

            _collider = GetComponent<BoxCollider2D>();
            _halfWidth = _collider.bounds.extents.x;
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // SpawnController may override the movement type per-spawn (random weighted) for
            // visual variety; if no override is given, fall back to the BirdConfig default.
            var movementType = movementOverride ?? config.movementType;
            _movementStrategy = BirdMovementFactory.Create(movementType);
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
            _layCooldown -= Time.deltaTime;
            _fallbackTimer -= Time.deltaTime;

            float birdX = transform.position.x;
            var gm = Managers.GameManager.Instance;
            var cannon = gm != null ? gm.currentCannon : null;

            bool crossedCannon = false;
            if (cannon != null)
            {
                float cannonX = cannon.transform.position.x;
                if (_hasPrevBirdX)
                {
                    bool prevSide = _prevBirdX >= cannonX;
                    bool currSide = birdX >= cannonX;
                    crossedCannon = prevSide != currSide
                                    || Mathf.Abs(birdX - cannonX) < CANNON_CROSS_TOLERANCE;
                }
            }

            _prevBirdX = birdX;
            _hasPrevBirdX = true;

            // One-lay model: a bird drops a single egg, then enters flee mode. HasLaid gates
            // further attempts so a bird oscillating across the cannon line can't machine-gun eggs.
            if (!HasLaid && _isInScreen && _layCooldown <= 0f && (crossedCannon || _fallbackTimer <= 0f))
            {
                OnLayEgg?.Invoke(this);
                HasLaid = true;
                EnterFleeMode();
            }

            _remainingLifetime -= Time.deltaTime;
            // Grace period: if the bird is still on-screen when lifetime expires, give it up to
            // OffscreenGraceSeconds to exit before force-despawning. Movement strategies aim past
            // the edge so this almost always exits cleanly.
            if (_remainingLifetime <= 0f && (!_isInScreen || _remainingLifetime < -OffscreenGraceSeconds))
                Die(false);
        }
        public void FlipDirection(float direction)
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _spriteRenderer.flipX = direction < 0f;
        }
        // Bird has just dropped its single egg — speed up and shorten remaining lifetime so it
        // makes a brief "flee" pass for the player to chase. Subclasses can override to add a
        // distinct flee animation or tint without re-implementing the timing.
        protected virtual void EnterFleeMode()
        {
            _currentSpeedMultiplier *= PostLaySpeedMultiplier;
            _remainingLifetime = Mathf.Min(_remainingLifetime, PostLayRemainingLifetime);
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
            if (_collider != null) _collider.enabled = false;

            OnDeathStarted?.Invoke(this);
            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            float spin = Random.Range(-deathSpinMagnitude, deathSpinMagnitude);
            transform.DOMoveY(transform.position.y - deathFallDistance, deathSequenceDuration)
                .SetEase(Ease.InQuad);
            transform.DORotate(new Vector3(0f, 0f, spin), deathSequenceDuration)
                .SetEase(Ease.OutQuad);

            if (_spriteRenderer != null)
            {
                float fadeDuration = deathSequenceDuration * 0.5f;
                _spriteRenderer.DOFade(0f, fadeDuration).SetDelay(deathSequenceDuration - fadeDuration);
            }

            yield return new WaitForSeconds(deathSequenceDuration);
            OnDestroyed?.Invoke(this);
        }

        protected virtual void OnDisable()
        {
            DOTween.Kill(transform);
            _movementStrategy?.Dispose();
        }
    }

    public enum BirdMovementType
    {
        NormalMove,
        ZigZagMove,
        LeftRightMove,
        CurvePathMove,
        TargetMove,
        DiagonalMove,
    }
}