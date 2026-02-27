using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Rocket")]
public class Rocket_SO : PowerUp_SO
{
    public float damage;
    public float[] interval;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.RocketEquip(true, damage, interval[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
