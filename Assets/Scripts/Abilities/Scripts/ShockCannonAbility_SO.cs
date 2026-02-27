using System.Threading.Tasks;
using UnityEngine;
[CreateAssetMenu(menuName = "Abilities/ShockCannon")]
public class ShockCannonAbility_SO : Abilities_SO
{
    public float[] damage;
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UseAbility(GameObject Player)
    {
        //Player.GetComponent<CannonAbility>().activeAbility.UseElectricField(damage[powerLevel]);
       Debug.Log($"Using {Player} Applying Electric Damage");
    }

  

 
}
