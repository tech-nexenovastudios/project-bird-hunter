// DroneSwarmBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using Gameplay.Interfaces;
using UnityEngine;

public class DroneSwarmBehaviour : BaseAttackBehaviour
{
    private DroneSwarmConfig config;
    private readonly List<DroneData> activeDrones = new();
    private Transform cannonTarget;

    private static readonly Collider2D[] overlap = new Collider2D[4];
    private static int playerMask;
    private static bool maskInit;

    private class DroneData
    {
        public GameObject obj;
        public Rigidbody2D rb;
        public float hoverPhase;
        public float lastDamageTime;
        public bool isAlive;
        public DroneHealth healthComp;
    }

    public void SetConfig(DroneSwarmConfig cfg) => config = cfg;

    private void Start()
    {
        if (!maskInit)
        {
            playerMask = LayerMask.GetMask("Player", "Cannon");
            maskInit = true;
        }
    }

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(SwarmSequence());
    }

    private IEnumerator SwarmSequence()
    {
        cannonTarget = FindCannon();

        // Spawn drones
        for (int i = 0; i < config.droneCount; i++)
        {
            SpawnDrone(i);
            if (i < config.droneCount - 1)
                yield return new WaitForSeconds(config.spawnInterval);
        }

        // Update drones until all dead or duration expires
        float elapsed = 0f;
        while (elapsed < duration && HasAliveDrones())
        {
            // Refresh cannon target in case it was destroyed and respawned
            if (cannonTarget == null)
                cannonTarget = FindCannon();

            UpdateDrones();
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnAll();
        isRunning = false;
        NotifyAttackComplete();
    }

    private void SpawnDrone(int index)
    {
        // Spawn position — spread around boss in a circle
        float angle = (360f / config.droneCount) * index * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * config.spawnRadius;
        Vector3 spawnPos = boss.transform.position + offset;

        var obj = PoolManager.Get(config.dronePrefab, spawnPos);

        // Scale relative to boss's CURRENT scale
        float bossScale = boss.transform.localScale.x;
        float droneScale = Mathf.Abs(bossScale) * config.scaleRatio;
        obj.transform.localScale = Vector3.one * droneScale;
        obj.tag = "Bird";

        // Rigidbody setup — kinematic so we control movement manually
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = obj.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider must NOT be trigger — we need it for damage detection via overlap
        var col = obj.GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;

        // Health — 10% of boss max HP
        var healthHandler = obj.GetComponent<BossHealthHandler>();
        float bossMaxHP = healthHandler != null ? 0f : 0f; // We get boss HP from the controller

        // Use a dedicated lightweight drone health component
        var droneHealth = obj.GetComponent<DroneHealth>();
        if (droneHealth == null)
            droneHealth = obj.AddComponent<DroneHealth>();

        // Get boss max HP from the health handler on the boss itself
        var bossHealth = boss.GetComponent<BossHealthHandler>();
        float droneHP = 100f; // fallback
        if (bossHealth != null)
        {
            // Access current max HP via the initial config
            var bossConfig = boss.GetComponent<BossBirdController>();
            // Simpler: just use the ratio on the config's damage value as a base
            // Actually — we can read it from the boss health handler if we expose it
            // For now: use the BossBirdConfig directly
        }

        // Cleanest approach: read boss HP from the BossBirdConfig the controller was initialized with
        // We passed boss (BossBirdController) — let's get config from it
        // But config is private... so let's calculate from what we have in our own config
        // The behaviour knows the boss's BossHealthHandler — read maxHealth from it
        // BossHealthHandler doesn't expose maxHealth publicly, so we calculate:
        // drone HP = boss HP * healthRatio
        // We'll store boss max HP at spawn time

        float bossMaxHealth = GetBossMaxHealth();
        droneHP = bossMaxHealth * config.healthRatio;

        droneHealth.Initialize(droneHP, config.contactDamage, config.damageCooldown);

        // Subscribe to drone death
        var droneData = new DroneData
        {
            obj = obj,
            rb = rb,
            hoverPhase = Random.Range(0f, Mathf.PI * 2f),
            lastDamageTime = -999f,
            isAlive = true,
            healthComp = droneHealth
        };

        droneHealth.OnDroneKilled = () => OnDroneKilled(droneData);
        activeDrones.Add(droneData);

        // Play VFX
        PlayParticles(obj);
    }

    private float GetBossMaxHealth()
    {
        // Read from boss's health handler — we need max health exposed
        // Since BossHealthHandler doesn't expose it, we calculate from config
        // The boss controller has the config — but it's private
        // Simplest safe approach: deal damage of 0 and check the normalized value
        // OR just read from our BaseAttackConfig's damage field as a proxy
        // 
        // Best approach: just make boss max health accessible
        // For now, use reflection-free method — store it during Initialize
        return bossMaxHealthCached;
    }

    private float bossMaxHealthCached = 100f;

    // Override to cache boss max health when behaviour is initialized
    public new void Initialize(BossBirdController owner, float attackCooldown, float attackDuration)
    {
        base.Initialize(owner, attackCooldown, attackDuration);

        // Get boss max HP — we need to read it somehow
        // Easiest: add a public getter to BossHealthHandler
        // For now: estimate from the config
        var bossHealthHandler = owner.GetComponent<BossHealthHandler>();
        // We'll add a public MaxHealth property to BossHealthHandler
        if (bossHealthHandler != null)
            bossMaxHealthCached = bossHealthHandler.MaxHealth;
    }

    private void UpdateDrones()
    {
        float dt = Time.deltaTime;
        float currentTime = Time.time;

        for (int i = activeDrones.Count - 1; i >= 0; i--)
        {
            var drone = activeDrones[i];
            if (!drone.isAlive || drone.obj == null)
            {
                activeDrones.RemoveAt(i);
                continue;
            }

            // ── Movement: track toward cannon ──
            Vector2 targetPos;
            if (cannonTarget != null)
                targetPos = cannonTarget.position;
            else
                targetPos = drone.obj.transform.position; // Stay in place if no target

            Vector2 currentPos = drone.obj.transform.position;
            Vector2 toTarget = targetPos - currentPos;
            float distToTarget = toTarget.magnitude;

            // Smooth tracking — accelerates toward target, doesn't snap
            Vector2 desiredVelocity = Vector2.zero;
            if (distToTarget > 0.1f)
            {
                desiredVelocity = toTarget.normalized * config.moveSpeed;
            }

            // ── Separation: push away from other drones ──
            Vector2 separation = Vector2.zero;
            for (int j = 0; j < activeDrones.Count; j++)
            {
                if (i == j || !activeDrones[j].isAlive || activeDrones[j].obj == null)
                    continue;

                Vector2 otherPos = activeDrones[j].obj.transform.position;
                Vector2 diff = currentPos - otherPos;
                float dist = diff.magnitude;

                if (dist < config.separationRadius && dist > 0.01f)
                {
                    // Push away — stronger when closer
                    separation += diff.normalized * (config.separationForce / dist);
                }
            }

            // ── Combine forces ──
            Vector2 finalVelocity = desiredVelocity + separation;

            // Smooth interpolation — stiffness controls how quickly drone changes direction
            drone.rb.linearVelocity = Vector2.Lerp(
                drone.rb.linearVelocity,
                finalVelocity,
                config.trackingStiffness * dt
            );

            // ── Hover bob ──
            drone.hoverPhase += dt * config.hoverSpeed;
            float hoverOffset = Mathf.Sin(drone.hoverPhase) * config.hoverAmplitude;
            Vector3 pos = drone.obj.transform.position;
            pos.y += hoverOffset * dt;
            drone.obj.transform.position = pos;

            // ── Face movement direction ──
            if (drone.rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                float moveAngle = Mathf.Atan2(drone.rb.linearVelocity.y, drone.rb.linearVelocity.x) * Mathf.Rad2Deg;
                drone.obj.transform.rotation = Quaternion.Lerp(
                    drone.obj.transform.rotation,
                    Quaternion.Euler(0f, 0f, moveAngle),
                    5f * dt
                );
            }

            // ── Damage cannon on contact ──
            if (currentTime - drone.lastDamageTime >= config.damageCooldown)
            {
                if (CheckHitCannon(drone, currentTime))
                    drone.lastDamageTime = currentTime;
            }
        }
    }

    private bool CheckHitCannon(DroneData drone, float currentTime)
    {
        // Use the drone's collider radius for overlap check
        float checkRadius = drone.obj.transform.localScale.x * 0.5f;
        checkRadius = Mathf.Max(checkRadius, 0.3f); // Minimum check radius

        int count = Physics2D.OverlapCircleNonAlloc(
            drone.obj.transform.position, checkRadius, overlap, playerMask);

        for (int j = 0; j < count; j++)
        {
            var col = overlap[j];
            if (col == null) continue;

            if (col.CompareTag("Player") && col.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(config.contactDamage);
                return true;
            }
        }

        return false;
    }

    private void OnDroneKilled(DroneData drone)
    {
        drone.isAlive = false;

        if (drone.obj == null) return;

        // Death VFX
        if (config.deathVfxPrefab != null)
            PoolManager.ReturnDelayed(
                PoolManager.Get(config.deathVfxPrefab, drone.obj.transform.position), 2f);

        StopParticles(drone.obj);
        PoolManager.Return(drone.obj);
    }

    private bool HasAliveDrones()
    {
        for (int i = 0; i < activeDrones.Count; i++)
            if (activeDrones[i].isAlive && activeDrones[i].obj != null)
                return true;
        return false;
    }

    private Transform FindCannon()
    {
        var obj = GameObject.FindWithTag("Player");
        return obj != null ? obj.transform : null;
    }

    private static void PlayParticles(GameObject obj)
    {
        if (obj == null) return;
        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private static void StopParticles(GameObject obj)
    {
        if (obj == null) return;
        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeDrones.Count; i++)
        {
            var drone = activeDrones[i];
            if (drone.obj != null)
            {
                StopParticles(drone.obj);
                PoolManager.Return(drone.obj);
            }
        }
        activeDrones.Clear();
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