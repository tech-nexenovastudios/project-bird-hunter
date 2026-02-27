using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/RevivePlayer")]
public class RevivePlayer_SO : PowerUp_SO
{
    public float[] healthValue;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.revivePlayer(true, healthValue[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
