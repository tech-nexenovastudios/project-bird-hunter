using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Passes through multiple enemies. Applied via power-up.
    /// Uses pierceRemaining from BaseBullet.
    /// </summary>
    public class PierceBehaviour : IBulletBehaviour<PierceBullet>
    {
        public void OnSpawn(PierceBullet bullet) => bullet.SetVelocity();
        public void Tick(PierceBullet bullet, float dt) { }
        
        public void OnHit(PierceBullet bullet, Collider2D collider)
        {
            bullet.ApplyDamage(collider);
            
            if (bullet.pierceRemaining <= 0)
            {
                bullet.Deactivate();
                return;
            }
            
            bullet.pierceRemaining--;
            // Continue moving - no deactivation
        }
        
        public void OnDespawn(PierceBullet bullet) { }
    }

    public class PierceBullet : BaseBullet<PierceBullet, PierceBehaviour>
    {
        public void SetVelocity() => rb.linearVelocity = transform.up * currentSpeed;
        
        public override void ApplyDamage(Collider2D collider)
        {
            if (collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }
}