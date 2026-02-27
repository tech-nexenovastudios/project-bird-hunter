using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/MiniCannon")]
public class MiniCannon_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.miniCannon();
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
