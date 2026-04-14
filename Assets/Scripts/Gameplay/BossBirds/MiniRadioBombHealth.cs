// MiniRadioBombHealth.cs
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

/// <summary>
/// Attached at runtime to both the original bomb and mini-bombs.
/// Takes damage from player projectiles and deals contact damage to BaseCannon.
/// </summary>
public class MiniRadioBombHealth : MonoBehaviour, IDamageablee
{
    private int currentHealth;
    private int contactDamage;
    private float contactCooldownDuration;
    private float contactCooldownTimer;
    private System.Action<GameObject> onDeathCallback;

    public void Init(int maxHealth, int dmgOnContact, float contactCooldown,
                     System.Action<GameObject> onDeath)
    {
        currentHealth = maxHealth;
        contactDamage = dmgOnContact;
        contactCooldownDuration = contactCooldown;
        contactCooldownTimer = 0f;
        onDeathCallback = onDeath;
    }

    public void ResetState(int maxHealth)
    {
        currentHealth = maxHealth;
        contactCooldownTimer = 0f;
        onDeathCallback = null;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= (int)damage;
        if (currentHealth <= 0)
            onDeathCallback?.Invoke(gameObject);
    }

    private void Update()
    {
        if (contactCooldownTimer > 0f)
            contactCooldownTimer -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (contactCooldownTimer > 0f) return;

        if (col.gameObject.CompareTag("Player") &&
            col.gameObject.TryGetComponent<BaseCannon>(out var cannon))
        {
            cannon.TakeDamage(contactDamage);
            contactCooldownTimer = contactCooldownDuration;
        }
    }
}