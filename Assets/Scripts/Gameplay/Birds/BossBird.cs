using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Interfaces;

namespace Gameplay.Birds
{
    /// <summary>
    /// Boss bird: two-phase behaviour, burst egg drops, special attack.
    /// Appears on level 10 and level 20 (handled by SpawnController).
    ///
    /// Phase 1 (full HP → phase2Threshold): normal speed, normal egg tier, periodic lays.
    /// Phase 2 (below threshold):           faster speed, heavier egg tier, faster lays + burst.
    /// </summary>
    public class BossBird : BaseBird
    {
        // ── Events ────────────────────────────────────────────────────────
        /// <summary>Fired when the boss transitions from phase 1 → phase 2.</summary>
        public event Action<BossBird> OnPhase2Entered;

        /// <summary>Fired when the boss is defeated (killed by player).</summary>
        public event Action<BossBird> OnBossDefeated;

        /// <summary>Fired when the boss triggers a burst lay (special attack).</summary>
        public event Action<BossBird, int> OnBurstLay; // (boss, eggCount)

        // ── State ─────────────────────────────────────────────────────────
        public BossConfig BossConfig   { get; private set; }
        public bool       IsPhase2     { get; private set; }
        public bool       IsDefeated   { get; private set; }

        private float _layTimer;
        private float _specialAttackTimer;

        // ── Init ──────────────────────────────────────────────────────────

        /// <summary>
        /// Call this instead of the base Init() for boss birds.
        /// Passes bossConfig.totalHp to the base class.
        /// </summary>
        public void InitBoss(BossConfig bossConfig)
        {
            BossConfig = bossConfig;
            IsPhase2   = false;
            IsDefeated = false;

            // Build a minimal BirdConfig wrapper so BaseBird plumbing still works.
            // Movement, egg laying timer, and lifetime are overridden below.
            var tempConfig = ScriptableObject.CreateInstance<BirdConfig>();
            tempConfig.birdId         = bossConfig.bossId;
            tempConfig.moveSpeed      = bossConfig.phase1MoveSpeed;
            tempConfig.layIntervalMin = bossConfig.phase1LayInterval;
            tempConfig.layIntervalMax = bossConfig.phase1LayInterval;
            tempConfig.eggTier        = bossConfig.phase1EggTier;
            tempConfig.lifetime       = float.MaxValue; // boss does not expire naturally
            tempConfig.baseHp         = bossConfig.totalHp;

            base.Init(tempConfig, bossConfig.totalHp);

            // Own timers — BaseBird's _layTimer is private so we manage ours here
            _layTimer            = bossConfig.phase1LayInterval;
            _specialAttackTimer  = bossConfig.specialAttackInterval;

            Debug.Log($"[BossBird] {bossConfig.bossId} spawned — HP {bossConfig.totalHp}");
            GameEvents.FireBossSpawned(this);
        }

        // ── Update ────────────────────────────────────────────────────────

        protected override void Update()
        {
            // BaseBird.Update() handles movement and its own timers.
            // We override egg-lay timing entirely, so we skip base.Update()'s
            // HandleTimers() — instead we call the base movement tick only.
            if (_isDead) return;

            HandleBossTimers();
            CheckPhaseTransition();
        }

        private void HandleBossTimers()
        {
            float dt = Time.deltaTime;

            // ── Normal lay ──────────────────────────────────────────────
            _layTimer -= dt;
            if (_layTimer <= 0f)
            {
                InvokeLayEgg();
                _layTimer = IsPhase2
                    ? BossConfig.phase2LayInterval
                    : BossConfig.phase1LayInterval;
            }

            // ── Special attack (burst lay) ───────────────────────────────
            _specialAttackTimer -= dt;
            if (_specialAttackTimer <= 0f)
            {
                TriggerBurstAttack();
                _specialAttackTimer = BossConfig.specialAttackInterval;
            }
        }

        private void CheckPhaseTransition()
        {
            if (IsPhase2) return;
            if (BossConfig == null) return;

            float hpFraction = (float)CurrentHp / BossConfig.totalHp;
            if (hpFraction <= BossConfig.phase2Threshold)
                EnterPhase2();
        }

        // ── Phase 2 ───────────────────────────────────────────────────────

        private void EnterPhase2()
        {
            IsPhase2 = true;

            Debug.Log($"[BossBird] {BossConfig.bossId} entering PHASE 2!");

            // Update the config wrapper so SpawnController uses the right egg tier
            // when it handles our OnLayEgg event.
            // We fire a dedicated event so UI / VFX can react.
            OnPhase2Entered?.Invoke(this);
            GameEvents.FireBossPhase2(this);
        }

        /// <summary>
        /// Returns the egg tier appropriate for the current phase.
        /// SpawnController should call this when handling OnLayEgg for a BossBird.
        /// </summary>
        public Eggs.EggTierConfig GetCurrentEggTier()
            => IsPhase2 ? BossConfig.phase2EggTier : BossConfig.phase1EggTier;

        // ── Special Attack ────────────────────────────────────────────────

        private void TriggerBurstAttack()
        {
            Debug.Log($"[BossBird] {BossConfig.bossId} BURST — dropping {BossConfig.burstEggCount} eggs.");
            OnBurstLay?.Invoke(this, BossConfig.burstEggCount);
            GameEvents.FireBossBurstAttack(this, BossConfig.burstEggCount);
        }

        // ── Death ─────────────────────────────────────────────────────────

        protected override void Die(bool killedByPlayer)
        {
            if (_isDead) return;

            IsDefeated = killedByPlayer;
            base.Die(killedByPlayer);

            if (killedByPlayer)
            {
                Debug.Log($"[BossBird] {BossConfig.bossId} DEFEATED by player!");
                OnBossDefeated?.Invoke(this);
                GameEvents.FireBossDefeated(this);
            }
            else
            {
                Debug.Log($"[BossBird] {BossConfig.bossId} despawned (not killed).");
            }
        }
    }
}