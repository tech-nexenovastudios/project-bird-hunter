using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ManaFillRate")]
public class ManaFillRateIncrease_SO : PowerUp_SO
{
    public float[] fillRate;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    ManaManager.IncreaseManaFillRate?.Invoke(fillRate[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
