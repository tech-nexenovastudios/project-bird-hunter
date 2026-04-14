// HazardGroundDetector.cs
using UnityEngine;
using Gameplay.Player;

public class HazardGroundDetector : MonoBehaviour, IPoolable
{
    [SerializeField] private float slowMultiplier = 0.5f;

    private Rigidbody2D rb;
    private float rollSpeed;
    private bool hasLanded;
    private BaseCannon slowedCannon;

    public void Initialize(Rigidbody2D rigidbody, float speed)
    {
        rb = rigidbody;
        rollSpeed = speed;
        hasLanded = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                hasLanded = true;
                StartRolling();
                return;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<BaseCannon>(out var cannon)) return;

        slowedCannon = cannon;
        cannon.ApplySpeedMultiplier(slowMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (slowedCannon == null) return;
        if (!other.TryGetComponent<BaseCannon>(out var cannon)) return;
        if (cannon != slowedCannon) return;

        cannon.ResetSpeedMultiplier();
        slowedCannon = null;
    }

    private void StartRolling()
    {
        if (rb == null) return;
        rb.constraints = RigidbodyConstraints2D.FreezePositionY;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(rollSpeed, 0f);
        rb.angularVelocity = -rollSpeed * 50f;
        GetComponent<HazardRollTrail>()?.StartTrail();
    }

    public void OnPoolSpawned()
    {
        hasLanded = false;
        slowedCannon = null;
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.None;
            rb.gravityScale = 3f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void OnPoolDespawned()
    {
        if (slowedCannon != null)
        {
            slowedCannon.ResetSpeedMultiplier();
            slowedCannon = null;
        }

        hasLanded = false;
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.None;
            rb.gravityScale = 3f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}