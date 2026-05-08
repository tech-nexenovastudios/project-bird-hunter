// SerpentEggDropBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerpentEggDropBehaviour : BaseAttackBehaviour
{
    private SerpentEggDropConfig config;
    private readonly List<Transform> dropPoints = new();
    private readonly List<GameObject> activeEggs = new();

    public void SetConfig(SerpentEggDropConfig cfg) => config = cfg;

    private void Start()
    {
        var parent = boss.transform.Find(config.dropParentName);
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
                dropPoints.Add(parent.GetChild(i));
            if (dropPoints.Count == 0)
                dropPoints.Add(parent);
        }
    }

    protected override void OnExecute()
    {
        ShowWarning(config, () => StartCoroutine(DropEggs()));
    }

    private IEnumerator DropEggs()
    {
        for (int i = 0; i < config.dropCount; i++)
        {
            Vector3 pos = dropPoints.Count > 0
                ? dropPoints[Random.Range(0, dropPoints.Count)].position
                : boss.transform.position;

            var egg = PoolManager.Get(config.eggPrefab, pos);
            activeEggs.Add(egg);

            // Setup ground detector for rolling + one-hit destruction
            if (!egg.TryGetComponent<HazardGroundDetector>(out var detector))
                detector = egg.AddComponent<HazardGroundDetector>();
            detector.OnDestroyed += OnEggDestroyed;

            if (egg.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = config.dropGravityScale;
                rb.constraints = RigidbodyConstraints2D.None;

                float dir = config.randomDirection ? (Random.value > 0.5f ? 1f : -1f) : 1f;
                detector.Initialize(rb, config.rollSpeed * dir, config.maxRollDistance);
            }

            if (i < config.dropCount - 1)
                yield return new WaitForSeconds(config.dropInterval);
        }

        yield return new WaitForSeconds(config.eggLifetime);
        ReturnAll();
        isRunning = false;
    }

    private void OnEggDestroyed(HazardGroundDetector detector)
    {
        if (detector == null) return;

        var go = detector.gameObject;
        detector.OnDestroyed -= OnEggDestroyed;

        if (config.deathVfxPrefab != null)
        {
            var vfx = Instantiate(config.deathVfxPrefab, go.transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        activeEggs.Remove(go);
        PoolManager.Return(go);
    }

    private void ReturnAll()
    {
        for (int i = 0; i < activeEggs.Count; i++)
        {
            if (activeEggs[i] == null) continue;

            if (activeEggs[i].TryGetComponent<HazardGroundDetector>(out var detector))
                detector.OnDestroyed -= OnEggDestroyed;

            PoolManager.Return(activeEggs[i]);
        }
        activeEggs.Clear();
    }

    public override void OnStop() { StopAllCoroutines(); ReturnAll(); isRunning = false; }
    public override void OnCleanup() => OnStop();
}
