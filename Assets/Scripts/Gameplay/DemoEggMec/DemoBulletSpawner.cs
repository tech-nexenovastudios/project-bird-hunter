using UnityEngine;
using UnityEngine.InputSystem;

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
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
            isDragging = true;
        if (pointer.press.wasReleasedThisFrame)
            isDragging = false;

        if (isDragging)
        {
            HandleFire();

            Vector3 mousePos = pointer.position.ReadValue();
            mousePos.z = zDistance;
            Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);

            Vector3 newPos = transform.position;
            newPos.x = worldPos.x;
            transform.position = newPos;
        }
    }
}
