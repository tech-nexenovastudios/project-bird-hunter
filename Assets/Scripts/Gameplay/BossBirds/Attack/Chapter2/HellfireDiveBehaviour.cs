// HellfireDiveBehaviour.cs
using System.Collections;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

public class HellfireDiveBehaviour : BaseAttackBehaviour
{
    private HellfireDiveConfig config;
    private Vector3 originalPosition;
    private GameObject activeColumn;
    private Collider2D columnCollider;

    // Locked once at attack start
    private Vector3 cannonPosition;
    private Collider2D cannonCollider;
    private BaseCannon cannonComponent;
    private bool hasTarget;

    public void SetConfig(HellfireDiveConfig cfg) => config = cfg;

    protected override void OnExecute()
    {
        var cannonObj = GameObject.FindWithTag("Player");
        if (cannonObj != null)
        {
            cannonPosition = cannonObj.transform.position;
            cannonCollider = cannonObj.GetComponent<Collider2D>();
            cannonComponent = cannonObj.GetComponent<BaseCannon>();
            hasTarget = cannonCollider != null && cannonComponent != null;
        }
        else
        {
            hasTarget = false;
            Debug.LogWarning("[HellfireDive] No Player found!");
        }

        NotifyAttackStarted();
        ShowWarning(config, () => StartCoroutine(DiveSequence()));
    }

    private IEnumerator DiveSequence()
    {
        originalPosition = boss.transform.position;

        float hoverOffset = Random.Range(4f, 5f);
        Vector3 diveTarget = new Vector3(cannonPosition.x, cannonPosition.y + hoverOffset, 0f);

        // Phase 1: Dive toward position above cannon
        while (Vector3.Distance(boss.transform.position, diveTarget) > 0.05f)
        {
            boss.transform.position = Vector3.MoveTowards(
                boss.transform.position, diveTarget, config.diveSpeed * Time.deltaTime);
            yield return null;
        }
        boss.transform.position = diveTarget;

        // Phase 2: Spawn fire column
        float columnHeight = diveTarget.y - cannonPosition.y;
        Vector3 columnMidpoint = new Vector3(
            diveTarget.x,
            cannonPosition.y + columnHeight * 0.5f,
            0f);

        activeColumn = PoolManager.Get(config.fireColumnPrefab, columnMidpoint);
        if (activeColumn != null)
        {
            //Rotate 180° on X so the VFX faces downward toward the cannon
            activeColumn.transform.rotation = Quaternion.Euler(180f, 0f, 0f);

            activeColumn.transform.localScale = new Vector3(
                activeColumn.transform.localScale.x,
                columnHeight * 0.15f,
                activeColumn.transform.localScale.z);

            columnCollider = activeColumn.GetComponent<Collider2D>();

            // FIX 2: Pooled objects keep stale particle state and won't auto-replay
            // on SetActive — must explicitly stop-clear-play every ParticleSystem
            var allPS = activeColumn.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allPS)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }
        else
        {
            Debug.LogError("[HellfireDive] fireColumnPrefab failed to spawn!");
        }

        yield return new WaitForSeconds(config.hoverDurationAtBottom);

        // Phase 3: Return to original position
        while (Vector3.Distance(boss.transform.position, originalPosition) > 0.05f)
        {
            boss.transform.position = Vector3.MoveTowards(
                boss.transform.position, originalPosition, config.returnSpeed * Time.deltaTime);
            yield return null;
        }
        boss.transform.position = originalPosition;

        // Phase 4: Column active — damage per second while cannon is inside it
        float columnTimer = 0f;
        float damageCooldown = 0f;

        while (columnTimer < config.columnLifetime)
        {
            float dt = Time.deltaTime;
            columnTimer += dt;
            damageCooldown += dt;

            if (damageCooldown >= 1f)
            {
                damageCooldown -= 1f;
                if (hasTarget && IsCannonTouchingColumn())
                    cannonComponent.TakeDamage(config.columnDamagePerTick);
            }

            yield return null;
        }

        ReturnColumn();
        isRunning = false;
        NotifyAttackComplete();
    }

    private bool IsCannonTouchingColumn()
    {
        if (activeColumn == null || !hasTarget) return false;

        if (columnCollider != null)
            return cannonCollider.bounds.Intersects(columnCollider.bounds);

        // Fallback: manual overlap box
        float columnHeight = activeColumn.transform.localScale.y;
        Vector2 size = new Vector2(config.columnWidth, columnHeight);
        return cannonCollider.OverlapPoint(activeColumn.transform.position) ||
               Physics2D.OverlapBox(activeColumn.transform.position, size, 0f,
                   LayerMask.GetMask("Player")) != null;
    }

    private void ReturnColumn()
    {
        if (activeColumn == null) return;

        var allPS = activeColumn.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allPS)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        PoolManager.Return(activeColumn);
        activeColumn = null;
        columnCollider = null;
    }

    public override void OnStop()
    {
        StopAllCoroutines();
        ReturnColumn();
        if (boss != null) boss.transform.position = originalPosition;
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnCleanup() => OnStop();
}