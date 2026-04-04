using System.Collections.Generic;
using UnityEngine;
using Gameplay.Interfaces;

namespace Gameplay.PowerUps
{
    public class GroundSpikes : MonoBehaviour
    {
        // Shared across ALL spikes — cleared every second by GroundSpikeEffect
        public static readonly HashSet<IDamageable> damagedThisTick = new();

        private ICannon cannon;
        private float damagePercent;
        private float tickTimer;
        private const float TickInterval = 1f;

        public void Init(ICannon cannon, float damagePercent)
        {
            this.cannon = cannon;
            this.damagePercent = damagePercent;
        }

        private void Update()
        {
            tickTimer -= Time.deltaTime;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (cannon == null || tickTimer > 0f) return;
            if (!other.CompareTag("Egg")) return;

            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            // Already damaged by another spike this tick — skip
            if (!damagedThisTick.Add(target)) return;

            int dmg = Mathf.Max(1, Mathf.RoundToInt(cannon.CurrentAttack * damagePercent / 100f));
            target.TakeDamage(dmg);
        }

        // Called by GroundSpikeEffect.Tick() once per second
        public static void ResetDamageTracker()
        {
            damagedThisTick.Clear();
        }
    }
}