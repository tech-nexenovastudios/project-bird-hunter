using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/SideBullet")]
public class SideBullet : PowerUp_SO
{
    public float[] damage;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.SideFire(true, damage[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
