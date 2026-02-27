using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ShootingBullet")]
public class ShootingBullet_SO : PowerUp_SO
{
    public int numberOfBulletShoot;
    public float[] damage;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.NumberOfBulletInPerShot(numberOfBulletShoot, damage[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
