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

    // Locked once at attack start
    private Vector3 cannonPosition;
    private Collider2D cannonCollider;
    private BaseCannon cannonComponent;
    private bool hasTarget;

    private class RingData
    {
        public GameObject obj;
        public float age;
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
    }

    protected override void OnExecute()
    {
        // Lock cannon reference ONCE
        var cannonObj = GameObject.FindWithTag("Player");
        if (cannonObj != null)
        {
            cannonPosition = cannonObj.transform.position;
            cannonCollider = cannonObj.GetComponent<Collider2D>();
            cannonComponent = cannonObj.GetComponent<BaseCannon>();
            hasTarget = cannonCollider != null && cannonComponent != null;

            if (!hasTarget)
                Debug.LogWarning("[AuraOfDamnation] Player found but missing Collider2D or BaseCannon!");
        }
        else
        {
            hasTarget = false;
            Debug.LogWarning("[AuraOfDamnation] No Player found!");
        }

        NotifyAttackStarted();
        ShowWarning(config, () => StartCoroutine(EmitRings()));
    }

    private IEnumerator EmitRings()
    {
        float elapsed = 0f;
        float nextSpawn = 0f;

        while (elapsed < duration)
        {
            if (elapsed >= nextSpawn)
            {
                SpawnRing();
                nextSpawn += config.ringSpawnInterval;
            }

            UpdateRings();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Keep updating until all rings expire naturally
        while (activeRings.Count > 0)
        {
            UpdateRings();
            yield return null;
        }

        ReturnAll();
        isRunning = false;
        NotifyAttackComplete();
    }

    private void SpawnRing()
    {
        Vector3 spawnPos = ringSpawnPoint != null
            ? ringSpawnPoint.position
            : boss.transform.position;

        var obj = PoolManager.Get(config.fireRingPrefab, spawnPos);
        obj.transform.rotation = Quaternion.identity;

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

        if (hasTarget)
        {
            float distance = Vector3.Distance(spawnPos, cannonPosition);
            float travelTime = Mathf.Max(config.ringLifetime - 1f, 0.5f);
            speed = distance / travelTime;
            dir = (cannonPosition - spawnPos).normalized;
        }

        activeRings.Add(new RingData
        {
            obj = obj,
            age = 0f,
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

            if (ring.age >= config.ringLifetime)
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
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnCleanup() => OnStop();
}