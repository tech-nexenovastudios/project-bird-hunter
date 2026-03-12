using UnityEngine;

public class EggLimiter : MonoBehaviour
{
    Rigidbody2D rb;

    public float maxSpeed = 5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }
}