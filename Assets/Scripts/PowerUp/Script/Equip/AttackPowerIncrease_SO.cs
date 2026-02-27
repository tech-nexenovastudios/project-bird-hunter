using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/AttackPower")]
public class AttackPowerIncrease : PowerUp_SO
{
    public float[] value;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.AttackPowerIncrease(value[tempPowerLevel]);
    }

    public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
