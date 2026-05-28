// AuraOfDamnationBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Player;

public class AuraOfDamnationBehaviour : BaseAttackBehaviour
{
    private AuraOfDamnationConfig config;
    private readonly List<RingData> activeRings = new();
    private static readonly Collider2D[] overlap = new Collider2D[32];
    private static int bulletMask;
    private static int playerMask;
    private static bool maskInit;
    private Transform ringSpawnPoint;

    // The prefab's authored root scale, captured once. Each spawn sets the pooled ring's scale to
    // baseScale * config.ringScale — multiplying the live scale instead would compound across pool reuse.
    private Vector3 ringBaseScale = Vector3.one;

    // Locked after the aim rotation; rings travel toward this point.
    private Vector3 cannonPosition;
    private Transform cannonTransform;
    private Collider2D cannonCollider;
    private BaseCannon cannonComponent;
    private bool hasTarget;

    private class RingData
    {
        public GameObject obj;
        public float age;
        public float lifetime;
        public bool hasHitCannon;
        public Vector3 direction;
        public float speed;
    }

    public void SetConfig(AuraOfDamnationConfig cfg) => config = cfg;

    private void Start()
    {
        if (!maskInit)
        {
            bulletMask = LayerMask.GetMask("Bullet");
            playerMask = LayerMask.GetMask("Player");
            maskInit = true;
        }

        ringSpawnPoint = boss.transform.Find("RingSpawnPosition");
        if (ringSpawnPoint == null)
            Debug.LogWarning("[AuraOfDamnation] 'RingSpawnPosition' not found on boss!", boss);

        if (config != null && config.fireRingPrefab != null)
            ringBaseScale = config.fireRingPrefab.transform.localScale;
    }

    protected override void OnExecute()
    {
        // Lock cannon reference ONCE
        var cannonObj = GameObject.FindWithTag("Player");
        if (cannonObj != null)
        {
            cannonTransform = cannonObj.transform;
            cannonPosition = cannonTransform.position;
            cannonCollider = cannonObj.GetComponent<Collider2D>();
            cannonComponent = cannonObj.GetComponent<BaseCannon>();
            hasTarget = cannonCollider != null && cannonComponent != null;

            if (!hasTarget)
                Debug.LogWarning("[AuraOfDamnation] Player found but missing Collider2D or BaseCannon!");
        }
        else
        {
            hasTarget = false;
            cannonTransform = null;
            Debug.LogWarning("[AuraOfDamnation] No Player found!");
        }

        NotifyAttackStarted();
        ShowWarning(config, () => StartCoroutine(AttackRoutine()));
    }

    private IEnumerator AttackRoutine()
    {
        // Aim at the cannon first (rotation before shoot), then lock the position the rings fly toward.
        if (config.rotateBeforeShoot && hasTarget && ringSpawnPoint != null && cannonTransform != null)
            yield return StartCoroutine(RotateTowardCannon());

        // Lock the target position AFTER aiming so rings travel toward where the boss settled on.
        if (hasTarget && cannonTransform != null)
            cannonPosition = cannonTransform.position;

        yield return StartCoroutine(EmitRings());

        // Reset the aim pivot so the next cast starts clean.
        if (ringSpawnPoint != null)
            ringSpawnPoint.localRotation = Quaternion.identity;
    }

    // Rotate the ring spawn point toward the cannon at a capped speed, then stop once aligned —
    // the same lock-on telegraph Stone Gaze Sweep does before firing.
    private IEnumerator RotateTowardCannon()
    {
        float elapsed = 0f;
        while (elapsed < config.aimMaxDuration)
        {
            if (cannonTransform == null) break;

            Vector3 toTarget = cannonTransform.position - ringSpawnPoint.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float target = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                float current = ringSpawnPoint.eulerAngles.z;
                float next = Mathf.MoveTowardsAngle(current, target, config.rotationSpeed * Time.deltaTime);
                ringSpawnPoint.rotation = Quaternion.Euler(0f, 0f, next);

                if (Mathf.Abs(Mathf.DeltaAngle(next, target)) < 1f)
                {
                    ringSpawnPoint.rotation = Quaternion.Euler(0f, 0f, target);
                    break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator EmitRings()
    {
        // Continuous stream: throw one ring every ringSpawnInterval with no cooldown gap.
        // Runs until the attack is stopped (phase change / retreat / death route through
        // OnStop, which StopAllCoroutines + ReturnAll). Rings overlap — a new one is thrown
        // on schedule whether or not earlier rings are still alive, and UpdateRings advances
        // every entry in activeRings each frame. 'duration' is intentionally ignored here.
        float elapsed = 0f;
        float nextSpawn = 0f;

        while (true)
        {
            if (elapsed >= nextSpawn)
            {
                SpawnRing();
                nextSpawn += Mathf.Max(0.01f, config.ringSpawnInterval);
            }

            UpdateRings();
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SpawnRing()
    {
        Vector3 spawnPos = ringSpawnPoint != null
            ? ringSpawnPoint.position
            : boss.transform.position;

        var obj = PoolManager.Get(config.fireRingPrefab, spawnPos);
        obj.transform.rotation = Quaternion.identity;

        // Size the ring (and its nested child rings) from the serialized radius multiplier.
        obj.transform.localScale = ringBaseScale * Mathf.Max(0.01f, config.ringScale);

        // Force Local simulation so particles travel with the moving ring object
        var allPS = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allPS)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        Vector3 dir = Vector3.zero;
        float speed = 0f;
        float lifetime = config.ringLifetime;

        // Re-acquire the cannon if it was never found (or was destroyed and respawned).
        // This is a continuous stream, so we can't rely on the single lookup done at OnExecute.
        if (cannonTransform == null)
        {
            var cannonObj = GameObject.FindWithTag("Player");
            if (cannonObj != null)
            {
                cannonTransform = cannonObj.transform;
                cannonComponent = cannonObj.GetComponent<BaseCannon>();
                hasTarget = cannonComponent != null;
            }
        }

        if (cannonTransform != null)
        {
            // Aim THIS ring at the cannon's live position (it and the boss both move), and
            // throw it at the configured speed.
            Vector3 target = cannonTransform.position;
            speed = config.ringSpeed;
            dir = (target - spawnPos).normalized;

            // Guarantee the ring lives long enough to actually reach the cannon at this
            // speed — a fixed ringSpeed + fixed ringLifetime would otherwise let the ring
            // expire mid-flight when the boss is farther than ringSpeed * ringLifetime.
            if (speed > 0.01f)
            {
                float travelTime = Vector3.Distance(spawnPos, target) / speed;
                lifetime = Mathf.Max(config.ringLifetime, travelTime + 0.5f);
            }
        }

        activeRings.Add(new RingData
        {
            obj = obj,
            age = 0f,
            lifetime = lifetime,
            hasHitCannon = false,
            direction = dir,
            speed = speed
        });
    }

    private void UpdateRings()
    {
        float dt = Time.deltaTime;

        for (int i = activeRings.Count - 1; i >= 0; i--)
        {
            var ring = activeRings[i];
            if (ring.obj == null)
            {
                activeRings.RemoveAt(i);
                continue;
            }

            ring.age += dt;

            if (ring.age >= ring.lifetime)
            {
                ReturnRing(ring);
                activeRings.RemoveAt(i);
                continue;
            }

            if (ring.direction != Vector3.zero)
                ring.obj.transform.position += ring.direction * ring.speed * dt;

            CheckCollisions(ring, i);
        }
    }

    private void CheckCollisions(RingData ring, int index)
    {
        Vector2 ringPos = ring.obj.transform.position;

        // Collision radius grows over the ring's lifetime, per config.
        float t = config.ringLifetime > 0f ? Mathf.Clamp01(ring.age / config.ringLifetime) : 1f;
        float radius = Mathf.Lerp(config.collisionRadiusStart, config.collisionRadiusEnd, t);

        // Destroy any player bullets that fall inside the ring radius.
        int count = Physics2D.OverlapCircleNonAlloc(ringPos, radius, overlap, bulletMask);
        for (int j = 0; j < count; j++)
        {
            if (overlap[j] != null && overlap[j].TryGetComponent<BaseBullet>(out var bullet))
                bullet.gameObject.SetActive(false);
        }

        // Damage cannon when it enters the ring radius (live position, not the locked
        // target the ring is travelling toward).
        if (!ring.hasHitCannon && hasTarget)
        {
            var hit = Physics2D.OverlapCircle(ringPos, radius, playerMask);
            if (hit != null && hit.TryGetComponent<BaseCannon>(out var cannon))
            {
                ring.hasHitCannon = true;
                cannon.TakeDamage(Mathf.RoundToInt(config.damage));

                // Play the destroy VFX at the cannon, then remove the ring there.
                if (config.destroyVfxPrefab != null)
                {
                    var vfx = PoolManager.Get(config.destroyVfxPrefab, hit.transform.position);
                    PoolManager.ReturnDelayed(vfx, config.destroyVfxLifetime);
                }

                ReturnRing(ring);
                activeRings.RemoveAt(index);
            }
        }
    }

    private void ReturnRing(RingData ring)
    {
        if (ring.obj == null) return;

        var allPS = ring.obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allPS)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        PoolManager.Return(ring.obj);
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeRings.Count; i++)
            ReturnRing(activeRings[i]);
        activeRings.Clear();
    }

    public override void OnStop()
    {
        StopAllCoroutines();
        ReturnAll();
        if (ringSpawnPoint != null)
            ringSpawnPoint.localRotation = Quaternion.identity;
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnCleanup() => OnStop();
}