using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Player;

namespace Gameplay.PowerUps
{
    // ═════════════════════════════════════════════════════════
    //  MARKER INTERFACE — every effect type implements this
    //  so they can all live in one [SerializeReference] list.
    // ═════════════════════════════════════════════════════════

    public interface IPowerUpEffect { }

    // ═════════════════════════════════════════════════════════
    //  CATEGORY INTERFACES — extend IPowerUpEffect
    // ═════════════════════════════════════════════════════════

    public interface IEffect<TTarget> : IPowerUpEffect
    {
        void Apply(TTarget target);
        void Cancel();
        event Action<IEffect<TTarget>> OnCompleted;
    }

    public interface ICannonModifier : IPowerUpEffect
    {
        void Activate(ICannon cannon);
        void Deactivate();
    }

    public interface IProjectileModifier : IPowerUpEffect
    {
        bool IsActive { get; }
        void Activate();
        void Deactivate();
        void ModifyBullet(BaseBullet bullet, int cannonAttack);
        int ExtraProjectiles { get; }
        float[] GetExtraAngles();

        // DPS uplift this mod contributes when active. The TTK spawn-gate multiplies
        // this across all active mods to estimate true player DPS. Default 1f means
        // "no DPS effect" (e.g. multi-shot mods, since extra bullets are already
        // counted via ExtraProjectiles). Mods with real damage uplift (bounce, pierce,
        // rocket, DoT) override this with a value-derived formula.
        float DpsMultiplier => 1f;
    }

    public interface IReactiveEffect : IPowerUpEffect
    {
        void Activate(ICannon cannon);
        void Deactivate();
        void Tick();
    }

    public interface ISummonEffect : IPowerUpEffect
    {
        void Activate(ICannon cannon);
        void Deactivate();
        void Tick();
    }

    // ═════════════════════════════════════════════════════════
    //  CANNON POWER-UP — ONE array holds everything
    // ═════════════════════════════════════════════════════════

    [Serializable]
    public class CannonPowerUp
    {
        [Header("Config")]
        public PowerupConfig config;
       

        [Header("Feedback")]
        public AudioClip castSfx;
        public GameObject castVfx;
        public GameObject runningVfx;

        [Header("Effects")]
        [Tooltip("Add any effect type — enemy damage, cannon buff, projectile mod, reactive, or summon.")]
        [SerializeReference] public List<IPowerUpEffect> effects = new();

        // ── Execution — filters by type internally ───────────

        public void ExecuteOnEnemy(IEntity target)
        {
            if (target == null) return;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is IEffect<IEntity> e) e.Apply(target);
        }

        public void ActivateOnCannon(ICannon cannon)
        {
            if (cannon == null) return;
            for (int i = 0; i < effects.Count; i++)
            {
                switch (effects[i])
                {
                    case ICannonModifier cm: cm.Activate(cannon); break;
                    case IProjectileModifier pm: pm.Activate(); break;
                    case IReactiveEffect re: re.Activate(cannon); break;
                    case ISummonEffect se: se.Activate(cannon); break;
                }
            }
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                switch (effects[i])
                {
                    case ICannonModifier cm: cm.Deactivate(); break;
                    case IProjectileModifier pm: pm.Deactivate(); break;
                    case IReactiveEffect re: re.Deactivate(); break;
                    case ISummonEffect se: se.Deactivate(); break;
                }
            }
        }

        // ── Queries — caster uses these to decide Cast vs Equip ──
        public void TickProjectiles(ICannon cannon)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                switch (effects[i])
                {
                    case LaserEffect laser:
                        laser.Tick(cannon);
                        break;
                    case BulletModifier bm:
                        bm.Tick(cannon);
                        break;
                }
            }
        }
        public bool HasEnemyEffects()
        {
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is IEffect<IEntity>) return true;
            return false;
        }

        public bool HasCannonEffects()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (e is ICannonModifier || e is IProjectileModifier ||
                    e is IReactiveEffect || e is ISummonEffect) return true;
            }
            return false;
        }

     
        public void TickReactives()
        {
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is IReactiveEffect re) re.Tick();
        }

        public void TickSummons()
        {
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is ISummonEffect se) se.Tick();
        }

        public void ApplyProjectileModifiers(BaseBullet bullet, int cannonAttack)
        {
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is IProjectileModifier pm && pm.IsActive)
                    pm.ModifyBullet(bullet, cannonAttack);
        }

        public int GetTotalExtraProjectiles()
        {
            int total = 0;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] is IProjectileModifier pm && pm.IsActive)
                    total += pm.ExtraProjectiles;
            return total;
        }
    }

    // ═════════════════════════════════════════════════════════
    //  ENEMY EFFECTS
    // ═════════════════════════════════════════════════════════

    [Serializable]
    public class DamageEffect : IEffect<IEntity>
    {
        [Min(0)] public int damageAmount = 10;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            target?.TakeDamage(damageAmount);
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }

    [Serializable]
    public class DamageOverTimeEffect : IEffect<IEntity>
    {
        [Min(0.1f)] public float duration = 5f;
        [Min(0.05f)] public float tickInterval = 1f;
        [Min(0)] public int damagePerTick = 2;
        public event Action<IEffect<IEntity>> OnCompleted;

        private IntervalTimer timer;
        private IEntity currentTarget;

        public void Apply(IEntity target)
        {
            if (target == null) { OnCompleted?.Invoke(this); return; }
            Cancel();
            currentTarget = target;
            timer = new IntervalTimer(duration, tickInterval);
            timer.OnInterval = () => currentTarget?.TakeDamage(damagePerTick);
            timer.OnTimerStop = Cleanup;
            timer.Start();
        }

        public void Cancel()
        {
            if (timer != null) { timer.OnInterval = null; timer.OnTimerStop = null; timer.Stop(); }
            Cleanup();
        }

        private void Cleanup()
        {
            timer = null; currentTarget = null;
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class MultiHitDamageEffect : IEffect<IEntity>
    {
        [Min(1)] public int hitCount = 3;
        [Min(0)] public int damagePerHit = 5;
        [Min(0.05f)] public float hitInterval = 0.2f;
        public event Action<IEffect<IEntity>> OnCompleted;

        private IntervalTimer timer;
        private IEntity currentTarget;
        private int hitsDone;

        public void Apply(IEntity target)
        {
            if (target == null || hitCount <= 0) { OnCompleted?.Invoke(this); return; }
            Cancel();
            currentTarget = target;
            hitsDone = 0;
            float total = hitCount * Mathf.Max(0.05f, hitInterval);
            timer = new IntervalTimer(total, Mathf.Max(0.05f, hitInterval));
            timer.OnInterval = () =>
            {
                if (currentTarget == null) { Cancel(); return; }
                currentTarget.TakeDamage(damagePerHit);
                if (++hitsDone >= hitCount) Cancel();
            };
            timer.OnTimerStop = Cleanup;
            timer.Start();
        }

        public void Cancel()
        {
            if (timer != null) { timer.OnInterval = null; timer.OnTimerStop = null; timer.Stop(); }
            Cleanup();
        }

        private void Cleanup()
        {
            timer = null; currentTarget = null; hitsDone = 0;
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class RandomDamageRangeEffect : IEffect<IEntity>
    {
        [Min(0)] public int minDamage = 5;
        [Min(0)] public int maxDamage = 15;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            if (target != null)
            {
                int lo = Mathf.Min(minDamage, maxDamage);
                int hi = Mathf.Max(minDamage, maxDamage);
                target.TakeDamage(UnityEngine.Random.Range(lo, hi + 1));
            }
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }

    [Serializable]
    public class PercentMaxHpDamageEffect : IEffect<IEntity>
    {
        [Range(0.01f, 1f)] public float percentOfMaxHp = 0.1f;
        [Min(1)] public int minimumDamage = 1;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            if (target != null)
            {
                int dmg = target.MaxHp > 0
                    ? Mathf.Max(minimumDamage, Mathf.CeilToInt(target.MaxHp * percentOfMaxHp))
                    : minimumDamage;
                target.TakeDamage(dmg);
            }
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }

    [Serializable]
    public class ExecuteDamageEffect : IEffect<IEntity>
    {
        [Range(0f, 1f)] public float healthThreshold = 0.3f;
        [Min(0)] public int normalDamage = 12;
        [Min(0)] public int executeBonusDamage = 25;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            if (target != null)
            {
                int total = normalDamage;
                if (target.MaxHp > 0 && (float)target.CurrentHp / target.MaxHp <= healthThreshold)
                    total += executeBonusDamage;
                target.TakeDamage(total);
            }
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }

    [Serializable]
    public class ChanceDamageEffect : IEffect<IEntity>
    {
        [Range(0f, 1f)] public float procChance = 0.25f;
        [Min(0)] public int damageAmount = 20;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            if (target != null && UnityEngine.Random.value <= procChance)
                target.TakeDamage(damageAmount);
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }

    [Serializable]
    public class FreezeEffect : IEffect<IEntity>
    {
        [Min(0.1f)] public float duration = 3f;
        [Range(0f, 1f)] public float slowPercent = 1f;
        [Min(0)] public int damagePerTick;
        [Min(0.1f)] public float tickInterval = 1f;
        public event Action<IEffect<IEntity>> OnCompleted;

        private IntervalTimer timer;
        private IEntity currentTarget;

        public void Apply(IEntity target)
        {
            if (target == null) { OnCompleted?.Invoke(this); return; }
            Cancel();
            currentTarget = target;
            if (target is IFreezable f) f.ApplyFreeze(slowPercent);

            float interval = damagePerTick > 0 ? tickInterval : duration;
            timer = new IntervalTimer(duration, interval);
            timer.OnInterval = () => { if (damagePerTick > 0) currentTarget?.TakeDamage(damagePerTick); };
            timer.OnTimerStop = Cleanup;
            timer.Start();
        }

        public void Cancel()
        {
            if (currentTarget is IFreezable f) f.RemoveFreeze();
            if (timer != null) { timer.OnInterval = null; timer.OnTimerStop = null; timer.Stop(); }
            Cleanup();
        }

        private void Cleanup()
        {
            if (currentTarget is IFreezable fr) fr.RemoveFreeze();
            timer = null; currentTarget = null;
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class ChainLightningEffect : IEffect<IEntity>
    {
        [Min(0)] public int baseDamage = 25;
        [Min(1)] public int maxChains = 3;
        [Min(0.1f)] public float chainRadius = 5f;
        [Range(0.1f, 1f)] public float damageDecay = 0.7f;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            if (target == null) { OnCompleted?.Invoke(this); return; }
            target.TakeDamage(baseDamage);

            MonoBehaviour mb = target as MonoBehaviour;
            if (mb == null) { OnCompleted?.Invoke(this); return; }

            HashSet<IEntity> hit = new() { target };
            Vector2 pos = mb.transform.position;
            int dmg = baseDamage;

            for (int i = 0; i < maxChains; i++)
            {
                dmg = Mathf.Max(1, Mathf.FloorToInt(dmg * damageDecay));
                Collider2D[] cols = Physics2D.OverlapCircleAll(pos, chainRadius);
                IEntity best = null;
                float bestDist = float.MaxValue;

                for (int c = 0; c < cols.Length; c++)
                {
                    IEntity e = cols[c].GetComponent<IEntity>();
                    if (e == null || !e.IsAlive || hit.Contains(e)) continue;
                    float d = Vector2.Distance(pos, cols[c].transform.position);
                    if (d < bestDist) { bestDist = d; best = e; }
                }

                if (best == null) break;
                best.TakeDamage(dmg);
                hit.Add(best);
                if (best is MonoBehaviour m) pos = m.transform.position;
            }
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }

    [Serializable]
    public class AreaDamageEffect : IEffect<IEntity>
    {
        [Min(0)] public int damage = 35;
        [Min(0.1f)] public float radius = 4f;
        public event Action<IEffect<IEntity>> OnCompleted;

        public void Apply(IEntity target)
        {
            MonoBehaviour mb = target as MonoBehaviour;
            if (mb == null) { OnCompleted?.Invoke(this); return; }

            Collider2D[] cols = Physics2D.OverlapCircleAll(mb.transform.position, radius);
            for (int i = 0; i < cols.Length; i++)
            {
                IEntity e = cols[i].GetComponent<IEntity>();
                if (e != null && e.IsAlive) e.TakeDamage(damage);
            }
            OnCompleted?.Invoke(this);
        }
        public void Cancel() => OnCompleted?.Invoke(this);
    }
}