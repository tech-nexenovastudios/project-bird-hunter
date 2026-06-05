using Gameplay.Player;
using UnityEngine;

/// <summary>
/// Health for a Haunting Wisp (boss-4 add). Shootable via the player bullet's "BossBird"-tag
/// path (IDamageablee), and kamikaze on contact with the cannon — deals contact damage and dies.
/// Movement/homing is driven by HauntingWispsBehaviour; this only owns HP + contact + death.
/// </summary>
public class WispHealth : MonoBehaviour, IDamageablee
{
    private float currentHealth;
    private int contactDamage;
    private bool isDead;

    // Fires on death (shot down OR kamikaze). The behaviour uses it to recycle the wisp.
    public System.Action<GameObject> OnKilled;

    public void Initialize(float hp, int contactDmg)
    {
        currentHealth = Mathf.Max(1f, hp);
        contactDamage = contactDmg;
        isDead = false;
        OnKilled = null;
    }

    // IDamageablee (boss-side float path) — player bullets call this via OnHitBossBird.
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0f) Die();
    }

    // Cannon's collider may be trigger or solid — listen to both, like MiniRadioBombHealth.
    private void OnTriggerEnter2D(Collider2D other) => HandleContact(other.gameObject);
    private void OnCollisionEnter2D(Collision2D col) => HandleContact(col.gameObject);

    private void HandleContact(GameObject other)
    {
        if (isDead) return;
        if (!other.CompareTag("Player")) return;
        if (other.TryGetComponent<BaseCannon>(out var cannon))
            cannon.TakeDamage(contactDamage);
        Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnKilled?.Invoke(gameObject);
    }
}
