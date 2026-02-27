using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/LaserEquip")]
public class LaserPowerUp_SO : PowerUp_SO
{
    public float[] duration;
    public float damage, interval;
public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.LaserEquip(true, damage, interval, duration[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
