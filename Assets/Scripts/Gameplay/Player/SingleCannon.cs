using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon
    {
        protected override void UpdateUI()
        {
            base.UpdateUI();
            if (fireParticles is { Length: > 0 })
            {
                fireParticles[0].Play();
            }
        }
        protected override void Shoot()
        {
            // Standard behavior: One bullet from the main gun tip or center
            if (gunTips == null || gunTips.Length == 0)
            {
                SpawnBullet(transform.position, transform.rotation);
            }
            else
            {
                // Single cannon usually only has one tip, but we follow the array pattern
                SpawnBullet(gunTips[0].position, gunTips[0].rotation);
            }

            if (fireParticles != null && fireParticles.Length > 0)
            {
                fireParticles[0].Play();
            }
        }
    }
}