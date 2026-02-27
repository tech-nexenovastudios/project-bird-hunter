using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/GiveAllEnemyDamage")]
public class GiveAllEnemyDamage_SO : PowerUp_SO
{
    public float[] damage;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.DamageObj(true, damage[tempPowerLevel]);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
