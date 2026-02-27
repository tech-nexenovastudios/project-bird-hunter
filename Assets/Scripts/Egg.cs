using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[Obsolete]public class EggObs : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float jumpImpulse = 8f;      // upward punch
    [SerializeField] private float wallKickImpulse = 2f;  // extra sideways push

    [Header("Bounds Settings")]
    [SerializeField] private Camera MainCam;
    [SerializeField] private Collider2D leftWall;   // Assign left wall collider for tablets
    [SerializeField] private Collider2D rightWall;  // Assign right wall collider for tablets

    [Header("Legacy Bounds (Fallback)")]
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;

    [SerializeField] GameObject SmokeParticle;
    [SerializeField] CircleCollider2D col;

    bool slowMo = true;
    bool useScreenBounds; // true = mobile, false = tablet

    // Dynamic bounds
    private float dynamicMinX;
    private float dynamicMaxX;


    public float point;

    private void Reset() => rb = GetComponent<Rigidbody2D>();

    private void Awake()
    {
        if (MainCam == null)
            MainCam = Camera.main;
    }

    private void Start()
    {
        CalculateBounds();
    }

    private void CalculateBounds()
    {
        // Check if walls are visible to determine device type
        useScreenBounds = !AreWallsVisible();

        float eggRadius = col.radius * transform.localScale.x; // Account for scale

        if (useScreenBounds)
        {
            // Mobile - use screen bounds
            Vector3 leftEdge = MainCam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2, 0));
            Vector3 rightEdge = MainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2, 0));

            dynamicMinX = leftEdge.x + eggRadius;
            dynamicMaxX = rightEdge.x - eggRadius;
        }
        else
        {
            // Tablet - use wall collider bounds
            Bounds leftBounds = leftWall.bounds;
            Bounds rightBounds = rightWall.bounds;

            float leftWallRightEdge = leftBounds.max.x;   // Right side of left wall
            float rightWallLeftEdge = rightBounds.min.x;  // Left side of right wall

            dynamicMinX = leftWallRightEdge + eggRadius;
            dynamicMaxX = rightWallLeftEdge - eggRadius;
        }

        // Fallback to legacy values if calculation fails
        if (dynamicMinX >= dynamicMaxX)
        {
            dynamicMinX = minX;
            dynamicMaxX = maxX;
            Debug.LogWarning("Egg bounds calculation failed, using fallback values");
        }

        //Debug.Log($"Egg bounds - Min X: {dynamicMinX}, Max X: {dynamicMaxX}, Using Screen Bounds: {useScreenBounds}");
    }

    private bool AreWallsVisible()
    {
        // Return false if wall colliders are not assigned
        if (leftWall == null || rightWall == null || MainCam == null)
            return false;

        // Get camera bounds in world space
        float cameraHeight = MainCam.orthographicSize * 2;
        float cameraWidth = cameraHeight * MainCam.aspect;

        Vector3 cameraPos = MainCam.transform.position;
        float cameraLeft = cameraPos.x - cameraWidth / 2;
        float cameraRight = cameraPos.x + cameraWidth / 2;

        // Check if both walls are within camera view
        bool leftWallVisible = leftWall.bounds.max.x >= cameraLeft && leftWall.bounds.min.x <= cameraRight;
        bool rightWallVisible = rightWall.bounds.max.x >= cameraLeft && rightWall.bounds.min.x <= cameraRight;

        return leftWallVisible && rightWallVisible;
    }

    /* ---------- Horizontal reflection ---------- */
    private void FixedUpdate()
    {
        Vector2 pos = rb.position;

        // Has the egg crossed the left or right bound?
        if (pos.x < dynamicMinX || pos.x > dynamicMaxX)
        {
            // 1) Snap inside the visible area
            pos.x = Mathf.Clamp(pos.x, dynamicMinX, dynamicMaxX);
            rb.position = pos;

            // 2) Reflect the horizontal velocity
            Vector2 v = rb.linearVelocity;
            v.x = -v.x;
            rb.linearVelocity = v;
        }

        if (CannonPower.instance != null)
        {
            if (CannonPower.instance.reduceGravity)
            {
                rb.linearVelocity = Vector2.zero;
                if (rb.linearVelocityY > 0)
                {
                    rb.gravityScale = 0.005f;
                }
                else
                {
                    rb.gravityScale = 0.5f;
                }
            }
            else
            {
                rb.gravityScale = 0.5f;
            }
        }
    }

    /* ---------- Ground & other collisions ---------- */
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Ground"))
        {
            //Destroy(Instantiate(SmokeParticle, transform.position - (transform.up * (transform.localScale.y - 1)) * col.radius, Quaternion.identity), 3f);
            Destroy(Instantiate(SmokeParticle, collision.contacts[0].point, Quaternion.identity), 3f);
            Vector2 v = rb.linearVelocity;
            v.y = 0f;  // kill any downward motion before the jump
            rb.linearVelocity = v;
            // Up and slightly sideways impulse
            float sideKick = Mathf.Clamp(v.x, -wallKickImpulse, wallKickImpulse);
            rb.AddForce(Vector2.up * jumpImpulse + Vector2.right * sideKick,
                        ForceMode2D.Impulse);
        }
        else if (collision.transform.CompareTag("Wall"))
        {
            Vector2 Normal = new Vector2();
            foreach (var contact in collision.contacts)
            {
                Normal += contact.normal;
            }
            rb.AddForce(Normal * 2f, ForceMode2D.Impulse);
        }
        else if (collision.transform.CompareTag("ForceField"))
        {
            Vector2 v = rb.linearVelocity;
            v.y = 0f;  // kill any downward motion before the jump
            rb.linearVelocity = v;
            // Up and slightly sideways impulse
            float sideKick = Mathf.Clamp(v.x, -wallKickImpulse, wallKickImpulse);
            rb.AddForce(Vector2.up * (jumpImpulse / 2) + Vector2.right * sideKick,
                        ForceMode2D.Impulse);
        }
    }

    // Public method to recalculate bounds if needed (e.g., screen orientation change)
    public void RecalculateBounds()
    {
        CalculateBounds();
    }

   
    private void OnEnable()
    {
        PointManager.AddPoint(point);
    }

    private void OnDisable()
    {
        PointManager.RemovePoint(point);
    }
    private void OnDestroy()
    {
        //PointManager.RemovePoint(point);
    }
}