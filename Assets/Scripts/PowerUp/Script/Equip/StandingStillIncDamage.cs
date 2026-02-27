using UnityEngine;

public class StandingStillIncDamage : PowerUp_SO
{
    public float[] damageIncrease;
    public float duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.standingStillIncreaseDamage(damageIncrease[tempPowerLevel], duration);
    }
}
