using System;
using Gameplay.Events;
using Gameplay.Health;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.Birds
{
    /// <summary>
    /// Base class for every attacking-bird variant.
    /// Handles HP, lifetime, movement entry-point, and the attack-tick loop.
    /// Concrete subtypes override <see cref="OnAttackTick"/> and
    /// optionally <see cref="OnHpThresholdReached"/> (used by Suicide bird).
    ///
    /// LIFECYCLE
    ///   SpawnController creates the prefab, calls Init(), then subscribes to OnDestroyed.
    ///   When the bird dies or expires, OnDestroyed fires and SpawnController cleans up.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class AttackingBird : MonoBehaviour, IDamageable
    {
        // ── Events ────────────────────────────────────────────────────────
        public event Action<AttackingBird> OnDestroyed;

        // ── Public State ──────────────────────────────────────────────────
        public AttackingBirdConfig Config     { get; private set; }
        public int                 CurrentHp  { get; private set; }
        public int                 MaxHp      { get; private set; }
        public bool                IsAlive    => !_isDead;

        // ── Private ───────────────────────────────────────────────────────
        protected bool  _isDead;
        private float   _attackTimer;
        private float   _lifetimeTimer;
        private bool    _thresholdFired;   // for Suicide / phase triggers

        // ─────────────────────────────────────────────────────────────────
        // Initialisation
        // ─────────────────────────────────────────────────────────────────

        public virtual void Init(AttackingBirdConfig config, float hpMultiplier = 1f)
        {
            Config         = config;
            MaxHp          = Mathf.RoundToInt(config.baseHp * hpMultiplier);
            CurrentHp      = MaxHp;
            _isDead        = false;
            _thresholdFired = false;
            _attackTimer   = config.attackInterval; // first attack after one full interval
            _lifetimeTimer = config.lifetime;

            OnInit();
        }

        /// <summary>Override to run type-specific setup after Init() (e.g. formation building for BeeHurdle).</summary>
        protected virtual void OnInit() { }

        // ─────────────────────────────────────────────────────────────────
        // Unity Loop
        // ─────────────────────────────────────────────────────────────────

        protected virtual void Update()
        {
            if (_isDead) return;

            // Lifetime
            _lifetimeTimer -= Time.deltaTime;
            if (_lifetimeTimer <= 0f)
            {
                Die(false);
                return;
            }

            // Attack tick
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                OnAttackTick();
                _attackTimer = Config.attackInterval;
            }

            // 50% HP threshold hook (used by SuicideBird)
            if (!_thresholdFired && CurrentHp <= MaxHp * 0.5f)
            {
                _thresholdFired = true;
                OnHpThresholdReached();
            }

            OnMovementTick();
        }

        // ─────────────────────────────────────────────────────────────────
        // Abstract / Virtual hooks for subtypes
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Called every attack interval. Fire projectile, drop payload, etc.</summary>
        protected abstract void OnAttackTick();

        /// <summary>Called every Update frame for movement logic.</summary>
        protected virtual void OnMovementTick() { }

        /// <summary>Called once when HP drops to or below 50%. Suicide bird dives here.</summary>
        protected virtual void OnHpThresholdReached() { }

        // ─────────────────────────────────────────────────────────────────
        // IDamageable
        // ─────────────────────────────────────────────────────────────────

        public void TakeDamage(int damage)
        {
            if (_isDead) return;
            CurrentHp -= damage;
            if (CurrentHp <= 0)
                Die(true);
        }

        public void ApplyEffect(IEffect<IDamageable> effect)
        {
            if (_isDead) return;
            effect.Apply(this);
        }

        public void ForceKill()
        {
            if (_isDead) return;
            Die(false);
        }

        // ─────────────────────────────────────────────────────────────────
        // Death
        // ─────────────────────────────────────────────────────────────────

        protected virtual void Die(bool killedByPlayer)
        {
            if (_isDead) return;
            _isDead = true;

            if (killedByPlayer && Config != null)
                GameEvents.FireAttackingBirdDestroyed(this, Config.scoreOnDefeat);

            OnDestroyed?.Invoke(this);
        }
    }
}