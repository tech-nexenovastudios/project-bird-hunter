using System.Collections.Generic;
using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.PowerUps;

namespace Gameplay.Birds
{
    public class Enemy0 : MonoBehaviour, IDamageable
    {
        public int health = 50;
        readonly List<IEffect<IDamageable>> activeEffects = new();

        public int CurrentHp { get; }
        public int MaxHp { get; }
        public bool IsAlive { get; }

        public void TakeDamage(int amount)
        {
            health -= amount;
            Debug.Log($"Enemy took {amount} damage. Health now {health}");

            if (health <= 0)
            {
                Die();
            }
        }

        public void ApplyEffect(IEffect<IDamageable> effect)
        {
            if (health <= 0) return; // Dead enemies should't receive effects

            effect.OnCompleted += RemoveEffect;
            activeEffects.Add(effect);
            effect.Apply(this);
        }

        void RemoveEffect(IEffect<IDamageable> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            activeEffects.Remove(effect);
        }

        void Die()
        {
            Debug.Log("Enemy died.");

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel();
            }

            activeEffects.Clear();

            Destroy(gameObject);
        }
    }
}