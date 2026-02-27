using UnityEngine;

public class Fire : MonoBehaviour
{
    public float fireDamage;
    public float damageDuration;
    private void OnTriggerEnter2D(Collider2D collision)
    {
   
       var damage =  collision.GetComponent<IDamageEffect>();
        if(damage != null)
        {
            damage.igniteDamage(fireDamage, damageDuration);
        }
    }

}
