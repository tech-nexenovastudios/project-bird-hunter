using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/RocketCannon")]
public class RocketCannonAbility_SO : Abilities_SO
{
    public List<float> rocketDamage;
    public List<float> rocketSpeed;
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UseAbility(GameObject Player)
    {
        //Player.GetComponent<CannonAbility>().UseRocket(rocketDamage[powerLevel], rocketSpeed[powerLevel]);
        Debug.Log($"Using {Player} Applying Rocket Damage");
    }
}
