using UnityEngine;

public class BossBirdHealth : MonoBehaviour, IHealthManager
{
    public float maxHealth;
    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }
    public void TakeDamage(float ApplyDamage)
    {
        if (this.enabled)
        {
            currentHealth -= ApplyDamage;
            if (currentHealth <= 0)
            {
                GetComponent<Bird>().DestroyBird();
            }
        }
    }


}
