using Gameplay.Eggs;
using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    public class StraightBullet : BaseBullet<StraightBullet>
    {
        public float maxHitImpulseForce = 8f;
        public float minHitImpulseForce = 2f;
        public float maxForceDistance   = 10f;
        public bool  applyHitImpulse    = true;
        public Vector2     startPosition;
        
        protected override IBulletBehaviour<StraightBullet> CreateBehaviour()
            => new StraightBehaviour();

        public override void ApplyDamage(Collider2D col)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg))
            {
                if (dmg.IsAlive)
                {
                    dmg.TakeDamage((int)currentDamage);
                }
            }
        }
        public virtual void ApplyHitImpulse(Collider2D collision)
        {
            if (!applyHitImpulse) return;

            Egg egg = collision.gameObject.GetComponent<Egg>();
            if (egg == null) return;

            Vector2 bulletDir = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : (Vector2)transform.up.normalized;
            
            float distance = Vector2.Distance(startPosition, transform.position);
            float t = maxForceDistance > 0f ? Mathf.Clamp01(distance / maxForceDistance) : 1f;
            float impulseForce = Mathf.Lerp(maxHitImpulseForce, minHitImpulseForce, t);

            egg.ApplyBulletHitForce(bulletDir, impulseForce);
        }
        
    }

    public class StraightBehaviour : IBulletBehaviour<StraightBullet>
    {
        // Velocity is already set by BaseBullet.Initialize — nothing extra needed
        public void OnSpawn(StraightBullet bullet)
        {
            bullet.startPosition = bullet.transform.position;
        }
        public void Tick(StraightBullet bullet, float dt) { }
        public void OnHit(StraightBullet bullet, Collider2D col)
        {
            Debug.Log("Straight hit");
            
            if(col.GetComponent<Egg>() != null) 
                bullet.ApplyHitImpulse(col);
            
            bullet.ApplyDamage(col);
            bullet.Deactivate();
        }

        public void OnDespawn(StraightBullet bullet)
        {
            
        }
    }
}