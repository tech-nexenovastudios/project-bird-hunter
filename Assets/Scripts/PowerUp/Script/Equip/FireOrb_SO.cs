using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/FireOrb")]
public class FireOrb_SO : PowerUp_SO
{
    public float[] damageValue;
    public float duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.FireOrb(duration, damageValue[tempPowerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
