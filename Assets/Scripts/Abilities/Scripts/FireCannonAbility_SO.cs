using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/FireCannon")]
public class FireCannonAbility_SO : Abilities_SO
{
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }
    
    public override void UseAbility(GameObject Player)
    {
        Debug.Log("Fire");
    }

   
}
