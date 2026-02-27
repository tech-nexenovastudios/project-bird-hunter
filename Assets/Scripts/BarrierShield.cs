using UnityEngine;

public class BarrierShield : MonoBehaviour
{
    public GameObject player;
    public float activeDuration;

    private void Update()
    {
        transform.position = player.transform.position+transform.up*0.66f;
        activeDuration -= Time.deltaTime;
        if(activeDuration <= 0)
        {
            this.gameObject.SetActive(false);
        }
    }
}
