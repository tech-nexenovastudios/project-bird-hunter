using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Explosive bullets are fired straight up, with no rotation.
    /// Used by Big Bartha.
    /// </summary>
    public class ExplosiveBehaviour : IBulletBehaviour<ExplosiveBullet>
    {
        public void OnSpawn(ExplosiveBullet bullet)
        {
            bullet.SetVelocity();
        }

        public void Tick(ExplosiveBullet bullet, float dt)
        {
        }

        public void OnHit(ExplosiveBullet bullet, Collider2D collider)
        {
            bullet.Explode();
            bullet.Deactivate();
        }

        public void OnDespawn(ExplosiveBullet bullet)
        {
        }
    }
    
    public class ExplosiveBullet : BaseBullet<ExplosiveBullet, ExplosiveBehaviour>
    {
        public float explosionRadius = 3f;

        public void SetVelocity()
        {
            rb.linearVelocity = transform.up * currentSpeed;
        }

        public void Explode()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

            foreach (var h in hits)
            {
                if (h.TryGetComponent<IDamageable>(out var dmg))
                    dmg.TakeDamage((int)currentDamage);
            }
        }
    }

}