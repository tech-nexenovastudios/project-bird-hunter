using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Player
{
    public class DoubleCannon : BaseCannon
    {
        protected override void Shoot()
        {
            // Shoot from two tips
            if (gunTips == null || gunTips.Length < 2)
            {
                // Fallback if not properly configured in inspector
                base.Shoot();
                return;
            }

            for (int i = 0; i < 2; i++)
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
