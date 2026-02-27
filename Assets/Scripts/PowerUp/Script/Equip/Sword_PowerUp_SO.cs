using UnityEngine;


[CreateAssetMenu(menuName = "PowerUps/SwordEquip")]
public class Sword_PowerUp_SO : PowerUp_SO
{
    public float[] Damage;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.SwordEquip(true, Damage[tempPowerLevel],3,10); 
    }
   public override void UpgradePower()
    {
       base.UpgradePower();
    }

   

}
