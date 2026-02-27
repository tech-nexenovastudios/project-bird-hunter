using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/BigBullet")]
public class BigBullet_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.BigBullet(true, 25, 3, 2);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
