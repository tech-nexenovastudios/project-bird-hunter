using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss-4 add: spawns a small flight of spectral wisps that home toward the cannon. Each wisp is
/// shootable (WispHealth) and kamikazes on contact. The volley ends when all wisps are gone or the
/// lifetime elapses.
/// </summary>
public class HauntingWispsBehaviour : BaseAttackBehaviour
{
    private HauntingWispsConfig config;
    private readonly List<GameObject> activeWisps = new();
    private int extraCount;
    private Transform cannon;

    public void SetConfig(HauntingWispsConfig cfg) => config = cfg;

    public override void OnEnrage() => extraCount += config.enrageExtraCount;

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(WispSequence());
    }

    private IEnumerator WispSequence()
    {
        cannon = GameObject.FindWithTag("Player")?.transform;

        int count = Random.Range(config.minCount, config.maxCount + 1) + extraCount;
        for (int i = 0; i < count; i++)
            SpawnWisp();

        float elapsed = 0f;
        while (elapsed < config.wispLifetime && activeWisps.Count > 0)
        {
            HomeWisps();
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnAll();
        isRunning = false;
        NotifyAttackComplete();
    }

    private void SpawnWisp()
    {
        if (config.wispPrefab == null) return;

        Vector2 offset = Random.insideUnitCircle * config.spawnRadius;
        Vector3 pos = boss.transform.position + (Vector3)offset;
        var obj = PoolManager.Get(config.wispPrefab, pos);

        var health = obj.GetComponent<WispHealth>();
        if (health == null) health = obj.AddComponent<WispHealth>();
        health.Initialize(config.wispHP, config.contactDamage);
        health.OnKilled = OnWispKilled;

        activeWisps.Add(obj);
    }

    private void HomeWisps()
    {
        if (cannon == null) cannon = GameObject.FindWithTag("Player")?.transform;
        if (cannon == null) return;

        float step = config.moveSpeed * Time.deltaTime;
        for (int i = activeWisps.Count - 1; i >= 0; i--)
        {
            var w = activeWisps[i];
            if (w == null) { activeWisps.RemoveAt(i); continue; }
            w.transform.position = Vector3.MoveTowards(w.transform.position, cannon.position, step);
        }
    }

    private void OnWispKilled(GameObject wisp)
    {
        activeWisps.Remove(wisp);
        PoolManager.Return(wisp);
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeWisps.Count; i++)
            if (activeWisps[i] != null) PoolManager.Return(activeWisps[i]);
        activeWisps.Clear();
    }

    public override void OnStop() { StopAllCoroutines(); ReturnAll(); isRunning = false; }
    public override void OnCleanup() => OnStop();
}
