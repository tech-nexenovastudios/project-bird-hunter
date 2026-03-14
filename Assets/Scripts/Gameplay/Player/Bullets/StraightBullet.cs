using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    public class StraightBullet : BaseBullet<StraightBullet, StraightBehaviour>
    {
        public void SetVelocity()
        {
            rb.linearVelocity = transform.up * currentSpeed;
        }

        public override void ApplyDamage(Collider2D collider)
        {
            if (collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }
    /// <summary>
    /// Straight bullets are fired straight up, with no rotation.
    /// Used by Single Shot and Rapid Fire cannons.
    /// </summary>
    public class StraightBehaviour : IBulletBehaviour<StraightBullet>
    {
        public void OnSpawn(StraightBullet bullet)
        {
            bullet.SetVelocity();
        }

        public void Tick(StraightBullet bullet, float dt)
        {
            // Straight bullets require no runtime logic
        }

        public void OnHit(StraightBullet bullet, Collider2D collider)
        {
            bullet.ApplyDamage(collider);
            bullet.Deactivate();
        }

        public void OnDespawn(StraightBullet bullet)
        {
        }
    }
}