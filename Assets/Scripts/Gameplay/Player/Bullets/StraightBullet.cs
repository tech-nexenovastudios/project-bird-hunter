using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player
{
    public class StraightBullet : BaseBullet<StraightBullet>
    {
        protected override IBulletBehaviour<StraightBullet> CreateBehaviour()
            => new StraightBehaviour();

        public override void ApplyDamage(Collider2D col)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage((int)currentDamage);
        }
    }

    public class StraightBehaviour : IBulletBehaviour<StraightBullet>
    {
        // Velocity is already set by BaseBullet.Initialize — nothing extra needed
        public void OnSpawn(StraightBullet bullet)  { }
        public void Tick(StraightBullet bullet, float dt) { }
        public void OnHit(StraightBullet bullet, Collider2D col)
        {
            bullet.ApplyDamage(col);
            bullet.Deactivate();
        }
        public void OnDespawn(StraightBullet bullet) { }
    }
}