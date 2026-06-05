// RadioactiveEggDropBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using Gameplay.Eggs;
using UnityEngine;

public class RadioactiveEggDropBehaviour : BaseAttackBehaviour
{
    protected RadioactiveEggDropConfig config;
    protected BossMovementHandler movement;
    protected Transform dropPoint;

    private readonly List<Egg> activeEggs = new();

    public void SetConfig(RadioactiveEggDropConfig cfg) => config = cfg;

    protected virtual void Start()
    {
        movement = boss != null ? boss.GetComponent<BossMovementHandler>() : null;

        if (boss != null && !string.IsNullOrEmpty(config.dropPointName))
            dropPoint = boss.transform.Find(config.dropPointName);
    }

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        if (movement != null) movement.Stop();

        bool warningDone = false;
        ShowWarning(config, () => warningDone = true);
        while (!warningDone) yield return null;

        SpawnEgg();

        if (config.stopDuration > 0f)
            yield return new WaitForSeconds(config.stopDuration);

        if (movement != null) movement.Resume();

        isRunning = false;
        NotifyAttackComplete();
    }

    // Subclass hook: per-egg size multiplier applied to the cloned tier's
    // sizeRange before Egg.Init. Default 1 means no change.
    protected virtual float GetSizeMultiplier() => 1f;

    protected virtual void SpawnEgg()
    {
        if (config.eggPrefab == null) return;

        Vector3 pos = dropPoint != null ? dropPoint.position : boss.transform.position;
        int hp = Random.Range(config.eggHpMin, config.eggHpMax + 1);
        SpawnEggAt(pos, hp, GetSizeMultiplier());
    }

    // Shared spawn plumbing — used for the primary drop and for split children
    // (SuperEggDropBehaviour). Returns null when the pool object isn't an egg.
    protected Egg SpawnEggAt(Vector3 pos, int hp, float sizeMultiplier)
    {
        var obj = PoolManager.Get(config.eggPrefab, pos);
        if (obj == null) return null;

        var egg = obj.GetComponent<Egg>();
        if (egg == null || egg.config == null)
        {
            // Not a real egg prefab — bail and return to pool to avoid leaking.
            PoolManager.Return(obj);
            return null;
        }

        // BossEggLifetime must Init BEFORE Egg.Init so its size override on the
        // cloned tier is in place when ApplyPersonality reads sizeRange.
        var lifetimeCtl = obj.GetComponent<BossEggLifetime>()
                          ?? obj.AddComponent<BossEggLifetime>();
        lifetimeCtl.Init(config.eggLifetime, sizeMultiplier);

        egg.OnReleaseReady = OnEggReleaseReady;
        egg.Init(egg.config, hp, config.eggSortingIndex);

        activeEggs.Add(egg);
        return egg;
    }

    private void OnEggReleaseReady(Egg egg)
    {
        if (egg == null) return;
        activeEggs.Remove(egg);
        egg.OnPoolRelease();
        PoolManager.Return(egg.gameObject);
    }

    public override void OnStop()
    {
        StopAllCoroutines();

        if (movement != null) movement.Resume();

        // Force-clean any eggs still in flight when the boss is stopped/cleaned up.
        for (int i = activeEggs.Count - 1; i >= 0; i--)
        {
            var egg = activeEggs[i];
            if (egg == null) { activeEggs.RemoveAt(i); continue; }

            egg.OnReleaseReady = null;
            egg.OnPoolRelease();
            PoolManager.Return(egg.gameObject);
        }
        activeEggs.Clear();

        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnCleanup() => OnStop();
}
