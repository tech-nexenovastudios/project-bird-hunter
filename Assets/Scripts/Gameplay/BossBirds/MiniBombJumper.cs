// MiniBombJumper.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MiniBombJumper : MonoBehaviour, IPoolable
{
    private Rigidbody2D rb;
    private float jumpForce;
    private bool isInitialized;
    private int groundMask;
    private bool isGrounded;

    // Prevent multiple jumps in same frame
    private float jumpCooldown = 0.15f;
    private float lastJumpTime;

    public void Init(float force)
    {
        jumpForce = force;
        isInitialized = true;
        isGrounded = false;
        lastJumpTime = -1f;

        rb = GetComponent<Rigidbody2D>();
        groundMask = LayerMask.GetMask("Ground");

        // Critical settings to prevent tunneling
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Make sure collider is NOT a trigger
        var col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger)
        {
            Debug.LogWarning($"[MiniBombJumper] Collider on {gameObject.name} is set to Trigger! " +
                             $"Changing to non-trigger so ground collision works.", this);
            col.isTrigger = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized || rb == null) return;

        // Ground check via short raycast downward from bottom of collider
        // More reliable than OnCollisionEnter2D for fast objects
        float rayLength = 0.15f;
        Vector2 origin = (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, groundMask);
        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        // Just landed — bounce
        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0f)
        {
            if (Time.time - lastJumpTime > jumpCooldown)
            {
                lastJumpTime = Time.time;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Kill downward velocity first
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }

        // Safety: if somehow below ground, push back up
        if (hit.collider != null && hit.distance < 0.01f)
        {
            Vector2 pos = transform.position;
            pos.y = hit.point.y + 0.1f;
            transform.position = pos;
        }
    }

    // Backup: OnCollisionEnter2D as fallback
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized) return;
        if (((1 << collision.gameObject.layer) & groundMask) == 0) return;

        // Check contact normal — must be hitting from above (landing on ground)
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                if (Time.time - lastJumpTime > jumpCooldown)
                {
                    lastJumpTime = Time.time;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                }
                return;
            }
        }
    }

    public void ResetState()
    {
        isGrounded = false;
        lastJumpTime = -1f;
        isInitialized = false;
    }

    public void OnPoolSpawned()
    {
        isGrounded = false;
        lastJumpTime = -1f;
    }

    public void OnPoolDespawned()
    {
        isInitialized = false;
    }
}