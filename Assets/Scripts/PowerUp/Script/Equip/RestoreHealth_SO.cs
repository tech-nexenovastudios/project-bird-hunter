using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/RestoreHealth")]
public class RestoreHealth_SO : PowerUp_SO
{
    public float[] value;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.RestoreHP(value[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
