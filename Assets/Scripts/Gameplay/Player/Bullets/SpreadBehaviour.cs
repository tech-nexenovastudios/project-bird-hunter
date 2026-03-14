using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Spread bullets are fired upwards, with randomized rotation.
    /// Used by Shotgun cannon.
    /// Spread is typically handled at spawn time by the cannon, not inside the bullet.
    /// </summary>
    public class SpreadBehaviour : IBulletBehaviour<SpreadBullet>
    {
        public void OnSpawn(SpreadBullet bullet)
        {
            bullet.SetVelocity();
        }

        public void Tick(SpreadBullet bullet, float dt)
        {
        }

        public void OnHit(SpreadBullet bullet, Collider2D collider)
        {
            bullet.ApplyDamage(collider);
            bullet.Deactivate();
        }

        public void OnDespawn(SpreadBullet bullet)
        {
        }
    }
    public class SpreadBullet : BaseBullet<SpreadBullet, SpreadBehaviour>
    {
        public void SetVelocity()
        {
            rb.linearVelocity = transform.up * currentSpeed;
        }

        public void ApplyDamage(Collider2D collider)
        {
            if (collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }
}