// Fireball.cs
using Gameplay.Player;
using UnityEngine;

/// <summary>
/// Attached to fireball instances by FireballRainBehaviour. Detonates on contact with
/// the player (dealing damage) or with the ground, and self-destructs on lifetime.
/// </summary>
public class Fireball : MonoBehaviour
{
    private int contactDamage;
    private float lifetime;
    private float age;
    private GameObject impactVfxPrefab;
    private System.Action<GameObject> onDoneCallback;
    private bool detonated;
    private int groundLayer = -1;

    public void Launch(int damage, float maxLifetime, GameObject impactVfx, System.Action<GameObject> onDone)
    {
        contactDamage = damage;
        lifetime = Mathf.Max(0.1f, maxLifetime);
        age = 0f;
        impactVfxPrefab = impactVfx;
        onDoneCallback = onDone;
        detonated = false;
        if (groundLayer < 0) groundLayer = LayerMask.NameToLayer("Ground");
    }

    private void Update()
    {
        if (detonated) return;
        age += Time.deltaTime;
        if (age >= lifetime)
            Detonate(playVfx: false);
    }

    // Cannon collider is a trigger, ground typically isn't — handle both event paths
    // so the fireball reliably detonates regardless of which collider type it touches.
    private void OnTriggerEnter2D(Collider2D other) => HandleContact(other.gameObject);
    private void OnCollisionEnter2D(Collision2D col) => HandleContact(col.gameObject);

    private void HandleContact(GameObject other)
    {
        if (detonated) return;

        if (other.CompareTag("Player") && other.TryGetComponent<BaseCannon>(out var cannon))
        {
            cannon.TakeDamage(contactDamage);
            Detonate(playVfx: true);
            return;
        }

        bool isGround =
            (groundLayer >= 0 && other.layer == groundLayer) ||
            other.CompareTag("Ground");
        if (isGround)
            Detonate(playVfx: true);
    }

    private void Detonate(bool playVfx)
    {
        if (detonated) return;
        detonated = true;

        if (playVfx && impactVfxPrefab != null)
        {
            var vfx = Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        onDoneCallback?.Invoke(gameObject);
    }
}
