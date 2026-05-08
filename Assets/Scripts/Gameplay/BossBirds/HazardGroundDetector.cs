// HazardGroundDetector.cs
using System;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

public class HazardGroundDetector : MonoBehaviour, IPoolable, IDamageable
{
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float destroyDelayAfterStop = 1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Camera mainCam;
    private float rollSpeed;
    private float maxRollDistance;
    private Vector3 rollStartPosition;
    private bool hasLanded;
    private bool hasStopped;
    private bool isDestroyed;
    private BaseCannon slowedCannon;
    private int groundLayer = -1;

    public int CurrentHp => isDestroyed ? 0 : 1;
    public int MaxHp => 1;
    public bool IsAlive => !isDestroyed;

    public event Action<HazardGroundDetector> OnDestroyed;

    private void Awake()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (groundLayer < 0) groundLayer = LayerMask.NameToLayer("Ground");
    }

    public void Initialize(Rigidbody2D rigidbody, float speed, float maxDistance = 0f)
    {
        rb = rigidbody;
        rollSpeed = speed;
        maxRollDistance = maxDistance;
        hasLanded = false;
        hasStopped = false;
        isDestroyed = false;
        if (col == null) col = GetComponent<Collider2D>();
        if (mainCam == null) mainCam = Camera.main;
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        DestroyHazard();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        bool isGround = (groundLayer >= 0 && collision.gameObject.layer == groundLayer)
                        || collision.collider.CompareTag("Ground");
        if (!isGround) return;

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
        rollStartPosition = transform.position;
        GetComponent<HazardRollTrail>()?.StartTrail();
    }

    private void Update()
    {
        if (!hasLanded || hasStopped || rb == null) return;

        Vector3 pos = transform.position;

        if (maxRollDistance > 0f)
        {
            float dx = pos.x - rollStartPosition.x;
            if (Mathf.Abs(dx) >= maxRollDistance)
            {
                pos.x = rollStartPosition.x + Mathf.Sign(dx) * maxRollDistance;
                transform.position = pos;
                StopRolling();
                return;
            }
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || col == null) return;

        Vector3 bottomLeft = mainCam.ViewportToWorldPoint(Vector3.zero);
        Vector3 topRight = mainCam.ViewportToWorldPoint(Vector3.one);
        float boundsWidth = col.bounds.extents.x;
        float leftBound = bottomLeft.x + boundsWidth;
        float rightBound = topRight.x - boundsWidth;

        Vector2 vel = rb.linearVelocity;
        if (pos.x < leftBound && vel.x < 0f)
        {
            pos.x = leftBound;
            transform.position = pos;
            StopRolling();
        }
        else if (pos.x > rightBound && vel.x > 0f)
        {
            pos.x = rightBound;
            transform.position = pos;
            StopRolling();
        }
    }

    private void StopRolling()
    {
        if (rb == null) return;
        hasStopped = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        GetComponent<HazardRollTrail>()?.StopTrail();

        // The egg has finished dropping its hazard stamps — clean itself up shortly.
        Invoke(nameof(DestroyHazard), Mathf.Max(0f, destroyDelayAfterStop));
    }

    private void DestroyHazard()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        OnDestroyed?.Invoke(this);
    }

    public void OnPoolSpawned()
    {
        hasLanded = false;
        hasStopped = false;
        isDestroyed = false;
        slowedCannon = null;
        CancelInvoke();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.None;
            rb.gravityScale = 1.2f;
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

        OnDestroyed = null;
        hasLanded = false;
        hasStopped = false;
        isDestroyed = false;
        CancelInvoke();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.None;
            rb.gravityScale = 1.2f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
