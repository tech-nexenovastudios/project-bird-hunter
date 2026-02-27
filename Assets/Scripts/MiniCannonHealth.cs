using UnityEngine;

public class MiniCannonHealth : MonoBehaviour, IHealthManager
{
    public float health;
    public void TakeDamage(float ApplyDamage)
    {
        health -= ApplyDamage;
        if(health <= 0f)
        {
            Destroy(gameObject);    
        }
    }
}
