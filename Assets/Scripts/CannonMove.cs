using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CannonMove : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] BoxCollider2D col;
    [SerializeField] Camera MainCam;

    public float speed;

    [Header("Movement Limit")]
    [SerializeField] float minY;
    [SerializeField] float maxY;

    [Header("Tablet Wall Colliders")]
    [SerializeField] Collider2D leftWall;
    [SerializeField] Collider2D rightWall;
    bool useScreenBounds;

    [Header("Direction")]
    float currentPos;
    float newPos;
    float Direction;

    [Header("Tyres")]
    public Transform wheelParent;
    public List<Transform> wheelList;
    public Transform tyre1;
    public Transform tyre2;
    [SerializeField] float offset;
    public bool FreeMove;

    [Header("Fire")]
    public bool firePower;
    public float fireDamage;
    public float fireDamageDuration;
    public float fireDestroyTime;
    public GameObject firePrefab;
    public Transform rightWheel;
    public Transform leftWheel;
    private float distancePos;
    public float distanceToSpawnFire;
    public Transform midPos;
    public float distance;
    [SerializeField] LayerMask groundMask;
    bool isGrounded;
    Queue<GameObject> firePool = new();

    public CannonStats cannonStats;

    [Tooltip("Min and Max Offset To ScreenBorder")]
    public Vector2 boundOffset;

    // ── FIX 1: Store base speed so FreezeEffect restores correctly ──
    private float baseSpeed;
    bool freeze;

    // ── FIX 2: Remove mouseHoldingTime threshold — move immediately ──
    Vector3 MousePos;
    bool isTouching;

    [SerializeField] AnimationCurve movementCurve;
    [Range(0, 1)] public float moveValue;

    float leftLimit;
    float rightLimit;

    private void OnValidate()
    {
        if (wheelParent != null)
            wheelList = wheelParent.Cast<Transform>().ToList();
    }

    private void Awake()
    {
        MainCam = Camera.main;

        if (cannonStats != null)
            speed = cannonStats.currentMoveSpeed;

        baseSpeed = speed; // Cache for FreezeEffect restore

        // var colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        // leftWall  = colliders.FirstOrDefault(c => c.name == "WallLeft");
        // rightWall = colliders.FirstOrDefault(c => c.name == "WallRight");
    }

    private void Start()
    {
        currentPos = transform.position.x;
        useScreenBounds = !AreWallsVisible();
        ScreenBoundCalculate();
        MousePos = transform.position; // Start at cannon position — no snap on first touch
    }

    public void SetWalls(Collider2D leftWall, Collider2D rightWall)
    {
        this.leftWall = leftWall;
        this.rightWall = rightWall;
    }

    private bool AreWallsVisible()
    {
        if (leftWall == null || rightWall == null) return false;

        float cameraHeight = MainCam.orthographicSize * 2;
        float cameraWidth  = cameraHeight * MainCam.aspect;
        float cameraLeft   = MainCam.transform.position.x - cameraWidth / 2;
        float cameraRight  = MainCam.transform.position.x + cameraWidth / 2;

        bool leftVisible  = leftWall.bounds.max.x  >= cameraLeft && leftWall.bounds.min.x  <= cameraRight;
        bool rightVisible = rightWall.bounds.max.x >= cameraLeft && rightWall.bounds.min.x <= cameraRight;
        return leftVisible && rightVisible;
    }

    private void Update()
    {
        GroundCheck();
        InputCheck();
        WheelRotationSet();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    public void InputCheck()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // ── FIX 3: Correct UI check using finger ID ──
            if (IsPointerOverUI(touch.fingerId)) return;

            // ── FIX 2: No delay — update MousePos immediately on any touch phase ──
            MousePos   = MainCam.ScreenToWorldPoint(touch.position);
            MousePos.z = 0;
            isTouching = true;
        }
        else
        {
            isTouching = false;
        }
    }

    public void ScreenBoundCalculate()
    {
        float halfWidth = col.bounds.extents.x;

        if (useScreenBounds)
        {
            leftLimit  = ScreenBounds.minX + halfWidth;
            rightLimit = ScreenBounds.maxX - halfWidth;
        }
        else
        {
            float leftWallRightEdge  = leftWall.bounds.max.x;
            float rightWallLeftEdge  = rightWall.bounds.min.x;
            leftLimit  = leftWallRightEdge + halfWidth;
            rightLimit = rightWallLeftEdge - halfWidth;
        }
    }

    public void Movement()
    {
        if (!isTouching) return;

        // ── FIX 1: Use Time.fixedDeltaTime in FixedUpdate — no more jitter ──
        float targetX  = Mathf.Lerp(rb.position.x, MousePos.x, speed * Time.fixedDeltaTime);
        float clampedX = Mathf.Clamp(targetX, leftLimit + boundOffset.x, rightLimit - boundOffset.y);

        float targetY = FreeMove
            ? Mathf.Clamp(Mathf.Lerp(rb.position.y, MousePos.y, speed * Time.fixedDeltaTime), minY, maxY)
            : transform.position.y;

        rb.MovePosition(new Vector2(clampedX + offset, targetY));

        newPos = clampedX;

        // ── FIX 1: Direction uses fixedDeltaTime consistently ──
        Direction  = (currentPos - newPos) / Time.fixedDeltaTime;
        currentPos = newPos;

        // Fire trail
        if (firePower && isGrounded)
        {
            if (Mathf.Abs(distancePos - newPos) > distanceToSpawnFire)
            {
                SpawnFire(rightWheel);
                SpawnFire(leftWheel);
                distancePos = currentPos;
            }
        }
    }

    private void SpawnFire(Transform wheel)
    {
        var go = Instantiate(firePrefab,
            wheel.position + transform.up * 0.15f,
            Quaternion.Euler(-90, 0, 0));

        if (go != null) SetFireValue(go.GetComponent<Fire>());
    }

    public void WheelRotationSet()
    {
        if (!isGrounded) return;
        foreach (var wheel in wheelList)
        {
            wheel.rotation = Quaternion.Euler(0, 0,
                wheel.eulerAngles.z + (Time.deltaTime * Direction * 100) % 360);
        }
    }

    public void SetFireValue(Fire fire)
    {
        fire.fireDamage    = fireDamage;
        fire.damageDuration = fireDamageDuration;
        Destroy(fire.gameObject, fireDestroyTime);
    }

    public void OnDrawGizmos()
    {
        if (midPos == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(midPos.position, Vector3.down * distance);
    }

    public void GroundCheck()
    {
        isGrounded = Physics2D.Raycast(midPos.position, Vector3.down, distance, groundMask);
    }

    // ── FIX 3: Proper touch UI detection using fingerId ──
    private bool IsPointerOverUI(int fingerId)
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    // ── FIX 4: FreezeEffect uses baseSpeed for safe restore ──
    public void FreezeEffect(float effectTime)
    {
        if (!freeze)
        {
            freeze = true;
            speed  = baseSpeed / 4f;
            Invoke(nameof(ResetFreeze), effectTime);
        }
    }

    private void ResetFreeze()
    {
        freeze = false;
        speed  = baseSpeed;
    }
}
