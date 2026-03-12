using System.Collections.Generic;
using UnityEngine;
using Gameplay.Interfaces;

namespace Gameplay.Player
{
    public class BouncingBullet : BaseBullet
    {
        //ToDo: Reimplement bouncing logic
        
        [Header("Bounce Settings")]
        public int bounceCount = 2;
        public float searchRadius = 5f;
        public LayerMask targetLayer;

        private List<GameObject> alreadyHitObjects = new List<GameObject>();
        private Transform currentTarget;
        private bool isHoming;
        private int initialBounceCount;
        private float initialDamage;

        protected override void Awake()
        {
            base.Awake();
            initialBounceCount = bounceCount;
            initialDamage = damage;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            bounceCount = initialBounceCount;
            damage = initialDamage;
            alreadyHitObjects.Clear();
            currentTarget = null;
            isHoming = false;
        }

        protected override void HandleMovement()
        {
            if (isHoming && currentTarget != null)
            {
                Vector2 dir = (Vector2)currentTarget.position - (Vector2)transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, bulletSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, currentTarget.position) < 0.1f)
                {
                    // If we reached the target, we should trigger hit if OnTriggerEnter2D didn't
                }
            }
        }

        protected override void HandlePostHit(Collider2D collision)
        {
            alreadyHitObjects.Add(collision.gameObject);
            
            if (bounceCount > 0)
            {
                bounceCount--;
                damage /= 2f; // Damage drops on bounce as per legacy
                
                FindNewTarget();
                
                if (currentTarget == null)
                {
                    Deactivate();
                }
            }
            else
            {
                Deactivate();
            }
        }

        private void FindNewTarget()
        {
            Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, searchRadius, targetLayer);
            float minDistance = float.MaxValue;
            Transform nearest = null;

            foreach (var t in targets)
            {
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
            }
            else
            {
                currentTarget = null;
                isHoming = false;
            }
        }
    }
}
