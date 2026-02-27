using UnityEngine;
[CreateAssetMenu(menuName = "PowerUps/SteelSpike")]
public class SteelSpike : PowerUp_SO
{
    public float[] area;
    public override void GrantAbility(CannonPower power)
    {
        base.GrantAbility(power);
        UpgradePower();
        power.SteelSpike(area[powerLevel]);
    }
    public override void UpgradePower()
    {
        base.UpgradePower();
    }
}
