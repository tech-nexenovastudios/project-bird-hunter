using UnityEngine;

namespace Gameplay.Player
{
    public class DoubleCannon : BaseCannon
    {
        [Header("Spread Settings")]
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 5f;
        // ↑ Exposed in Inspector with a slider
        //   Change it anytime without touching code
        //   Works for 5°, 10°, 15° — whatever feels best during playtesting

        protected override void Shoot()
        {
            if (gunTips == null || gunTips.Length < 2)
            {
                base.Shoot();
                return;
            }

            // Left tip → rotate negatively (spread left)
            Quaternion leftRotation = gunTips[0].rotation * Quaternion.Euler(0f, 0f, spreadAngle);
            SpawnBullet(gunTips[0].position, leftRotation);

            // Right tip → rotate positively (spread right)
            Quaternion rightRotation = gunTips[1].rotation * Quaternion.Euler(0f, 0f, -spreadAngle);
            SpawnBullet(gunTips[1].position, rightRotation);

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