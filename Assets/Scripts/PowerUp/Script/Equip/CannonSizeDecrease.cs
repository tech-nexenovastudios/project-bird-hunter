using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/CannonSizeDecrease")]
public class CannonSizeDecrease : PowerUp_SO
{
    public float[] sizeDecreaseValue;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.DecreaseSize(sizeDecreaseValue[tempPowerLevel]);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
