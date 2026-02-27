using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/ShadowCannon")]
public class ShadowCannon_SO : PowerUp_SO
{
    public float[] damage;
    public override void GrantAbility(CannonPower power)
    {
        base.GrantAbility(power);
        power.ShadowCannon(damage[tempPowerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
