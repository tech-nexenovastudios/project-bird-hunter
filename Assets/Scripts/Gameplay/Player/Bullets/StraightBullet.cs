using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Player                          // ✅ same namespace
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
        public void OnSpawn  (StraightBullet bullet)               { }  // ✅ velocity set in Initialize
        public void Tick     (StraightBullet bullet, float dt)     { }  // no runtime logic
        public void OnHit    (StraightBullet bullet, Collider2D col) { bullet.ApplyDamage(col); bullet.Deactivate(); }
        public void OnDespawn(StraightBullet bullet)               { }
    }
}