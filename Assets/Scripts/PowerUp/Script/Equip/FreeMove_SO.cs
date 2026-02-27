using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/FreeMove")]
public class FreeMove_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.FreeMove(true);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
