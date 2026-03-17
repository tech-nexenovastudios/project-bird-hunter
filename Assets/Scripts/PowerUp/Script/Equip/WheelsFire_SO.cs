using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/WheelFire")]
public class WheelsFire_SO : PowerUp_SO
{
    public float[] duration;
    public override void GrantAbility(CannonPower power)
    {
        base.GrantAbility(power);
        power.WheelFire(true, 1f, 3f, duration[tempPowerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
