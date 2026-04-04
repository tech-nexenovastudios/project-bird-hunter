using UnityEngine;

namespace Gameplay.Player
{
    public class ElementalBullet : BaseBullet
    {
        //ToDo: Reimplement elemental logic
        
        public enum ElementType { None, Fire, Electric, Poison, Ice }
        
        [Header("Elemental Settings")]
        public ElementType element = ElementType.None;
        public float effectDuration = 3f;

        protected override void HandleMovement()
        {
            //
        }

        //protected override void ApplyElementalEffects(GameObject target)
        //{
        //    if (target.TryGetComponent<IDamageEffect>(out var damageEffect))
        //    {
        //        switch (element)
        //        {
        //            case ElementType.Fire:
        //                damageEffect.igniteDamage(damage, effectDuration);
        //                break;
        //            case ElementType.Electric:
        //                damageEffect.ElectricDamage(damage, effectDuration);
        //                break;
        //            case ElementType.Poison:
        //                damageEffect.PoisonDamage(damage, effectDuration);
        //                break;
        //            case ElementType.Ice:
        //                damageEffect.FreezeEffect(effectDuration);
        //                break;
        //        }
        //    }
        //}
    }
}
