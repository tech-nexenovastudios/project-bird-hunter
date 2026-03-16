using UnityEngine;

public class DemoBulletSpawner : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float fireRate = 0.3f;

    private float nextFireTime;

    private Camera mainCam;
    private bool isDragging = false;
    private float zDistance;

    void Awake()
    {
        mainCam = Camera.main;
        zDistance = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
    }

    private void Update()
    {
        HandleMovement();
    }

    void HandleFire()
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.identity);
    }

    void HandleMovement()
    {
        // On mouse down, start dragging
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }

        // On mouse up, stop dragging
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            HandleFire();
            
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = zDistance; // keep correct depth
            Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);

            // Only move horizontally
            Vector3 newPos = transform.position;
            newPos.x = worldPos.x;
            transform.position = newPos;
        }
    }
}