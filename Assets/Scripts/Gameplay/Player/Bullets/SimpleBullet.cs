using UnityEngine;

namespace Gameplay.Player
{
    public class SimpleBullet : BaseBullet
    {
        [SerializeField] private ParticleSystem trailEffect;
        protected override void HandleMovement()
        {
            transform.position += transform.up * bulletSpeed * Time.deltaTime;
        }

        protected override void ApplyElementalEffects(GameObject target)
        {
            // Simple bullet has no special elemental effects
            base.ApplyElementalEffects(target);
        }
    }
}
