using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/FullyHealBeforeBattle")]
public class FullyHealBeforeBossBattle : PowerUp_SO
{
    public float[] healPercentage;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.fullHealBeforeBossBattle(true, healPercentage[tempPowerLevel]);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
