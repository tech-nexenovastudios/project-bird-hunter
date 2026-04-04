using System.Collections.Generic;
using UnityEngine;
using Gameplay.Interfaces;

namespace Gameplay.Player
{
    public class BouncingBullet : BaseBullet
    {
        [Header("Bounce Settings")]
        public int bounceCount = 0;
        public float searchRadius = 15f; // Increased for better testing
        public LayerMask targetLayer;

        private readonly List<GameObject> alreadyHitObjects = new List<GameObject>();
        private Transform currentTarget;
        private bool isHoming;

        protected override void OnEnable()
        {
            base.OnEnable();
            alreadyHitObjects.Clear();
            currentTarget = null;
            isHoming = false;
            // NOTE: Do NOT reset bounceCount here if it's being set by a Modifier
        }

        protected override void HandleMovement()
        {
            if (isHoming && currentTarget != null)
            {
                // 1. Kill the physics velocity so it doesn't fight the homing
                if (rb != null) rb.linearVelocity = Vector2.zero;

                // 2. Move towards target
                Vector2 dir = (Vector2)currentTarget.position - (Vector2)transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, bulletSpeed * Time.deltaTime);
            }
            else if (rb != null && rb.linearVelocity.sqrMagnitude < 0.1f)
            {
                // Fallback: If not homing and stopped, keep moving forward
                rb.linearVelocity = transform.up * bulletSpeed;
            }
        }

        protected override void HandlePostHit(Collider2D collision)
        {
            alreadyHitObjects.Add(collision.gameObject);

            if (bounceCount > 0)
            {
                bounceCount--;
                damage *= 0.8f; // Less aggressive damage drop than /2

                FindNewTarget();

                if (currentTarget == null)
                {
                    Deactivate();
                }
                // We do NOT call base.Deactivate() here because we want to keep living
            }
            else
            {
                Deactivate();
            }
        }

        private void FindNewTarget()
        {
            // FIX: Added the 'Transform nearest' declaration here
            Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, searchRadius, targetLayer);
            float minDistance = float.MaxValue;
            Transform nearest = null;

            foreach (var t in targets)
            {
                // Don't bounce back to something we already hit
                if (alreadyHitObjects.Contains(t.gameObject)) continue;

                float dist = Vector2.Distance(transform.position, t.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = t.transform;
                }
            }

            if (nearest != null)
            {
                currentTarget = nearest;
                isHoming = true;

                // Debug visual: Draws a red line to the new target in the Scene view
                Debug.DrawLine(transform.position, nearest.position, Color.red, 2f);
            }
            else
            {
                currentTarget = null;
                isHoming = false;
            }
        }
    }
}