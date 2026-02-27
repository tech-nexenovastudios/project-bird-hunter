using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Poison")]
public class Poison_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.PoisonEquip(true);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
