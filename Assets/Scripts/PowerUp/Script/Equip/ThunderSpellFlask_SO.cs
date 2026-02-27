using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/ThunderSpellFlask")]
public class ThunderSpellFlask_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.ThunderSpellFlaskPower = true;
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
