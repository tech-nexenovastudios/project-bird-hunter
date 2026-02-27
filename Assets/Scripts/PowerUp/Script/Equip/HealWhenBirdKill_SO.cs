using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/HealWhenBirdKill")]
public class HealWhenBirdKill_SO : PowerUp_SO
{
    public float[] chance;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.GetBackHealth(true, chance[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
