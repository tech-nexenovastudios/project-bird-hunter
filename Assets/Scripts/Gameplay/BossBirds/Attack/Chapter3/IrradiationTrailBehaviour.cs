// IrradiationTrailBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using Gameplay.Interfaces;
using UnityEngine;

public class IrradiationTrailBehaviour : BaseAttackBehaviour
{
    private IrradiationTrailConfig config;

    private class CloudData
    {
        public GameObject obj;
        public float radius;
        public float lifetime;
        public float tickTimer;
    }

    private readonly List<CloudData> activeClouds = new();

    private static readonly Collider2D[] overlap = new Collider2D[16];
    private static int playerMask;
    private static bool maskInit;

    public void SetConfig(IrradiationTrailConfig cfg) => config = cfg;

    private void Awake()
    {
        if (!maskInit)
        {
            playerMask = LayerMask.GetMask("Player", "Cannon");
            maskInit = true;
        }
    }

    protected override void OnExecute()
    {
        StartCoroutine(TrailSequence());
    }

    private IEnumerator TrailSequence()
    {
        float elapsed = 0f;
        float nextSpawn = 0f;

        while (elapsed < config.duration)
        {
            // Spawn cloud at boss's current position (boss is flying erratically via movement handler)
            if (elapsed >= nextSpawn && activeClouds.Count < config.maxCloudsActive)
            {
                SpawnCloud();
                nextSpawn += config.spawnInterval;
            }

            UpdateClouds();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Wait for all clouds to expire
        while (activeClouds.Count > 0)
        {
            UpdateClouds();
            yield return null;
        }

        isRunning = false;
    }

    private void SpawnCloud()
    {
        var obj = PoolManager.Get(config.radiationCloudPrefab, boss.transform.position);
        obj.transform.localScale = Vector3.one * config.cloudStartRadius;

        activeClouds.Add(new CloudData
        {
            obj = obj,
            radius = config.cloudStartRadius,
            lifetime = 0f,
            tickTimer = 0f
        });
    }

    private void UpdateClouds()
    {
        float tickInterval = 1f / config.cloudTicksPerSecond;

        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            var cloud = activeClouds[i];
            if (cloud.obj == null) { activeClouds.RemoveAt(i); continue; }

            cloud.lifetime += Time.deltaTime;

            // Shrink over time
            cloud.radius -= config.cloudShrinkSpeed * Time.deltaTime;
            if (cloud.radius < config.cloudMinRadius)
                cloud.radius = config.cloudMinRadius;

            cloud.obj.transform.localScale = Vector3.one * cloud.radius;

            // Expired
            if (cloud.lifetime >= config.cloudLifetime)
            {
                PoolManager.Return(cloud.obj);
                activeClouds.RemoveAt(i);
                continue;
            }

            // Damage tick
            cloud.tickTimer += Time.deltaTime;
            if (cloud.tickTimer >= tickInterval)
            {
                cloud.tickTimer -= tickInterval;
                DamageInCloud(cloud.obj.transform.position, cloud.radius * 0.5f);
            }
        }
    }

    private void DamageInCloud(Vector2 center, float radius)
    {
        int count = Physics2D.OverlapCircleNonAlloc(center, radius, overlap, playerMask);
        for (int i = 0; i < count; i++)
        {
            var col = overlap[i];
            if (col != null && col.CompareTag("Player") && col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(config.cloudDamagePerTick);
        }
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeClouds.Count; i++)
            if (activeClouds[i].obj != null) PoolManager.Return(activeClouds[i].obj);
        activeClouds.Clear();
    }

    public override void OnStop() { StopAllCoroutines(); ReturnAll(); isRunning = false; }
    public override void OnCleanup() => OnStop();
}