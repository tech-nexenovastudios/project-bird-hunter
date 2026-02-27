using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/BulletPierceEnemies")]
public class BulletPierceEnemies_SO : PowerUp_SO
{
    public float[] chance;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.ChanceOfBulletPierceEnemies(true, chance[tempPowerLevel]);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
