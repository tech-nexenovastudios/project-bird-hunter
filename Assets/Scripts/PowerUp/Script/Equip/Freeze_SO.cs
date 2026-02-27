using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Freeze")]
public class Freeze_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.FreezeEquip(true);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
