using UnityEngine;

public class OrbParent : MonoBehaviour
{
    public CannonFire cannonFire;
    public float duration;
    public float damage;

    private void Start()
    {
        if (cannonFire != null)
        {
            transform.GetChild(0).GetComponent<Orb>().orbParent = this;
            transform.GetChild(1).GetComponent<Orb>().orbParent = this;
        }
    }

    public void AssignValue(float duration,float damage)
    {
        this.duration = duration;
        this.damage = damage;
    }
}
