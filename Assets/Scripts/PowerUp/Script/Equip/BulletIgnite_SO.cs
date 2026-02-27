using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/BulletIgnite")]
public class BulletIgnite_SO : PowerUp_SO
{
    [SerializeField] public float[] duration;
    public override void GrantAbility(CannonPower power)
    {
    base.GrantAbility(power);
    power.IgniteBullet(true, duration[tempPowerLevel]);
    }

   public override void UpgradePower()
    {
       base.UpgradePower();
    }
}
