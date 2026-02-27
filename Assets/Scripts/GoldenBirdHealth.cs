using UnityEngine;

public class GoldenBirdHealth : MonoBehaviour,IHealthManager
{
    public float maxHealth;
    public float Health { get; set; }

    private void Start()
    {
        Health = maxHealth;
    }
    public void TakeDamage(float ApplyDamage)
    {
      Health -= ApplyDamage;
        if(Health < 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log("Die");
    }

   
}
