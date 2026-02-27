using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/KillEnemyDamage")]
public class KillEnemyDamage_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.DestroyDamageObj(true);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
