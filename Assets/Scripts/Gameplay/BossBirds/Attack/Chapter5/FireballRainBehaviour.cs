// FireballRainBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballRainBehaviour : BaseAttackBehaviour
{
    private FireballRainConfig config;
    private Transform spawnPoint;
    private readonly List<GameObject> activeFireballs = new();

    public void SetConfig(FireballRainConfig cfg) => config = cfg;

    private void Start()
    {
        spawnPoint = boss.transform.Find(config.spawnPointName);
        if (spawnPoint == null)
            Debug.LogWarning($"[FireballRain] '{config.spawnPointName}' not found on boss — falling back to boss position.", boss);
    }

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(RainSequence());
    }

    private IEnumerator RainSequence()
    {
        if (config.fireballPrefab == null)
        {
            Debug.LogError("[FireballRain] fireballPrefab is null — assign a prefab on the config asset.", this);
            isRunning = false;
            NotifyAttackComplete();
            yield break;
        }

        float interval = 1f / Mathf.Max(0.01f, config.spawnRate);
        float elapsed = 0f;
        float nextSpawn = 0f;

        while (elapsed < duration)
        {
            if (elapsed >= nextSpawn)
            {
                SpawnFireball();
                nextSpawn += interval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isRunning = false;
        NotifyAttackComplete();
    }

    private void SpawnFireball()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : boss.transform.position;
        var obj = PoolManager.Get(config.fireballPrefab, pos);
        activeFireballs.Add(obj);

        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = config.gravityScale;
        rb.linearVelocity = new Vector2(0f, -Mathf.Abs(config.initialDownSpeed));
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.None;

        var fireball = obj.GetComponent<Fireball>() ?? obj.AddComponent<Fireball>();
        fireball.Launch(config.contactDamage, config.lifetime, config.impactVfxPrefab, OnFireballGone);
    }

    private void OnFireballGone(GameObject obj)
    {
        activeFireballs.Remove(obj);
        PoolManager.Return(obj);
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeFireballs.Count; i++)
            if (activeFireballs[i] != null) PoolManager.Return(activeFireballs[i]);
        activeFireballs.Clear();
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
