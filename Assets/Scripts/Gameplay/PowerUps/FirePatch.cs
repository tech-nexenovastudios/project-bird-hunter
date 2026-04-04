using UnityEngine;
using Gameplay.Interfaces;


    // ═════════════════════════════════════════════════════════
    //  FIRE PATCH
    //
    //  Attach this to your fire patch prefab.
    //  When an Egg stays inside the trigger, it takes
    //  damage every tickInterval seconds.
    //
    //  Setup on the prefab:
    //    1. Add a CircleCollider2D → set "Is Trigger" ON
    //    2. Set the radius to match your visual size
    //    3. Add this script
    //    4. Add your fire VFX (particle system / sprite)
    //    5. Make sure the prefab has a Rigidbody2D set to
    //       Kinematic (required for trigger detection)
    // ═════════════════════════════════════════════════════════

    public class FirePatch : MonoBehaviour
    {
        [Tooltip("Damage dealt per tick to each enemy inside the patch.")]
        [Min(1)] public int damagePerTick = 5;

        [Tooltip("Seconds between each damage tick.")]
        [Min(0.1f)] public float tickInterval = 0.5f;

        private float tickTimer;

        private void OnTriggerStay2D(Collider2D other)
        {
            // Only damage Eggs
            if (!other.CompareTag("Egg")) return;

            // Rate-limit damage using a shared timer.
            // This means ALL enemies in the patch share the same tick rhythm,
            // which is fine for a ground hazard and avoids per-enemy tracking.
            if (tickTimer > 0f) return;
        Debug.Log("Egg is in fire");
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null && target.IsAlive)
                target.TakeDamage(damagePerTick);
        }

        private void Update()
        {
            if (tickTimer > 0f)
                tickTimer -= Time.deltaTime;
            else
                tickTimer = tickInterval;  // Reset for next tick
        }
    }

