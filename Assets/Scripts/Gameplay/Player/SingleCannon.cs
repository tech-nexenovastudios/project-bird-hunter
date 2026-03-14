using UnityEngine;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon<StraightBullet>
    {
        protected override void Fire(Vector2 position, Vector2 direction)
        {
            base.SpawnBullet(position, direction);
        }
    }
}