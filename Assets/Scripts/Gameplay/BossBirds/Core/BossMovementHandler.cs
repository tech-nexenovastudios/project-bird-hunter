using UnityEngine;

public class BossMovementHandler : MonoBehaviour
{
    private BossMovementConfig config;
    private float direction = 1f;
    private float hoverTimer;
    private bool isStopped;
    private float speedMultiplier = 1f;

    // All bounds derived from camera at runtime
    private float screenMinX;
    private float screenMaxX;
    private float screenMinY;
    private float screenMaxY;
    private float screenCenterY;

    public void Initialize(BossMovementConfig cfg)
    {
        config = cfg;
        direction = 1f;
        isStopped = false;
        speedMultiplier = 1f;
        CalculateScreenBounds();
    }

    private void CalculateScreenBounds()
    {
        Camera cam = Camera.main;
        float z = transform.position.z - cam.transform.position.z;

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        float halfWidth = GetHalfWidth();

        screenMinX = bottomLeft.x + halfWidth;
        screenMaxX = topRight.x - halfWidth;
        screenMinY = bottomLeft.y;
        screenMaxY = topRight.y;

        // Vertical anchor for hover/horizontal — upper portion of screen
        screenCenterY = Mathf.Lerp(screenMinY, screenMaxY, config.hoverVerticalAnchor);
    }

    private float GetHalfWidth()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return sr.bounds.extents.x;
        return config.screenEdgePadding; // Spine fallback — tune in Inspector
    }

    public void ApplySpeedMultiplier(float mult) => speedMultiplier = mult;
    public void Stop() => isStopped = true;
    public void Resume() => isStopped = false;   

    private void Update()
    {
        if (isStopped || config == null) return;

        switch (config.pattern)
        {
            case BossMovementConfig.MovementPattern.HorizontalSweep:
                MoveHorizontal();
                break;
            case BossMovementConfig.MovementPattern.Erratic:
                MoveErratic();
                break;
            case BossMovementConfig.MovementPattern.Hovering:
                MoveHover();
                break;
            default:
                MoveHorizontal();
                break;
        }
    }

    private void MoveHorizontal()
    {
        var pos = transform.position;

        pos.x += direction * config.speed * speedMultiplier * Time.deltaTime;

        // Y oscillates around screen-derived anchor — not manual config bounds
        hoverTimer += Time.deltaTime * 2f;
        pos.y = screenCenterY + Mathf.Sin(hoverTimer) * config.hoverAmplitude;
        pos.y = Mathf.Clamp(pos.y, screenMinY, screenMaxY);

        // Clamp + flip at screen edges
        if (pos.x >= screenMaxX) { pos.x = screenMaxX; direction = -1f; }
        if (pos.x <= screenMinX) { pos.x = screenMinX; direction = 1f; }

        transform.position = pos;
    }

    private void MoveErratic()
    {
        var pos = transform.position;
        pos += (Vector3)(Random.insideUnitCircle * config.speed * speedMultiplier * Time.deltaTime);

        // Erratic still uses screen-derived bounds for safety
        pos.x = Mathf.Clamp(pos.x, screenMinX, screenMaxX);
        pos.y = Mathf.Clamp(pos.y, screenMinY, screenMaxY);

        transform.position = pos;
    }

    private void MoveHover()
    {
        hoverTimer += Time.deltaTime;
        var pos = transform.position;

        // Oscillate around screen-space anchor
        pos.y = screenCenterY + Mathf.Sin(hoverTimer) * config.hoverAmplitude;

        // Safety clamp in case hoverAmplitude is large
        pos.y = Mathf.Clamp(pos.y, screenMinY, screenMaxY);

        transform.position = pos;
    }
}