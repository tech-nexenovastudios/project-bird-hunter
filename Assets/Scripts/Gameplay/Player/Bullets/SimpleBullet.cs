using UnityEngine;

namespace Gameplay.Player
{
    public class SimpleBullet : BaseBullet
    {
        [SerializeField] private TrailRenderer trailRenderer;
  

        protected override void OnEnable()
        {
            base.OnEnable();

            // Immediately disable trail — we don't want it recording
            // during the teleport that's about to happen in SpawnBullet()
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.enabled = false;
            }
        }

        protected override void OnInitComplete()
        {
            // This runs AFTER Init() has positioned the bullet
            // and set velocity — now it's safe to start recording
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.enabled = true;
            }
        }

        protected override void HandleMovement()
        {
            // Pure movement — never touch the trail here
            transform.position += transform.up * bulletSpeed * Time.deltaTime;
        }

        public override void Deactivate()
        {
            // Clean up trail BEFORE returning to pool
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.enabled = false;
            }

            base.Deactivate();
        }

        protected override void ApplyElementalEffects(GameObject target)
        {
            // Simple bullet — no elemental effects
        }
    }
}