using Gameplay.Interfaces;
using UnityEngine;

public class LaserBarrage : MonoBehaviour, IDamageablee   // correct — boss-side interface
{
    [SerializeField] private float damage    = 5f;
    [SerializeField] private float maxHealth = 5f;

    private float currentHealth;

    private void OnEnable()
    {
        currentHealth = maxHealth;       // reset on every pool spawn
    }

    // Called by BaseBullet.OnHitBossWeapon via IDamageablee
    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        Debug.Log($"Laser took {dmg} damage, HP remaining: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log("Laser Barrage destroyed");
        PoolManager.Return(gameObject);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        
        if (collision.TryGetComponent<IDamageable>(out var damageable))
        {
            Debug.Log("Laser hit player!");
            damageable.TakeDamage((int)damage);
        }
    }
}