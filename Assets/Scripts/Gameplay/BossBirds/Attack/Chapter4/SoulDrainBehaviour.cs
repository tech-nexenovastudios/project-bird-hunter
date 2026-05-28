// SoulDrainBehaviour.cs
// Beam attack copied from StoneGazeSweepBehaviour (Ch1 boss): a laser fired from the boss that
// locks onto the cannon, then tracks it at a capped rotation speed (the dodgeable delay).
// Soul Drain keeps its speed-debuff: while the beam is on the cannon, the cannon is slowed.
using System.Collections;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

public class SoulDrainBehaviour : BaseAttackBehaviour
{
    private SoulDrainConfig config;
    private Transform firePoint;
    private bool ownsFirePoint;
    private LaserBeamVisual laserVisual;
    private Transform cannonTarget;
    private BaseCannon cannonComponent;
    private Vector3 lockedDirection;
    private float currentBeamLength;
    private float damageTickInterval;
    private float damageTickTimer;
    private bool isHittingTarget;
    private bool debuffApplied;

    private static readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private static int hitMask;
    private static bool maskInitialized;

    public void SetConfig(SoulDrainConfig cfg) => config = cfg;

    private void Awake()
    {
        if (!maskInitialized)
        {
            hitMask = LayerMask.GetMask("Player", "Cannon");
            maskInitialized = true;
        }
    }

    private void Start()
    {
        ResolveFirePoint();

        // Setup laser visual — parented to the fire point so it rotates with the beam.
        if (config.tetherLinePrefab != null)
        {
            var obj = Instantiate(config.tetherLinePrefab, firePoint);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            laserVisual = obj.GetComponent<LaserBeamVisual>();
            if (laserVisual != null)
            {
                laserVisual.SetColors(config.coreColor, config.glowColor, config.haloColor);
                laserVisual.Deactivate();
            }
            obj.SetActive(false);
        }

        damageTickInterval = 1f / Mathf.Max(config.ticksPerSecond, 0.1f);
    }

    // Rotate a fire point, never the boss. Use the named child if present; otherwise spawn a
    // dedicated pivot at the boss origin so RotateFirePoint/TrackPlayer can't spin the boss art.
    private void ResolveFirePoint()
    {
        if (!string.IsNullOrEmpty(config.firePointName))
            firePoint = boss.transform.Find(config.firePointName);

        if (firePoint == null)
        {
            var go = new GameObject("SoulDrainFirePoint");
            go.transform.SetParent(boss.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            firePoint = go.transform;
            ownsFirePoint = true;
        }
    }

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(SweepSequence());
    }

    private IEnumerator SweepSequence()
    {
        if (firePoint == null) ResolveFirePoint();

        cannonTarget = GameObject.FindWithTag("Player")?.transform;
        cannonComponent = cannonTarget != null ? cannonTarget.GetComponent<BaseCannon>() : null;

        Vector3 origin = firePoint.position;
        lockedDirection = GetDirection(origin);

        // Rotate fire point toward the cannon before firing.
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
        TrackPlayer();

        Vector3 origin = firePoint.position;
        currentBeamLength = CalculateLength(origin, lockedDirection);
        Vector3 end = origin + lockedDirection * currentBeamLength;

        laserVisual?.SetPositions(origin, end);

        // Damage ticks
        damageTickTimer += Time.deltaTime;
        if (damageTickTimer >= damageTickInterval)
        {
            damageTickTimer -= damageTickInterval;
            DamagePlayer(origin, lockedDirection);
        }

        // Soul Drain: slow the cannon while the beam is on it, restore when it slips out.
        ApplyDebuff(isHittingTarget);

        // Impact FX
        if (laserVisual != null)
        {
            if (isHittingTarget)
                laserVisual.ShowImpact(end, lockedDirection);
            else
                laserVisual.HideImpact();
        }
    }

    private void TrackPlayer()
    {
        if (cannonTarget == null) return;
        Vector3 toTarget = cannonTarget.position - firePoint.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float current = firePoint.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, config.trackingRotationSpeed * Time.deltaTime);
        firePoint.rotation = Quaternion.Euler(0f, 0f, next);
        lockedDirection = firePoint.right;
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

    private void ApplyDebuff(bool active)
    {
        if (cannonComponent == null) return;
        if (active && !debuffApplied)
        {
            debuffApplied = true;
            cannonComponent.ApplySpeedMultiplier(config.speedDebuffMultiplier);
        }
        else if (!active && debuffApplied)
        {
            debuffApplied = false;
            cannonComponent.ApplySpeedMultiplier(1f);
        }
    }

    private float CalculateLength(Vector3 origin, Vector3 dir)
    {
        if (cannonTarget == null) return config.beamLength;
        float dist = Vector3.Dot(cannonTarget.position - origin, dir);
        if (dist <= 0f) return config.beamLength;
        Vector3 closest = origin + dir * dist;
        if (Vector3.Distance(cannonTarget.position, closest) <= config.tetherWidth * 2f)
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
        ApplyDebuff(false);

        if (laserVisual != null)
        {
            laserVisual.Deactivate();
            laserVisual.gameObject.SetActive(false);
        }
        if (firePoint != null)
            firePoint.localRotation = Quaternion.identity;

        isHittingTarget = false;
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnStop()
    {
        StopAllCoroutines();
        Shutdown();
    }

    public override void OnCleanup()
    {
        OnStop();
        if (laserVisual != null) Destroy(laserVisual.gameObject);
        if (ownsFirePoint && firePoint != null) Destroy(firePoint.gameObject);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
