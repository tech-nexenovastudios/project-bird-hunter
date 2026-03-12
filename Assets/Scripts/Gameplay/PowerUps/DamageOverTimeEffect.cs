using System;
using System.Collections.Generic;
using System.Linq;
using ImprovedTimers;
using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Birds;
using Gameplay.Eggs;

namespace Gameplay.PowerUps
{
    [Serializable]
    public class CannonPowerUp
    {
        public AudioClip castSfx;
        public GameObject castVfx;
        public GameObject runningVfx;

        [SerializeReference] public List<IEffect<IDamageable>> effects = new();

        public void Execute(IDamageable target)
        {
            if (target == null) return;

            foreach (var effect in effects)
            {
                if (effect == null) continue;

                if (target is BaseBird enemy)
                {
                    enemy.ApplyEffect(effect);
                }

                if (target is Egg egg)
                {
                    
                }
                else
                {
                    effect.Apply(target);
                }
            }
        }
    }

    public interface IEffect<TTarget>
    {
        void Apply(TTarget target);
        void Cancel();
        event Action<IEffect<TTarget>> OnCompleted;
    }

    [Serializable]
    public class DamageEffect : IEffect<IDamageable>
    {
        public int damageAmount = 10;

        public event Action<IEffect<IDamageable>> OnCompleted;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            target.TakeDamage(damageAmount);
            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class DamageOverTimeEffect : IEffect<IDamageable>
    {
        public float duration = 5f;
        public float tickInterval = 1f;
        public int damagePerTick = 2;

        public event Action<IEffect<IDamageable>> OnCompleted;

        private IntervalTimer timer;
        private IDamageable currentTarget;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            Cancel();

            currentTarget = target;
            timer = new IntervalTimer(duration, tickInterval);
            timer.OnInterval = OnInterval;
            timer.OnTimerStop = OnStop;
            timer.Start();
        }

        private void OnInterval()
        {
            currentTarget?.TakeDamage(damagePerTick);
        }

        private void OnStop()
        {
            Cleanup();
        }

        public void Cancel()
        {
            if (timer != null)
            {
                timer.OnInterval = null;
                timer.OnTimerStop = null;
                timer.Stop();
            }

            Cleanup();
        }

        private void Cleanup()
        {
            timer = null;
            currentTarget = null;
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class MultiHitDamageEffect : IEffect<IDamageable>
    {
        public int hitCount = 3;
        public int damagePerHit = 5;
        public float hitInterval = 0.2f;

        public event Action<IEffect<IDamageable>> OnCompleted;

        private IntervalTimer timer;
        private IDamageable currentTarget;
        private int hitsDone;

        public void Apply(IDamageable target)
        {
            if (target == null || hitCount <= 0)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            Cancel();

            currentTarget = target;
            hitsDone = 0;

            float totalDuration = Mathf.Max(0.05f, hitCount * hitInterval);

            timer = new IntervalTimer(totalDuration, Mathf.Max(0.05f, hitInterval));
            timer.OnInterval = OnInterval;
            timer.OnTimerStop = OnStop;
            timer.Start();
        }

        private void OnInterval()
        {
            if (currentTarget == null)
            {
                Cancel();
                return;
            }

            currentTarget.TakeDamage(damagePerHit);
            hitsDone++;

            if (hitsDone >= hitCount)
            {
                Cancel();
            }
        }

        private void OnStop()
        {
            Cleanup();
        }

        public void Cancel()
        {
            if (timer != null)
            {
                timer.OnInterval = null;
                timer.OnTimerStop = null;
                timer.Stop();
            }

            Cleanup();
        }

        private void Cleanup()
        {
            timer = null;
            currentTarget = null;
            hitsDone = 0;
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class RandomDamageRangeEffect : IEffect<IDamageable>
    {
        public int minDamage = 5;
        public int maxDamage = 15;

        public event Action<IEffect<IDamageable>> OnCompleted;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            int low = Mathf.Min(minDamage, maxDamage);
            int high = Mathf.Max(minDamage, maxDamage);
            target.TakeDamage(UnityEngine.Random.Range(low, high + 1));
            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class PercentMaxHpDamageEffect : IEffect<IDamageable>
    {
        [Range(0.01f, 1f)] public float percentOfMaxHp = 0.1f;
        public int minimumDamage = 1;

        public event Action<IEffect<IDamageable>> OnCompleted;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            int damage = minimumDamage;

            if (target.MaxHp > 0)
            {
                damage = Mathf.Max(minimumDamage, Mathf.CeilToInt(target.MaxHp * percentOfMaxHp));
            }

            target.TakeDamage(damage);
            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class ExecuteDamageEffect : IEffect<IDamageable>
    {
        [Range(0f, 1f)] public float healthThreshold = 0.3f;
        public int normalDamage = 12;
        public int executeBonusDamage = 25;

        public event Action<IEffect<IDamageable>> OnCompleted;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            int totalDamage = normalDamage;

            if (target.MaxHp > 0)
            {
                float ratio = (float)target.CurrentHp / target.MaxHp;
                if (ratio <= healthThreshold)
                {
                    totalDamage += executeBonusDamage;
                }
            }

            target.TakeDamage(totalDamage);
            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class ChanceDamageEffect : IEffect<IDamageable>
    {
        [Range(0f, 1f)] public float procChance = 0.25f;
        public int damageAmount = 20;

        public event Action<IEffect<IDamageable>> OnCompleted;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            if (UnityEngine.Random.value <= procChance)
            {
                target.TakeDamage(damageAmount);
            }

            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class CompositeDamageEffect : IEffect<IDamageable>
    {
        [SerializeReference] public List<IEffect<IDamageable>> nestedEffects = new();

        public event Action<IEffect<IDamageable>> OnCompleted;

        public void Apply(IDamageable target)
        {
            if (target == null)
            {
                OnCompleted?.Invoke(this);
                return;
            }

            foreach (var effect in nestedEffects.Where(effect => effect != null))
            {
                effect.Apply(target);
            }

            OnCompleted?.Invoke(this);
        }

        public void Cancel()
        {
            foreach (var effect in nestedEffects.Where(effect => effect != null))
            {
                effect.Cancel();
            }

            OnCompleted?.Invoke(this);
        }
    }
}