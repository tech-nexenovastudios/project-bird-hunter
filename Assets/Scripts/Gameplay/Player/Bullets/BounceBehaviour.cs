using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Bounces off screen edges. Applied via power-up.
    /// </summary>
    public class BounceBehaviour : IBulletBehaviour<BounceBullet>
    {
        public void OnSpawn(BounceBullet bullet) => bullet.SetVelocity();
        
        public void Tick(BounceBullet bullet, float dt)
        {
            bullet.CheckBounds();
        }
        
        public void OnHit(BounceBullet bullet, Collider2D collider)
        {
            bullet.ApplyDamage(collider);
            
            if (bullet.bounceRemaining <= 0)
                bullet.Deactivate();
            else
                bullet.Bounce(collider);
        }
        
        public void OnDespawn(BounceBullet bullet) { }
    }

    public class BounceBullet : BaseBullet<BounceBullet, BounceBehaviour>
    {
        public int bounceRemaining = 3;
        private static Vector2 _screenMin, _screenMax;
        
        protected override void Awake()
        {
            base.Awake();
            _screenMin = Camera.main.ViewportToWorldPoint(Vector2.zero);
            _screenMax = Camera.main.ViewportToWorldPoint(Vector2.one);
        }
        
        public void SetVelocity() => rb.linearVelocity = transform.up * currentSpeed;
        
        public void CheckBounds()
        {
            Vector2 pos = transform.position;
            Vector2 vel = rb.linearVelocity;
            
            if (pos.x <= _screenMin.x || pos.x >= _screenMax.x)
                rb.linearVelocity = new Vector2(-vel.x, vel.y);
        }
        
        public void Bounce(Collider2D collider)
        {
            bounceRemaining--;
            Vector2 normal = (transform.position - collider.transform.position).normalized;
            rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal);
        }
        
        public override void ApplyDamage(Collider2D collider)
        {
            if (collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }
}
