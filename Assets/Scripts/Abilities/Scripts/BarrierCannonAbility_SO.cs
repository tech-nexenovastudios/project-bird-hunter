using UnityEngine;


[CreateAssetMenu(menuName = "Abilities/BarrierCannon")]
public class BarrierCannonAbility_SO : Abilities_SO
{
    private GameObject barrier;
    public float[] activeTime;
    public override void UpgradeAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UseAbility(GameObject player)
    {
        if (barrier == null)
        {
            barrier = Instantiate(player.GetComponent<CannonPower>().barrier);
            Debug.Log(player);
            barrier.GetComponent<BarrierShield>().player = player;
        }
   
        barrier.SetActive(true);
        barrier.GetComponent<BarrierShield>().activeDuration = activeTime[powerLevel];


    }
}
