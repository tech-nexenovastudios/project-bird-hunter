using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/GravityPull")]
public class GravityPull_SO : PowerUp_SO
{
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    throw new System.NotImplementedException();
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }

    
}
