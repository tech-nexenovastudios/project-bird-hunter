using UnityEngine;
using Gameplay.Interfaces;

namespace Gameplay.PowerUps
{
    public class SpinningSword : MonoBehaviour
    {
        private enum BladePhase { Attached, Flying, Returning }

        private BladePhase phase = BladePhase.Attached;

        private Transform cannonTransform;
        private Vector3 localAttachOffset;      // position relative to cannon when attached
        private Quaternion attachedRotation;    // z = -11 original rotation

        private Vector2 flyTarget;
        private float speed;
        private float rotSpeed;

        private int damage;

        private const float ArrivalThreshold = 0.4f;

        // ───────── Called by BladeStrikeEffect once on spawn ─────────
        public void Init(Transform cannon, Vector3 localOffset, float speed, float rotationSpeed, int damage)
        {
            this.cannonTransform = cannon;
            this.localAttachOffset = localOffset;
            this.speed = speed;
            this.rotSpeed = rotationSpeed;
            this.damage = damage;

            // Store original rotation (z = -11)
            attachedRotation = transform.rotation;

            // Start attached
            phase = BladePhase.Attached;
        }

        // ───────── Called by BladeStrikeEffect when egg found ─────────
        public void Launch(Vector2 target)
        {
            if (phase != BladePhase.Attached) return;
            flyTarget = target;
            transform.SetParent(null); // detach from cannon
            phase = BladePhase.Flying;
        }

        private void Update()
        {
            switch (phase)
            {
                case BladePhase.Attached:
                    // Follow cannon exactly at offset
                    if (cannonTransform == null) { Destroy(gameObject); return; }
                    transform.position = cannonTransform.TransformPoint(localAttachOffset);
                    transform.rotation = attachedRotation;
                    break;

                case BladePhase.Flying:
                    // Fly toward egg position, spin forward
                    MoveToward(flyTarget);
                    transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

                    if (Vector2.Distance(transform.position, flyTarget) < ArrivalThreshold)
                    {
                        // Deal damage at target
                        DamageAtTarget();
                        phase = BladePhase.Returning;
                    }
                    break;

                case BladePhase.Returning:
                    // Return to cannon's current position
                    if (cannonTransform == null) { Destroy(gameObject); return; }

                    Vector2 returnPos = cannonTransform.TransformPoint(localAttachOffset);
                    MoveToward(returnPos);

                    // Spin in reverse while returning
                    transform.Rotate(0f, 0f, -rotSpeed * Time.deltaTime);

                    if (Vector2.Distance(transform.position, returnPos) < ArrivalThreshold)
                    {
                        // Snap back, restore rotation, re-attach
                        transform.position = cannonTransform.TransformPoint(localAttachOffset);
                        transform.rotation = attachedRotation;
                        transform.SetParent(cannonTransform);
                        phase = BladePhase.Attached;
                    }
                    break;
            }
        }

        private void MoveToward(Vector2 target)
        {
            Vector2 dir = (target - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
        }

        private void DamageAtTarget()
        {
            // Damage all IDamageable in a small radius at target point
            Collider2D[] hits = Physics2D.OverlapCircleAll(flyTarget, 0.8f);
            foreach (var col in hits)
            {
                var entity = col.GetComponent<IEntity>();
                if (entity != null && entity.IsAlive)
                    entity.TakeDamage(damage);
            }
        }
    }
}