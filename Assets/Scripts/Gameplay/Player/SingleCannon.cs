using UnityEngine;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon<StraightBullet, StraightBehaviour>
    {
        protected override void Fire(Vector2 position, Vector2 direction)
        {
            SpawnBullet(position, direction);
            PlayFireParticles();
        }

        private void PlayFireParticles()
        {
            if (fireParticles == null) return;
            foreach (var p in fireParticles)
                if (p != null) p.Play();
        }
    }
}