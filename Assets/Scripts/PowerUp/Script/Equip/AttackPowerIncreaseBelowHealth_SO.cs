using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/AttackPowerIncreaseBelowHealth")]
public class AttackPowerIncreaseBelowHealth_SO : PowerUp_SO
{
    public float[] value;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.DamageIncreaseBelowHealth(true, value[tempPowerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
