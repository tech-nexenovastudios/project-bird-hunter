// CriticalMassBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using Gameplay.Player;
using UnityEngine;

public class CriticalMassBehaviour : BaseAttackBehaviour
{
    private CriticalMassConfig config;

    // Tracks every live fragment independently
    private class FragmentData
    {
        public GameObject fragmentObj;   // glow prefab (warning phase)
        public Vector3 spawnPosition;
        public float warningTimer;
        public bool hasExploded;
    }

    private readonly List<FragmentData> activeFragments = new();
    // Explosion VFX objects just linger then get returned — tracked separately
    private readonly List<(GameObject obj, float timer)> activeExplosions = new();

    private static readonly Collider2D[] overlap = new Collider2D[16];
    private static int playerMask;
    private static bool maskInit;

    // Orbit state
    private float orbitAngle;

    public void SetConfig(CriticalMassConfig cfg) => config = cfg;

    private void Start()
    {
        if (!maskInit)
        {
            playerMask = LayerMask.GetMask("Player");
            maskInit = true;
        }
    }

    protected override void OnExecute()
    {
        // Start orbit angle from boss's current position so movement isn't jarring
        Vector2 offset = (Vector2)boss.transform.position - config.orbitCenter;
        orbitAngle = Mathf.Atan2(
            offset.y / Mathf.Max(config.orbitRadiusY, 0.01f),
            offset.x / Mathf.Max(config.orbitRadiusX, 0.01f));

        NotifyAttackStarted();
        ShowWarning(config, () => StartCoroutine(AttackSequence()));
    }

    private IEnumerator AttackSequence()
    {
        float elapsed = 0f;
        float nextDrop = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;

            OrbitBoss(dt);

            if (elapsed >= nextDrop)
            {
                DropFragment();
                nextDrop += config.dropInterval;
            }

            UpdateFragments(dt);
            UpdateExplosions(dt);

            elapsed += dt;
            yield return null;
        }

        // Let remaining fragments finish their warning + explode naturally
        while (activeFragments.Count > 0 || activeExplosions.Count > 0)
        {
            float dt = Time.deltaTime;
            UpdateFragments(dt);
            UpdateExplosions(dt);
            yield return null;
        }

        isRunning = false;
        NotifyAttackComplete();
    }

    // ─── Orbit ───────────────────────────────────────────────────────────────

    private void OrbitBoss(float dt)
    {
        orbitAngle += config.orbitSpeed * dt;

        float x = config.orbitCenter.x + Mathf.Cos(orbitAngle) * config.orbitRadiusX;
        float y = config.orbitCenter.y + Mathf.Sin(orbitAngle) * config.orbitRadiusY;
        boss.transform.position = new Vector3(x, y, boss.transform.position.z);
    }

    // ─── Fragment lifecycle ───────────────────────────────────────────────────

    private void DropFragment()
    {
        Vector3 pos = boss.transform.position;
        var obj = PoolManager.Get(config.fragmentPrefab, pos);

        // Keep fragment stationary — reset any leftover transform state
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        PlayParticles(obj);

        activeFragments.Add(new FragmentData
        {
            fragmentObj = obj,
            spawnPosition = pos,
            warningTimer = 0f,
            hasExploded = false
        });
    }

    private void UpdateFragments(float dt)
    {
        for (int i = activeFragments.Count - 1; i >= 0; i--)
        {
            var frag = activeFragments[i];
            if (frag.fragmentObj == null) { activeFragments.RemoveAt(i); continue; }

            frag.warningTimer += dt;

            if (!frag.hasExploded && frag.warningTimer >= config.warningDuration)
            {
                frag.hasExploded = true;
                Explode(frag);
                activeFragments.RemoveAt(i);
            }
        }
    }

    private void Explode(FragmentData frag)
    {
        // Stop warning VFX and return the glow prefab
        StopParticles(frag.fragmentObj);
        PoolManager.Return(frag.fragmentObj);

        // Spawn explosion VFX at the same world position
        if (config.explosionPrefab != null)
        {
            var exObj = PoolManager.Get(config.explosionPrefab, frag.spawnPosition);
            exObj.transform.rotation = Quaternion.identity;
            exObj.transform.localScale = Vector3.one * config.explosionRadius;
            PlayParticles(exObj);
            activeExplosions.Add((exObj, 0f));
        }

        // One-shot damage check at explosion position
        int count = Physics2D.OverlapCircleNonAlloc(
            frag.spawnPosition, config.explosionRadius, overlap, playerMask);

        for (int j = 0; j < count; j++)
        {
            var col = overlap[j];
            if (col != null &&
                col.CompareTag("Player") &&
                col.TryGetComponent<BaseCannon>(out var cannon))
            {
                cannon.TakeDamage(config.explosionDamage);
            }
        }
    }

    private void UpdateExplosions(float dt)
    {
        for (int i = activeExplosions.Count - 1; i >= 0; i--)
        {
            var (obj, timer) = activeExplosions[i];
            float newTimer = timer + dt;

            if (newTimer >= config.explosionLingerDuration)
            {
                StopParticles(obj);
                PoolManager.Return(obj);
                activeExplosions.RemoveAt(i);
            }
            else
            {
                activeExplosions[i] = (obj, newTimer);
            }
        }
    }

    // ─── VFX helpers ─────────────────────────────────────────────────────────

    private static void PlayParticles(GameObject obj)
    {
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

    // ─── Cleanup ─────────────────────────────────────────────────────────────

    private void ReturnAll()
    {
        foreach (var frag in activeFragments)
        {
            StopParticles(frag.fragmentObj);
            if (frag.fragmentObj != null) PoolManager.Return(frag.fragmentObj);
        }
        activeFragments.Clear();

        foreach (var (obj, _) in activeExplosions)
        {
            StopParticles(obj);
            if (obj != null) PoolManager.Return(obj);
        }
        activeExplosions.Clear();
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