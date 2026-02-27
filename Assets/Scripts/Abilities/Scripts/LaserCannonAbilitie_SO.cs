using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/LaserCannon")]
public class LaserCannonAbility_SO : Abilities_SO
{
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UseAbility(GameObject Player)
    {
        Debug.Log("Laser");
    }

    
}
