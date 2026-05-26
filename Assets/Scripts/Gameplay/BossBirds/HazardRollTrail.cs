using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the hazard prefab alongside HazardGroundDetector.
/// While rolling, stamps a poison VFX patch every <see cref="stampInterval"/> world-units.
/// Each stamp scales up from zero to give the "spreading along the path" feel.
/// </summary>
public class HazardRollTrail : MonoBehaviour, IPoolable
{
    [Header("Set via config — call Initialize() before StartTrail()")]
    [SerializeField] private GameObject trailVfxPrefab;
    [SerializeField] private float stampInterval = 0.4f;  // world-units between stamps
    [SerializeField] private float vfxLifetime = 3f;    // how long each stamp persists

    private bool isActive;
    private float distanceSinceLastStamp;
    private Vector3 lastPosition;

    // Per-second damage each spawned patch deals to the player while stood in it.
    // Set from the attack config via SetPatchDamage(); 0 = patches are harmless visuals.
    private int patchDamagePerSecond;

    // All stamps spawned during this roll — so we can force-return them on cleanup
    private readonly List<GameObject> activeStamps = new();

    // ── Public API ────────────────────────────────────────────────────

    public void Initialize(GameObject vfxPrefab, float interval, float lifetime)
    {
        trailVfxPrefab = vfxPrefab;
        stampInterval = interval;
        vfxLifetime = lifetime;
    }

    /// <summary>Set the per-second damage each dropped patch deals to the player. 0 disables patch damage.</summary>
    public void SetPatchDamage(int damagePerSecond) => patchDamagePerSecond = Mathf.Max(0, damagePerSecond);

    public void StartTrail()
    {
        if (trailVfxPrefab == null)
        {
            Debug.LogWarning("[HazardRollTrail] trailVfxPrefab is null — no trail will play.", this);
            return;
        }

        isActive = true;
        distanceSinceLastStamp = stampInterval; // stamp immediately on first frame
        lastPosition = transform.position;

        // Drop the first stamp straight away so there's no gap at the landing point
        StampHere();
    }

    public void StopTrail() => isActive = false;

    /// <summary>Force-return all live stamps to the pool (call on despawn/stop).</summary>
    public void Cleanup()
    {
        StopTrail();
        for (int i = activeStamps.Count - 1; i >= 0; i--)
        {
            if (activeStamps[i] != null)
                PoolManager.Return(activeStamps[i]);
        }
        activeStamps.Clear();
    }

    // ── IPoolable ─────────────────────────────────────────────────────

    public void OnPoolSpawned()
    {
        isActive = false;
        distanceSinceLastStamp = 0f;
        activeStamps.Clear();
    }

    public void OnPoolDespawned() => Cleanup();

    // ── Internals ────────────────────────────────────────────────────

    private void Update()
    {
        if (!isActive) return;

        float moved = Vector3.Distance(transform.position, lastPosition);
        distanceSinceLastStamp += moved;
        lastPosition = transform.position;

        // Stamp once per interval — loop handles bursts if framerate is low
        while (distanceSinceLastStamp >= stampInterval)
        {
            StampHere();
            distanceSinceLastStamp -= stampInterval;
        }
    }

    private void StampHere()
    {
        // Snap to ground surface: raycast downward from the hazard centre
        Vector3 spawnPos = GetGroundPosition();

        var stamp = PoolManager.Get(trailVfxPrefab, spawnPos);
        activeStamps.Add(stamp);

        // Let the stamp know how long to live and drive its own scale-up + auto-return
        if (stamp.TryGetComponent<HazardTrailStamp>(out var s))
            s.Activate(vfxLifetime, OnStampExpired);

        // Arm the patch's damage-over-time (if this hazard is configured to hurt the player).
        if (stamp.TryGetComponent<PoisonPatchDamage>(out var dmg))
            dmg.Init(patchDamagePerSecond);
    }

    private Vector3 GetGroundPosition()
    {
        var origin = transform.position + Vector3.up * 0.5f;
        if (Physics2D.Raycast(origin, Vector2.down, 2f) is { } hit && hit.collider != null)
        {
            Vector3 pos = hit.point;

            pos.y -= 0.3f;       // lift slightly above ground so it doesn't z-fight
            //pos.z = -0.1f;        // push in front of the ground layer (2D sorting)
                                  // pos.x += 0.2f;     // nudge horizontally if needed
            return pos;
        }

        return transform.position + Vector3.down * 0.1f;
    }

    private void OnStampExpired(GameObject stamp)
    {
        activeStamps.Remove(stamp);
        // PoolManager.Return is already called by HazardTrailStamp — no double-return needed
    }
}