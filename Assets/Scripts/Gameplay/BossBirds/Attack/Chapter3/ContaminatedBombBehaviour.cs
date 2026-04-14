// ContaminatedBombBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using Gameplay.Player;
using UnityEngine;

public class ContaminatedBombBehaviour : BaseAttackBehaviour
{
    private ContaminatedBombConfig config;
    private readonly List<Transform> dropPoints = new();

    private GameObject activeBomb;
    private bool bombDestroyedEarly;
    private Vector3 bombDestroyedPosition;

    private class MiniBombData
    {
        public GameObject obj;
        public bool isAlive;
    }
    private readonly List<MiniBombData> activeMiniBombs = new();

    private static readonly Collider2D[] overlap = new Collider2D[4];
    private static int groundMask;
    private static bool maskInit;

    public void SetConfig(ContaminatedBombConfig cfg) => config = cfg;

    private void Awake()
    {
        if (!maskInit)
        {
            groundMask = LayerMask.GetMask("Ground");
            maskInit = true;
        }
    }

    private void Start()
    {
        var parent = boss.transform.Find(config.dropParentName);
        if (parent != null)
            for (int i = 0; i < parent.childCount; i++)
                dropPoints.Add(parent.GetChild(i));
    }

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        ShowWarning(config, () => StartCoroutine(BombSequence()));
    }

    private IEnumerator BombSequence()
    {
        bombDestroyedEarly = false;

        Vector3 dropPos = dropPoints.Count > 0
            ? dropPoints[Random.Range(0, dropPoints.Count)].position
            : boss.transform.position;

        activeBomb = PoolManager.Get(config.bombPrefab, dropPos);
        activeBomb.transform.rotation = Quaternion.identity;
        PlayParticles(activeBomb);

        var bombRb = activeBomb.GetComponent<Rigidbody2D>();
        if (bombRb != null)
        {
            bombRb.linearVelocity = Vector2.zero;
            bombRb.gravityScale = config.bombGravityScale;
        }

        var bombHealth = activeBomb.GetComponent<MiniRadioBombHealth>()
                      ?? activeBomb.AddComponent<MiniRadioBombHealth>();
        bombHealth.Init(config.bombHealth, config.bombContactDamage,
                        config.bombContactCooldown, OnOriginalBombKilled);

        yield return StartCoroutine(WaitForGroundHit(bombRb));

        Vector3 splitPos;

        if (bombDestroyedEarly)
        {
            splitPos = bombDestroyedPosition;
        }
        else
        {
            if (activeBomb == null) yield break;

            float groundY = activeBomb.transform.position.y;
            float dropHeight = dropPos.y - groundY;

            if (bombRb != null)
            {
                float bounceHeight = Mathf.Max(dropHeight * config.bounceHeightFraction, 0.5f);
                float gravity = Mathf.Abs(Physics2D.gravity.y) * bombRb.gravityScale;
                float bounceVY = Mathf.Sqrt(2f * gravity * bounceHeight);
                bombRb.linearVelocity = new Vector2(0f, bounceVY);
            }

            yield return StartCoroutine(WaitForApex(bombRb));

            if (bombDestroyedEarly)
            {
                splitPos = bombDestroyedPosition;
            }
            else
            {
                if (activeBomb == null) yield break;
                splitPos = activeBomb.transform.position;

                StopParticles(activeBomb);
                bombHealth.ResetState(config.bombHealth);
                PoolManager.Return(activeBomb);
                activeBomb = null;
            }
        }

        if (config.explosionVfxPrefab != null)
            PoolManager.ReturnDelayed(
                PoolManager.Get(config.explosionVfxPrefab, splitPos), 2f);

        // 🔥 UPDATED: Arc-based spawn
        SpawnMiniBombs(splitPos);

        float elapsed = 0f;
        while (elapsed < config.miniBombLifetime && HasLiveMiniBombs())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnMiniBombs();
        isRunning = false;
        NotifyAttackComplete();
    }

    // ─────────────────────────────────────────────────────────
    // 🔥 UPDATED ARC-BASED MINI BOMB SPAWN
    // ─────────────────────────────────────────────────────────
    // Replace the SpawnMiniBombs method in ContaminatedBombBehaviour.cs

    private void SpawnMiniBombs(Vector3 center)
    {
        int count = config.miniBombCount;
        if (count <= 0) return;

        float totalAngle = config.miniBombSpreadAngle;
        float startAngle = -totalAngle / 2f;
        float step = (count > 1) ? totalAngle / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            var obj = PoolManager.Get(config.miniBombPrefab, center);
            obj.transform.rotation = Quaternion.identity;
            PlayParticles(obj);

            float angle = startAngle + (step * i);
            angle += Random.Range(-5f, 5f);
            float rad = angle * Mathf.Deg2Rad;

            float t = (count > 1) ? Mathf.Abs(i - (count - 1) / 2f) / ((count - 1) / 2f) : 0f;
            float speed = Mathf.Lerp(config.miniBombLaunchSpeed, config.miniBombLaunchSpeed * 0.7f, t);

            Vector2 velocity = new Vector2(
                Mathf.Sin(rad) * speed,
                Mathf.Cos(rad) * speed
            );

            var rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Reset everything from previous pool use
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.None;

                // Critical: these three settings prevent ground tunneling
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.gravityScale = config.miniBombGravityScale;

                // Apply launch velocity
                rb.linearVelocity = velocity;
            }

            // Ensure collider exists and is NOT a trigger
            var col = obj.GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = false;
                col.enabled = true;
            }
            else
            {
                Debug.LogError($"[ContaminatedBomb] miniBombPrefab has no Collider2D! " +
                               $"It will fall through ground.", obj);
            }

            var jumper = obj.GetComponent<MiniBombJumper>()
                      ?? obj.AddComponent<MiniBombJumper>();
            jumper.Init(config.miniBombJumpForce);

            var health = obj.GetComponent<MiniRadioBombHealth>()
                      ?? obj.AddComponent<MiniRadioBombHealth>();
            health.Init(config.miniBombHealth, config.miniBombContactDamage,
                        config.miniBombContactCooldown, OnMiniBombKilled);

            activeMiniBombs.Add(new MiniBombData { obj = obj, isAlive = true });
        }
    }

    // ─────────────────────────────────────────────────────────

    private void OnOriginalBombKilled(GameObject deadObj)
    {
        bombDestroyedEarly = true;
        bombDestroyedPosition = deadObj.transform.position;
        StopParticles(deadObj);

        if (deadObj.TryGetComponent<MiniRadioBombHealth>(out var h))
            h.ResetState(config.bombHealth);

        PoolManager.Return(deadObj);
        activeBomb = null;
    }

    private IEnumerator WaitForGroundHit(Rigidbody2D rb)
    {
        float timeout = 6f;
        float timer = 0f;
        yield return null;

        while (timer < timeout)
        {
            if (bombDestroyedEarly) yield break;
            if (activeBomb == null) yield break;

            if (rb == null || rb.linearVelocity.y <= 0f)
            {
                int count = Physics2D.OverlapCircleNonAlloc(
                    activeBomb.transform.position, 0.3f, overlap, groundMask);
                if (count > 0) yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForApex(Rigidbody2D rb)
    {
        float safety = 0f;
        while (safety < 0.1f) { safety += Time.deltaTime; yield return null; }

        while (!bombDestroyedEarly && activeBomb != null &&
               rb != null && rb.linearVelocity.y > 0.1f)
            yield return null;
    }

    private void OnMiniBombKilled(GameObject deadObj)
    {
        foreach (var data in activeMiniBombs)
            if (data.obj == deadObj) { data.isAlive = false; break; }

        StopParticles(deadObj);

        if (config.explosionVfxPrefab != null)
            PoolManager.ReturnDelayed(
                PoolManager.Get(config.explosionVfxPrefab, deadObj.transform.position), 1.5f);

        if (deadObj.TryGetComponent<MiniRadioBombHealth>(out var h))
            h.ResetState(config.miniBombHealth);

        if (deadObj.TryGetComponent<MiniBombJumper>(out var j))
            j.ResetState();

        PoolManager.Return(deadObj);
    }

    private bool HasLiveMiniBombs()
    {
        foreach (var d in activeMiniBombs)
            if (d.isAlive && d.obj != null) return true;
        return false;
    }

    private static void PlayParticles(GameObject obj)
    {
        if (obj == null) return;
        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
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

    private void ReturnMiniBombs()
    {
        foreach (var data in activeMiniBombs)
        {
            if (!data.isAlive || data.obj == null) continue;

            if (data.obj.TryGetComponent<MiniRadioBombHealth>(out var h))
                h.ResetState(config.miniBombHealth);

            if (data.obj.TryGetComponent<MiniBombJumper>(out var j))
                j.ResetState();

            StopParticles(data.obj);
            PoolManager.Return(data.obj);
        }
        activeMiniBombs.Clear();
    }

    public override void OnStop()
    {
        StopAllCoroutines();

        if (activeBomb != null)
        { 
            StopParticles(activeBomb);

            if (activeBomb.TryGetComponent<MiniRadioBombHealth>(out var h))
                h.ResetState(config.bombHealth);

            PoolManager.Return(activeBomb);
            activeBomb = null;
        }

        ReturnMiniBombs();
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnCleanup() => OnStop();
}