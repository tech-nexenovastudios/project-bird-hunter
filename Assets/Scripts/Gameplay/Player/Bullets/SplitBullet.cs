using Gameplay.Interfaces;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Used by Double Bullet and Triple Bullet cannons.
    /// Splits into child bullets on hit or at max range.
    /// </summary>
    public class SplitBehaviour : IBulletBehaviour<SplitBullet>
    {
        public void OnSpawn(SplitBullet bullet) => bullet.SetVelocity();
        public void Tick(SplitBullet bullet, float dt) { }
        
        public void OnHit(SplitBullet bullet, Collider2D collider)
        {
            bullet.ApplyDamage(collider);
            bullet.SpawnSplitChildren();
            bullet.Deactivate();
        }
        
        public void OnDespawn(SplitBullet bullet) { }
    }
    public class SplitBullet : BaseBullet<SplitBullet, SplitBehaviour>
    {
        [Header("Split Config")]
        public int splitCount = 2;        // 2 = Double, 3 = Triple
        public float splitAngle = 30f;    // Spread between children

        public void SetVelocity()
        {
            rb.linearVelocity = transform.up * currentSpeed;
        }

        public void SpawnSplitChildren()
        {
            if (splitCount <= 1) return;

            for (int i = 0; i < splitCount; i++)
            {
                float angle = -splitAngle + (splitAngle * 2f / (splitCount - 1)) * i;
                Vector2 splitDir = Quaternion.Euler(0, 0, angle) * (Vector2)transform.up;

                // BulletManager.Instance.SpawnBullet<StraightBullet>(
                //     config,
                //     transform.position,
                //     splitDir,
                //     null
                // );
            }
        }

        public override void ApplyDamage(Collider2D collider)
        {
            if (collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }
}