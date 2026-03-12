using UnityEngine;


[CreateAssetMenu(menuName ="Abilities/Healing")]
public class HealingAbility_SO : Abilities_SO
{

    public float[] healthIncreasePercentageByLevel;
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UseAbility(GameObject Player)
    {
        Player.GetComponent<CannonPower>().RestoreHP((Player.GetComponent<CannonHealthOld>().maxHealth* healthIncreasePercentageByLevel[powerLevel]) /100);
    }
}
