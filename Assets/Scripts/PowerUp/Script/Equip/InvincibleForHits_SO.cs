using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/InvincibleForHits")]
public class InvincibleForHits_SO : PowerUp_SO
{
    public float[] invincibleCount;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.InvincibleForHits(invincibleCount[tempPowerLevel]);
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
