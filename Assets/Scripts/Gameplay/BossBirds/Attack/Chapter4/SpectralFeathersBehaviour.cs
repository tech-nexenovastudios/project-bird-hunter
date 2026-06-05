// SpectralFeathersBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using Gameplay.Interfaces;
using UnityEngine;

public class SpectralFeathersBehaviour : BaseAttackBehaviour
{
    private SpectralFeathersConfig config;
    private readonly List<FeatherData> activeFeathers = new();

    // Local copies so enrage can escalate without mutating the shared config asset.
    private int featherCount;
    private float spawnInterval;

    private static readonly Collider2D[] overlap = new Collider2D[4];
    private static int playerMask;
    private static bool maskInit;

    private class FeatherData
    {
        public GameObject obj;
        public SpriteRenderer sr;

        // Physics-like state (no Rigidbody — we simulate manually)
        public Vector2 velocity;
        public float angularVelocity;

        // Leaf flutter parameters (randomized per feather)
        public float dragCoefficient;
        public float liftStrength;
        public float tumbleRate;
        public float tumblePhase;
        public float mass;

        // Tracking
        public float age;
        public float lastDamageTime;
    }

    public void SetConfig(SpectralFeathersConfig cfg)
    {
        config = cfg;
        featherCount = cfg.featherCount;
        spawnInterval = cfg.spawnInterval;
    }

    // Feather Storm: denser, faster volleys once the boss enrages.
    public override void OnEnrage()
    {
        featherCount += config.enrageFeatherBonus;
        spawnInterval *= config.enrageSpawnIntervalMultiplier;
    }

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
        StartCoroutine(FeatherSequence());
    }

    private IEnumerator FeatherSequence()
    {
        for (int i = 0; i < featherCount; i++)
        {
            SpawnFeather();
            if (i < featherCount - 1)
                yield return new WaitForSeconds(spawnInterval);
        }

        float elapsed = 0f;
        float maxLifetime = config.duration + 15f;

        while (elapsed < maxLifetime && activeFeathers.Count > 0)
        {
            UpdateFeathers();
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnAll();
        isRunning = false;
        NotifyAttackComplete();
    }

    private void SpawnFeather()
    {
        float halfWidth = config.spawnWidth * 0.5f;
        float x = boss.transform.position.x + Random.Range(-halfWidth, halfWidth);
        float y = boss.transform.position.y + config.spawnYOffset;

        Vector3 spawnPos = new Vector3(x, y, 0f);
        var obj = PoolManager.Get(config.featherPrefab, spawnPos);

        // Random initial rotation
        obj.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Random.Range(0.5f, 0.8f);
            sr.color = c;
        }

        // Each feather gets slightly different physics so they all fall differently
        activeFeathers.Add(new FeatherData
        {
            obj = obj,
            sr = sr,
            velocity = new Vector2(Random.Range(-0.5f, 0.5f), 0f), // Slight initial horizontal drift
            angularVelocity = Random.Range(-40f, 40f),
            dragCoefficient = Random.Range(config.dragMin, config.dragMax),
            liftStrength = Random.Range(config.liftMin, config.liftMax),
            tumbleRate = Random.Range(config.tumbleRateMin, config.tumbleRateMax),
            tumblePhase = Random.Range(0f, Mathf.PI * 2f),
            mass = Random.Range(0.8f, 1.2f),
            age = 0f,
            lastDamageTime = -999f
        });
    }

    private void UpdateFeathers()
    {
        float dt = Time.deltaTime;
        float currentTime = Time.time;
        float gravity = -config.fallSpeed; // Downward force

        for (int i = activeFeathers.Count - 1; i >= 0; i--)
        {
            var f = activeFeathers[i];
            if (f.obj == null)
            {
                activeFeathers.RemoveAt(i);
                continue;
            }

            f.age += dt;

            // ── Forces ──

            // 1. Gravity pulls down
            float gravityForce = gravity * f.mass;

            // 2. Air drag opposes velocity (slows the feather, prevents terminal velocity from being too fast)
            Vector2 drag = -f.velocity.normalized * f.velocity.sqrMagnitude * f.dragCoefficient;

            // 3. Lift — the key to leaf-like motion
            //    As the feather tumbles, its "angle of attack" changes,
            //    creating a sideways lift force perpendicular to velocity
            float tumbleAngle = Mathf.Sin(f.age * f.tumbleRate + f.tumblePhase);

            //    Lift direction is perpendicular to current velocity (rotated 90 degrees)
            //    The tumble angle determines which side the lift pushes toward
            Vector2 velocityPerp = new Vector2(-f.velocity.y, f.velocity.x);
            if (velocityPerp.sqrMagnitude > 0.001f)
                velocityPerp.Normalize();

            Vector2 lift = velocityPerp * tumbleAngle * f.liftStrength * Mathf.Abs(f.velocity.x + f.velocity.y * 0.3f);

            // 4. Apply forces
            Vector2 acceleration = new Vector2(
                (drag.x + lift.x) / f.mass,
                (gravityForce + drag.y + lift.y) / f.mass
            );

            f.velocity += acceleration * dt;

            // Clamp so feather doesn't fly upward or go crazy
            f.velocity.y = Mathf.Min(f.velocity.y, 0.5f); // Can't rise much
            f.velocity.x = Mathf.Clamp(f.velocity.x, -config.maxHorizontalSpeed, config.maxHorizontalSpeed);

            // ── Position ──
            Vector3 pos = f.obj.transform.position;
            pos.x += f.velocity.x * dt;
            pos.y += f.velocity.y * dt;
            f.obj.transform.position = pos;

            // ── Rotation — feather tilts in the direction it's drifting ──
            //    The rotation follows the velocity direction with some angular momentum
            float targetAngle = Mathf.Atan2(f.velocity.y, f.velocity.x) * Mathf.Rad2Deg - 90f;

            // Smooth rotation — feather doesn't snap, it tumbles
            float currentZ = f.obj.transform.eulerAngles.z;
            float angleDiff = Mathf.DeltaAngle(currentZ, targetAngle);

            // Angular velocity blends between following velocity direction and free spin
            f.angularVelocity += angleDiff * 2f * dt; // Gently pulled toward velocity direction
            f.angularVelocity *= 0.97f;               // Angular drag — prevents infinite spin

            f.obj.transform.Rotate(0f, 0f, f.angularVelocity * dt);

            // ── Fade near bottom ──
            if (f.sr != null && pos.y < config.destroyBelowY + 3f)
            {
                float alpha = Mathf.Clamp01((pos.y - config.destroyBelowY) / 3f) * 0.7f;
                Color c = f.sr.color;
                c.a = alpha;
                f.sr.color = c;
            }

            // ── Remove below screen ──
            if (pos.y < config.destroyBelowY)
            {
                ReturnFeather(f);
                activeFeathers.RemoveAt(i);
                continue;
            }

            // ── Damage ──
            if (currentTime - f.lastDamageTime >= config.damageCooldown)
            {
                if (CheckHitCannon(f, currentTime))   // true = hit a cannon, destroy feather
                {
                    ReturnFeather(f);
                    activeFeathers.RemoveAt(i);
                    continue;
                }
            }
        }
    }

    // Returns true if the feather hit a cannon and should be destroyed
    private bool CheckHitCannon(FeatherData feather, float currentTime)
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            feather.obj.transform.position, 0.4f, overlap, playerMask);

        for (int j = 0; j < count; j++)
        {
            var col = overlap[j];
            if (col == null) continue;

            if (col.CompareTag("Player") && col.TryGetComponent<IDamageable>(out var player))
            {
                player.TakeDamage(config.contactDamage);
                feather.lastDamageTime = currentTime;
                return false;   // Hit player — keep feather alive
            }

            if (col.CompareTag("Cannon") && col.TryGetComponent<IDamageable>(out var cannon))
            {
                cannon.TakeDamage(config.contactDamage);
                return true;    // Hit cannon — destroy feather
            }
        }

        return false;
    }

    private void ReturnFeather(FeatherData f)
    {
        if (f.obj == null) return;

        if (f.sr != null)
        {
            Color c = f.sr.color;
            c.a = 1f;
            f.sr.color = c;
        }

        foreach (var ps in f.obj.GetComponentsInChildren<ParticleSystem>(true))
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        PoolManager.Return(f.obj);
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeFeathers.Count; i++)
            ReturnFeather(activeFeathers[i]);
        activeFeathers.Clear();
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