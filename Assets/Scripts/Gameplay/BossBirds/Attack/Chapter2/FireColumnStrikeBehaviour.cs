// FireColumnStrikeBehaviour.cs
using System.Collections;
using Gameplay.Player;
using UnityEngine;

// Inferno Phoenix phase-2 attack. The boss stays in place and repeatedly drops a fire
// column that spans from the boss's own position down to the cannon's locked position.
// Each column lives for columnLifetime, then after respawnDelay a new one is locked onto
// the cannon's current position and spawned. Loops until the attack is stopped.
public class FireColumnStrikeBehaviour : BaseAttackBehaviour
{
    private FireColumnStrikeConfig config;
    private BossMovementHandler movement;
    private Transform spawnPoint;
    private GameObject activeColumn;
    private ParticleSystemRenderer beamRenderer;
    private Vector2 columnHitCenter;
    private Vector2 columnHitSize;
    private bool columnBoundsValid;
    private static int playerLayerMask;
    private static bool playerLayerMaskInit;

    public void SetConfig(FireColumnStrikeConfig cfg) => config = cfg;

    private void Start()
    {
        if (!playerLayerMaskInit)
        {
            playerLayerMask = LayerMask.GetMask("Player");
            playerLayerMaskInit = true;
        }

        movement = boss.GetComponent<BossMovementHandler>();

        // The column originates here. It's a child of the boss, so it rides along and its
        // X is the boss-relative spawn point you author (independent of the cannon's X).
        spawnPoint = boss.transform.Find("FireBlockSpawnPosition");
        if (spawnPoint == null)
        {
            var go = new GameObject("FireBlockSpawnPosition");
            go.transform.SetParent(boss.transform, false);
            spawnPoint = go.transform;
            Debug.LogWarning("[FireColumnStrike] 'FireBlockSpawnPosition' child not found on boss — " +
                             "created one at the boss origin. Add it to the prefab and position it.", boss);
        }
    }

    protected override void OnExecute()
    {
        // Single long-running loop: lock cannon → spawn column → live columnLifetime →
        // destroy → wait respawnDelay → repeat. OnStop ends it (phase change / retreat / death).
        NotifyAttackStarted();
        StartCoroutine(StrikeLoop());
    }

    private IEnumerator StrikeLoop()
    {
        while (true)
        {
            // Lock the cannon's position fresh for each strike.
            var cannonObj = GameObject.FindWithTag("Player");
            if (cannonObj == null)
            {
                // No target yet (e.g. cannon still spawning at fight start) — wait and retry.
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            Vector3 cannonPos = cannonObj.transform.position;
            SpawnColumn(cannonPos);

            // Freeze the boss where it is for as long as this column is alive.
            movement?.Stop();

            // Damage the cannon per tick while the column is alive.
            float timer = 0f;
            float damageCooldown = 0f;
            float tickInterval = 1f / Mathf.Max(0.01f, config.columnTicksPerSecond);

            while (timer < config.columnLifetime)
            {
                float dt = Time.deltaTime;
                timer += dt;
                damageCooldown += dt;

                if (damageCooldown >= tickInterval)
                {
                    damageCooldown -= tickInterval;
                    TryDamageCannonInColumn();
                }

                yield return null;
            }

            ReturnColumn();

            // Column gone — let the boss move again during the gap before the next strike.
            movement?.Resume();

            // Wait before spawning the next column.
            yield return new WaitForSeconds(config.respawnDelay);
        }
    }

    private void SpawnColumn(Vector3 cannonPos)
    {
        // The prefab is a particle beam that emits downward from its transform origin, so the
        // emitter sits exactly at FireBlockSpawnPosition (its X and Y). The beam drops straight
        // down from there; the cannon's Y only sets how far down the damage region reaches.
        Vector3 origin = spawnPoint != null ? spawnPoint.position : boss.transform.position;
        float columnHeight = Mathf.Max(0.1f, origin.y - cannonPos.y);
        Vector3 spawnPos = new Vector3(origin.x, origin.y, 0f);

        activeColumn = PoolManager.Get(config.fireColumnPrefab, spawnPos);
        if (activeColumn == null)
        {
            Debug.LogError("[FireColumnStrike] fireColumnPrefab failed to spawn!");
            columnBoundsValid = false;
            return;
        }

        // Rotate 180° on X so the VFX faces downward toward the cannon.
        activeColumn.transform.rotation = Quaternion.Euler(180f, 0f, 0f);

        activeColumn.transform.localScale = new Vector3(
            activeColumn.transform.localScale.x,
            columnHeight * 0.15f,
            activeColumn.transform.localScale.z);

        // World-space damage region (the prefab's localScale.y is a VFX tuning value, not the
        // hit height, so we trust the geometry). Pad vertically so a cannon sitting just below
        // the locked Y is still inside the box. Width is resolved per tick from the live beam
        // visuals so damage requires actual contact with the column.
        float verticalPad = 1.5f;
        columnHitCenter = new Vector2(
            origin.x,
            cannonPos.y + (columnHeight * 0.5f) - (verticalPad * 0.5f));
        columnHitSize = new Vector2(
            config.columnWidth,
            columnHeight + verticalPad);
        columnBoundsValid = true;

        // The root ParticleSystem is the core beam (children are embers/sparks that drift
        // wider than the flame itself) — its rendered bounds define the contact width.
        beamRenderer = activeColumn.GetComponent<ParticleSystemRenderer>();

        // Pooled objects keep stale particle state and won't auto-replay on SetActive —
        // explicitly stop-clear-play every ParticleSystem.
        var allPS = activeColumn.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allPS)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private void TryDamageCannonInColumn()
    {
        if (activeColumn == null || !columnBoundsValid) return;
        if (!TryGetBeamContactBox(out Vector2 hitCenter, out Vector2 hitSize)) return;

        // Fresh physics overlap so the check is robust against the column having no Collider2D
        // (it's a VFX) and against the cannon prefab being swapped mid-run.
        var hit = Physics2D.OverlapBox(hitCenter, hitSize, 0f, playerLayerMask);
        if (hit == null) return;

        var cannon = hit.GetComponentInParent<BaseCannon>();
        if (cannon == null) return;

        cannon.TakeDamage(config.columnDamagePerTick);
    }

    // The damage region this frame: the analytic column box (spawn X → cannon Y span)
    // intersected with the beam's actual rendered world bounds, so damage requires genuine
    // visual contact — before the particles reach the cannon, after they fade, or if the
    // beam drifts off the locked X, no damage is dealt. Returns false when the beam isn't
    // rendering (no visible column = no contact).
    private bool TryGetBeamContactBox(out Vector2 center, out Vector2 size)
    {
        center = columnHitCenter;
        size = columnHitSize;
        if (beamRenderer == null)
        {
            // No renderer to track — fall back to the analytic box capped at columnWidth.
            size.x = Mathf.Min(size.x, config.columnWidth);
            return true;
        }

        Bounds rendered = beamRenderer.bounds;
        if (rendered.size.x < 0.01f || rendered.size.y < 0.01f)
            return false; // nothing rendered yet (or already cleared)

        // Intersect analytic box with rendered bounds on both axes.
        float xMin = Mathf.Max(columnHitCenter.x - columnHitSize.x * 0.5f, rendered.min.x);
        float xMax = Mathf.Min(columnHitCenter.x + columnHitSize.x * 0.5f, rendered.max.x);
        float yMin = Mathf.Max(columnHitCenter.y - columnHitSize.y * 0.5f, rendered.min.y);
        float yMax = Mathf.Min(columnHitCenter.y + columnHitSize.y * 0.5f, rendered.max.y);
        if (xMax <= xMin || yMax <= yMin)
            return false; // beam visuals don't overlap the column region at all

        center = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        size = new Vector2(Mathf.Min(xMax - xMin, config.columnWidth), yMax - yMin);
        return true;
    }

    private void ReturnColumn()
    {
        if (activeColumn == null) return;

        var allPS = activeColumn.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allPS)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        PoolManager.Return(activeColumn);
        activeColumn = null;
        beamRenderer = null;
        columnBoundsValid = false;
    }

    public override void OnStop()
    {
        StopAllCoroutines();
        ReturnColumn();
        // Don't leave the boss frozen if we were interrupted mid-column. (Retreat/death paths
        // in BossBirdController call movement.Stop() right after this, so re-stopping wins.)
        movement?.Resume();
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnCleanup() => OnStop();

#if UNITY_EDITOR
    // Scene-view debug: yellow = full analytic column region, red = live contact box that
    // actually deals damage this frame (beam visuals ∩ column region). Select the boss in
    // the Hierarchy with Gizmos enabled to see them while a column is active.
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || activeColumn == null || !columnBoundsValid) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(columnHitCenter, columnHitSize);

        if (TryGetBeamContactBox(out Vector2 center, out Vector2 size))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}
