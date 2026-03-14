using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// 
    /// Used by Lucky Cannon.
    /// </summary>
    public class HomingBehaviour : IBulletBehaviour<HomingBullet>
    {
        public void OnSpawn(HomingBullet bullet)
        {
            bullet.FindTarget();
        }

        public void Tick(HomingBullet bullet, float dt)
        {
            bullet.HomeTowardsTarget(dt);
        }

        public void OnHit(HomingBullet bullet, Collider2D collider)
        {
            bullet.ApplyDamage(collider);
            bullet.Deactivate();
        }

        public void OnDespawn(HomingBullet bullet)
        {
        }
    }
    public class HomingBullet : BaseBullet<HomingBullet, HomingBehaviour>
    {
        private Transform target;
        private float turnSpeed = 6f;

        public void FindTarget()
        {
            //target = EnemyManager.Instance.GetNearestEnemy(transform.position);
        }

        public void HomeTowardsTarget(float dt)
        {
            if (target == null) return;

            Vector2 dir = (target.position - transform.position).normalized;
            Vector2 newDir = Vector2.Lerp(rb.linearVelocity.normalized, dir, turnSpeed * dt);

            rb.linearVelocity = newDir * currentSpeed;
            transform.up = rb.linearVelocity;
        }

        public void ApplyDamage(Collider2D collider)
        {
            if (collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }
}