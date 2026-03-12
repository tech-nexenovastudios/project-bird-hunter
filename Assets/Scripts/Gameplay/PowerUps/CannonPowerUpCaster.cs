using System.Collections;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using Gameplay.Birds;
using UnityEngine;
using UnityUtils;

namespace Gameplay.PowerUps
{
    public class CannonPowerUpCaster : MonoBehaviour
    {
        public CannonPowerUp[] hotbar;

        private void Update()
        {
            for (int i = 0; i < hotbar.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Cast(hotbar[i], FindFirstObjectByType<BaseBird>());
                }
            }
        }

        private void Cast(CannonPowerUp powerUp, IDamageable target)
        {
            powerUp.Execute(target);

            var targetMb = target as MonoBehaviour;

            if (powerUp.castVfx && targetMb)
            {
                Instantiate(powerUp.castVfx, targetMb.transform.position.Add(y: 2), Quaternion.identity);
            }

            if (powerUp.runningVfx && targetMb)
            {
                var runningVfxInstance = Instantiate(powerUp.runningVfx, targetMb.transform);
                Destroy(runningVfxInstance, 3f);
            }

            if (powerUp.castSfx)
            {
                AudioSource.PlayClipAtPoint(powerUp.castSfx, transform.position);
            }
        }
    }
}