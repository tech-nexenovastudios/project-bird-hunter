// StoneGazeSweepBehaviour.cs
using System.Collections;
using Gameplay.Interfaces;
using UnityEngine;

public class StoneGazeSweepBehaviour : BaseAttackBehaviour
{
    private StoneGazeSweepConfig config;
    private Transform firePoint;
    private LaserBeamVisual laserVisual;
    private Transform cannonTarget;
    private Vector3 lockedDirection;
    private float currentBeamLength;
    private float damageTickInterval;
    private float damageTickTimer;
    private bool isHittingTarget;

    private static readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private static int hitMask;
    private static int projectileMask;
    private static bool masksInitialized;

    public void SetConfig(StoneGazeSweepConfig cfg) => config = cfg;

    private void Awake()
    {
        if (!masksInitialized)
        {
            hitMask = LayerMask.GetMask("Player", "Cannon");
            projectileMask = LayerMask.GetMask("PlayerBullet");
            masksInitialized = true;
        }
    }

    private void Start()
    {
        // Find fire point
        firePoint = boss.transform.Find(config.firePointName);
        if (firePoint == null) firePoint = boss.transform;

        // Setup laser visual
        if (config.laserVisualPrefab != null)
        {
            var obj = Instantiate(config.laserVisualPrefab, firePoint);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            laserVisual = obj.GetComponent<LaserBeamVisual>();
            laserVisual.SetColors(config.coreColor, config.glowColor, config.haloColor);
            laserVisual.Deactivate();
            obj.SetActive(false);
        }

        damageTickInterval = 1f / Mathf.Max(config.ticksPerSecond, 0.1f);
    }

    protected override void OnExecute()
    {
        ShowWarning(config, () => StartCoroutine(SweepSequence()));
    }

    private IEnumerator SweepSequence()
    {
        cannonTarget = GameObject.FindWithTag("Player")?.transform;

        Vector3 origin = firePoint.position;
        lockedDirection = GetDirection(origin);

        // Rotate fire point toward player
        if (config.rotateBeforeShoot && cannonTarget != null)
        {
            yield return StartCoroutine(RotateFirePoint());
            lockedDirection = firePoint.right;
        }

        // Activate
        if (laserVisual != null)
        {
            laserVisual.gameObject.SetActive(true);
            laserVisual.SetLinesEnabled(true);
        }
        damageTickTimer = 0f;

        // Warm-up
        float warmup = 0f;
        while (warmup < config.warmupDuration)
        {
            float t = EaseOut(warmup / config.warmupDuration);
            laserVisual?.SetWidthNormalized(t, config.widthMultiplier);
            UpdateBeam();
            warmup += Time.deltaTime;
            yield return null;
        }

        laserVisual?.SetWidth(config.widthMultiplier);

        // Main beam
        float elapsed = 0f;
        float mainDur = Mathf.Max(0f, config.duration - config.warmupDuration);
        while (elapsed < mainDur)
        {
            UpdateBeam();
            elapsed += Time.deltaTime;
            yield return null;
        }

        Shutdown();
    }

    private void UpdateBeam()
    {
        Vector3 origin = firePoint.position;
        currentBeamLength = CalculateLength(origin, lockedDirection);
        Vector3 end = origin + lockedDirection * currentBeamLength;

        laserVisual?.SetPositions(origin, end);

        // Petrify player projectiles that touch the beam
        PetrifyProjectilesInBeam(origin, lockedDirection);

        // Damage ticks
        damageTickTimer += Time.deltaTime;
        if (damageTickTimer >= damageTickInterval)
        {
            damageTickTimer -= damageTickInterval;
            DamagePlayer(origin, lockedDirection);
        }

        // Impact FX
        if (laserVisual != null)
        {
            if (isHittingTarget)
                laserVisual.ShowImpact(end, lockedDirection);
            else
                laserVisual.HideImpact();
        }
    }

    private void PetrifyProjectilesInBeam(Vector2 origin, Vector2 dir)
    {
        int count = Physics2D.RaycastNonAlloc(origin, dir, hits, currentBeamLength, projectileMask);
        for (int i = 0; i < count; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;

            // Freeze the projectile then destroy after delay
            if (col.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
            }

            // Spawn petrify VFX
            if (config.petrifyVfxPrefab != null)
                PoolManager.Get(config.petrifyVfxPrefab, col.transform.position);

            // Destroy after delay
            StartCoroutine(DestroyAfterDelay(col.gameObject, config.petrifyDelay));
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            if (obj.TryGetComponent<IPoolable>(out _))
                PoolManager.Return(obj);
            else
                Destroy(obj);
        }
    }

    private void DamagePlayer(Vector2 origin, Vector2 dir)
    {
        int count = Physics2D.RaycastNonAlloc(origin, dir, hits, currentBeamLength, hitMask);
        isHittingTarget = false;
        for (int i = 0; i < count; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;
            isHittingTarget = true;
            if (col.CompareTag("Player") && col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(config.damagePerTick);
        }
    }

    private float CalculateLength(Vector3 origin, Vector3 dir)
    {
        if (cannonTarget == null) return config.beamLength;
        float dist = Vector3.Dot(cannonTarget.position - origin, dir);
        if (dist <= 0f) return config.beamLength;
        Vector3 closest = origin + dir * dist;
        if (Vector3.Distance(cannonTarget.position, closest) <= config.beamWidth * 2f)
            return Mathf.Min(dist + 0.3f, config.beamLength);
        return config.beamLength;
    }

    private Vector3 GetDirection(Vector3 origin)
    {
        if (cannonTarget != null)
            return (cannonTarget.position - origin).x >= 0f ? Vector3.right : Vector3.left;
        return boss.transform.localScale.x >= 0f ? Vector3.right : Vector3.left;
    }

    private IEnumerator RotateFirePoint()
    {
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            if (cannonTarget == null) break;
            Vector3 toTarget = cannonTarget.position - firePoint.position;
            float target = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float current = firePoint.eulerAngles.z;
            float next = Mathf.MoveTowardsAngle(current, target, config.rotationSpeed * Time.deltaTime);
            firePoint.rotation = Quaternion.Euler(0f, 0f, next);
            if (Mathf.Abs(Mathf.DeltaAngle(next, target)) < 1f)
            {
                firePoint.rotation = Quaternion.Euler(0f, 0f, target);
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void Shutdown()
    {
        if (laserVisual != null)
        {
            laserVisual.Deactivate();
            laserVisual.gameObject.SetActive(false);
        }
        if (firePoint != null && firePoint != boss.transform)
            firePoint.localRotation = Quaternion.identity;
        isRunning = false;
    }

    public override void OnStop() { StopAllCoroutines(); Shutdown(); }
    public override void OnCleanup()
    {
        OnStop();
        if (laserVisual != null) Destroy(laserVisual.gameObject);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}