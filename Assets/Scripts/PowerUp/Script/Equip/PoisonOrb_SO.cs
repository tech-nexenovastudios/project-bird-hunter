using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/PoisonOrb")]
public class PoisonOrb_SO : PowerUp_SO
{
    public float[] damageValue;
    public float duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.PoisonOrb(duration, damageValue[tempPowerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
