using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Player
{
    public class TripleCannon : BaseCannon
    {
        protected override void Shoot()
        {
            // Shoot from three tips
            if (gunTips == null || gunTips.Length < 3)
            {
                // Fallback if not properly configured in inspector
                base.Shoot();
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                SpawnBullet(gunTips[i].position, gunTips[i].rotation);
            }

            if (fireParticles != null)
            {
                foreach (var p in fireParticles)
                {
                    if (p != null) p.Play();
                }
            }
        }
    }
}
