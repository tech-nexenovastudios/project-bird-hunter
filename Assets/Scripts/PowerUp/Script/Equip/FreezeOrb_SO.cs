using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/FreezeOrb")]
public class FreezeOrb_SO : PowerUp_SO
{
    public float[] damageValue;
    public float duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.FreezeOrb(duration, damageValue[tempPowerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
