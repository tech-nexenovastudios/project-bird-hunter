using UnityEngine;

namespace Gameplay.Player
{
    public class SimpleBullet : BaseBullet
    {
        protected override void HandleMovement()
        {
            
        }

        protected override void ApplyElementalEffects(GameObject target)
        {
            // Simple bullet has no special elemental effects
            base.ApplyElementalEffects(target);
        }
    }
}
