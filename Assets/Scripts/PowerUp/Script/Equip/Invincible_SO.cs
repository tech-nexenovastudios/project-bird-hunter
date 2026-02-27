using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Invincible")]
public class Invincible_SO : PowerUp_SO
{
    public float[] invincibleTime;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.InvincibleCannon(true, invincibleTime[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
