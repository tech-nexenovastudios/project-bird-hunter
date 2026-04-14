// DroneHealth.cs
using Gameplay.Interfaces;
using UnityEngine;

public class DroneHealth : MonoBehaviour, IDamageablee
{
    private float maxHealth;
    private float currentHealth;
    private int contactDamage;
    private float damageCooldown;
    private float lastContactTime;

    public System.Action OnDroneKilled;

    public void Initialize(float hp, int contactDmg, float contactCd)
    {
        maxHealth = hp;
        currentHealth = hp;
        contactDamage = contactDmg;
        damageCooldown = contactCd;
        lastContactTime = -999f;
        OnDroneKilled = null;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDroneKilled?.Invoke();
        }
    }

    // Drones also deal contact damage to the cannon when touched
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (currentHealth <= 0f) return;
        if (Time.time - lastContactTime < damageCooldown) return;

        if (collision.collider.CompareTag("Player") &&
            collision.collider.TryGetComponent<IDamageable>(out var target))
        {
            lastContactTime = Time.time;
            target.TakeDamage(contactDamage);
        }
    }

    public void ResetState()
    {
        currentHealth = maxHealth;
        lastContactTime = -999f;
        OnDroneKilled = null;
    }
}