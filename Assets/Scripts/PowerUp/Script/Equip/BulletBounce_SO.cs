using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/BulletBounce")]
public class BulletBounce_SO : PowerUp_SO
{
    public int[] bounce;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.BulletBounce(bounce[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
