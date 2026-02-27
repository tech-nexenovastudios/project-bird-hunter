using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/BulletLighting")]
public class BulletLighting_SO : PowerUp_SO
{
    public float[] duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.ElectricBullet(true, duration[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
