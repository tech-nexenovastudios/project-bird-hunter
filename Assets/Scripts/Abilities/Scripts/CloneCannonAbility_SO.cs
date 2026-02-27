using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/CloneCannon")]
public class CloneCannonAbility_SO : Abilities_SO
{
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UseAbility(GameObject User)
    {
        Debug.Log("Clone");
    }
}
