// MiniRadioBombHealth.cs
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

/// <summary>
/// Attached at runtime to both the original bomb and mini-bombs.
/// Takes damage from player projectiles and detonates on contact with the cannon —
/// dealing its current remaining HP as damage and dying.
/// Implements both IDamageable (single-e, used by BaseBullet's "Egg"-tag path) and
/// IDamageablee (double-e, the legacy boss-side interface) so the bomb works whether
/// it's tagged Egg, BossBird, or BossWeapons.
/// </summary>
public class MiniRadioBombHealth : MonoBehaviour, IDamageablee, IDamageable
{
    private int currentHealth;
    private int maxHealth;
    private int contactDamage;            // kept for back-compat with config; unused now that contact deals currentHealth
    private float contactCooldownDuration; // unused after kamikaze rework
    private float contactCooldownTimer;
    private bool isDead;
    private System.Action<GameObject> onDeathCallback;

    public int CurrentHealth => currentHealth;

    // ── IDamageable (single-e) ──
    public int CurrentHp => currentHealth;
    public int MaxHp => maxHealth;
    public bool IsAlive => !isDead;

    public void Init(int maxHp, int dmgOnContact, float contactCooldown,
                     System.Action<GameObject> onDeath)
    {
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;
        contactDamage = dmgOnContact;
        contactCooldownDuration = contactCooldown;
        contactCooldownTimer = 0f;
        isDead = false;
        onDeathCallback = onDeath;
    }

    public void ResetState(int maxHp)
    {
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;
        contactCooldownTimer = 0f;
        isDead = false;
        onDeathCallback = null;
    }

    // IDamageablee (double-e, boss-side) — float overload
    public void TakeDamage(float damage) => ApplyDamage((int)damage);

    // IDamageable (single-e, used by BaseBullet "Egg" path) — int overload
    public void TakeDamage(int damage) => ApplyDamage(damage);

    private void ApplyDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= Mathf.Max(0, damage);
        Debug.Log("RAVEN BALL TOOK : " + damage + "damage!");
        if (currentHealth <= 0)
            Die();
    }

    private void Update()
    {
        if (contactCooldownTimer > 0f)
            contactCooldownTimer -= Time.deltaTime;
    }

    // Cannon collider is a trigger — non-trigger ↔ trigger contacts only fire
    // OnTriggerEnter2D, not OnCollisionEnter2D. Listen to both so this works
    // regardless of the cannon's collider configuration.
    private void OnTriggerEnter2D(Collider2D other) => HandleContact(other.gameObject);
    private void OnCollisionEnter2D(Collision2D col) => HandleContact(col.gameObject);

    private void HandleContact(GameObject other)
    {
        if (isDead) return;
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<BaseCannon>(out var cannon)) return;

        // Kamikaze: deal damage equal to the bomb's remaining HP, then die.
        int damage = Mathf.Max(1, currentHealth);
        cannon.TakeDamage(damage);
        currentHealth = 0;
        Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        onDeathCallback?.Invoke(gameObject);
    }
}
