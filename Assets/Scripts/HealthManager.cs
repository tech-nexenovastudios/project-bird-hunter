using UnityEngine;

public class HealthManager : MonoBehaviour, IHealthManager
{
    [SerializeField] float currentHealth;
    public void TakeDamage(float ApplyDamage)
    {
        currentHealth -= ApplyDamage;
        if (currentHealth <= 0)
        {
            Destroy(this.gameObject);                                                       
        }
    }
}
