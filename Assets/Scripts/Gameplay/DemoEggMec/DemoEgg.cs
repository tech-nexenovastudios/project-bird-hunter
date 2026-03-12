using UnityEngine;

public class DemoEgg : MonoBehaviour
{
    [Header("Splitting Settings")]
    public GameObject smallerEggPrefab;
    public int splitLevel = 2; // times it can split
    public float splitForce = 4f;

    [Header("Health Settings")]
    public int health = 20;

    [Header("Bounce Settings")]
    public float bounceVelocity = 8f;       // Y velocity on ground hit
    public float bounceIncrease = 1.5f;     // extra bounce for smaller eggs
    public float horizontalIncrease = 1.2f; // horizontal push multiplier
    public float maxSpeed = 12f;            // clamp speed for safety

    private Rigidbody2D rb;
    private Camera mainCam;
    private float halfWidth; // for screen edge detection

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        // approximate egg width from collider
        Collider2D col = GetComponent<Collider2D>();
        halfWidth = col.bounds.extents.x;
    }

    void FixedUpdate()
    {
        // Clamp maximum velocity so eggs don't fly off screen
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);

        HandleScreenEdges();
    }

    void HandleScreenEdges()
    {
        Vector3 pos = transform.position;

        // Get camera bounds in world units
        float leftBound = mainCam.ViewportToWorldPoint(Vector3.zero).x + halfWidth;
        float rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;

        // Bounce off left/right edges
        if (pos.x < leftBound)
        {
            pos.x = leftBound;
            rb.linearVelocity = new Vector2(Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y);
        }
        else if (pos.x > rightBound)
        {
            pos.x = rightBound;
            rb.linearVelocity = new Vector2(-Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y);
        }

        transform.position = pos;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Vector2 v = rb.linearVelocity;
            v.y = bounceVelocity;
            rb.linearVelocity = v;
        }
    }

    public void Split()
    {
        if (smallerEggPrefab == null)
        {
            Debug.LogWarning("smallerEggPrefab not assigned for " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        
        if (splitLevel <= 0)
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject newEgg = Instantiate(smallerEggPrefab, transform.position, Quaternion.identity);

            DemoEgg eggScript = newEgg.GetComponent<DemoEgg>();
            eggScript.splitLevel = splitLevel - 1;

            // Increase bounce for smaller eggs
            eggScript.bounceVelocity = bounceVelocity + bounceIncrease;

            Rigidbody2D newRb = newEgg.GetComponent<Rigidbody2D>();
            Vector2 dir = (i == 0) ? Vector2.left : Vector2.right;

            newRb.AddForce(
                new Vector2(dir.x * splitForce * horizontalIncrease, splitForce),
                ForceMode2D.Impulse
            );
        }

        Destroy(gameObject);
    }
}