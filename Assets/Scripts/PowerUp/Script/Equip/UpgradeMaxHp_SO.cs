using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/UpgradeHealth")]
public class UpgradeMaxHp : PowerUp_SO
{
    public float[] value;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.UpdateMaxHP(value[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }

    
}
