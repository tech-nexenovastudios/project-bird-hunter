using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/FlameTrail")]
public class FlameTrail : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.WheelFire(true, 1f, 3f, 3f);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
