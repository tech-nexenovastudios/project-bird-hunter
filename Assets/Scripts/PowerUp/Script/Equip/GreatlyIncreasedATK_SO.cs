using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/GreatlyIncreasedATK")]
public class GreatlyIncreasedATK_SO : PowerUp_SO
{
    public float[] duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.IncreaseDamageAfterHit(true,5f, duration[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
