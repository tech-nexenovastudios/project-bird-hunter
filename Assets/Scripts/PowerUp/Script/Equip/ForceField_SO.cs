using UnityEngine;
[CreateAssetMenu(menuName = "PowerUps/ForceField")]
public class ForceField_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
   base.GrantAbility(power);
   power.ForceField();
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
