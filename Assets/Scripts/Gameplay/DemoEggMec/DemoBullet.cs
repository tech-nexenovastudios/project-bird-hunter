using UnityEngine;

public class DemoBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 15f;
    public float maxHitForce = 8f;  // force for close eggs
    public float minHitForce = 2f;  // force for far eggs
    public float maxForceDistance = 10f; // distance at which force becomes minHitForce

    private Rigidbody2D rb;
    private Vector2 startPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        startPosition = transform.position;
        rb.linearVelocity = Vector2.up * speed;  // move bullet up
        Destroy(gameObject, 3f);           // auto cleanup
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Egg"))
        {
            DemoEgg egg = collision.gameObject.GetComponent<DemoEgg>();
            Rigidbody2D eggRb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (egg != null && eggRb != null)
            {
                if (egg.health <= 0)
                {
                    egg.Split();
                }
                else
                {
                    // reduce health
                    egg.health--;

                    // calculate distance-based hit force
                    float distance = Vector2.Distance(startPosition, transform.position);
                    float t = Mathf.Clamp01(distance / maxForceDistance);
                    float hitForce = Mathf.Lerp(maxHitForce, minHitForce, t);

                    // apply force toward bullet hit direction
                    Vector2 hitDirection = collision.contacts[0].normal * -1;
                    eggRb.AddForce(hitDirection * hitForce, ForceMode2D.Impulse);
                }
            }

            Destroy(gameObject); // destroy bullet after hit
        }
    }
}