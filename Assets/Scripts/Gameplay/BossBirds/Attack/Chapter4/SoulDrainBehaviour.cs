// SoulDrainBehaviour.cs
using System.Collections;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

public class SoulDrainBehaviour : BaseAttackBehaviour
{
    private SoulDrainConfig config;

    private GameObject activeTelegraph;
    private GameObject activeGroundVfx;
    private LaserBeamVisual tetherLine;
    private GameObject tetherLineObj;

    private Transform cannonTarget;
    private float tetherX;
    private bool cannonIsInside;
    private bool debuffApplied;

    private float damageTickInterval;
    private float damageTickTimer;

    private static readonly Collider2D[] overlap = new Collider2D[8];
    private static int playerMask;
    private static bool maskInit;

    public void SetConfig(SoulDrainConfig cfg) => config = cfg;

    private void Start()
    {
        if (!maskInit)
        {
            playerMask = LayerMask.GetMask("Player", "Cannon");
            maskInit = true;
        }
        damageTickInterval = 1f / Mathf.Max(config.ticksPerSecond, 0.1f);
    }

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(SoulDrainSequence());
    }

    private IEnumerator SoulDrainSequence()
    {
        cannonTarget = FindCannon();
        if (cannonTarget == null)
        {
            isRunning = false;
            NotifyAttackComplete();
            yield break;
        }

        // Lock cannon X position
        tetherX = cannonTarget.position.x;

        // Telegraph
        if (config.telegraphPrefab != null)
        {
            Vector3 telegraphPos = new Vector3(tetherX, config.groundY, 0f);
            activeTelegraph = PoolManager.Get(config.telegraphPrefab, telegraphPos);
            PlayParticles(activeTelegraph);

            yield return new WaitForSeconds(config.telegraphDuration);

            StopParticles(activeTelegraph);
            PoolManager.Return(activeTelegraph);
            activeTelegraph = null;
        }

        // Activate tether line
        ActivateTether();

        // Main loop
        float elapsed = 0f;
        damageTickTimer = damageTickInterval; // First tick is immediate on entry
        cannonIsInside = false;
        debuffApplied = false;

        while (elapsed < config.tetherActiveDuration)
        {
            UpdateTether();
            elapsed += Time.deltaTime;
            yield return null;
        }

        Shutdown();
    }

    private void ActivateTether()
    {
        Vector3 top = new Vector3(tetherX, boss.transform.position.y, 0f);
        Vector3 bottom = new Vector3(tetherX, config.groundY, 0f);

        // Spawn LaserBeamVisual
        if (config.tetherLinePrefab != null)
        {
            tetherLineObj = Instantiate(config.tetherLinePrefab);
            tetherLine = tetherLineObj.GetComponent<LaserBeamVisual>();

            if (tetherLine != null)
            {
                tetherLine.SetColors(config.coreColor, config.glowColor, config.haloColor);
                tetherLine.SetPositions(top, bottom);
                tetherLine.SetLinesEnabled(true);
                tetherLine.SetWidth(config.widthMultiplier);
            }
        }

        // Ground impact VFX
        if (config.groundImpactVfxPrefab != null)
        {
            activeGroundVfx = PoolManager.Get(config.groundImpactVfxPrefab, bottom);
            PlayParticles(activeGroundVfx);
        }
    }

    private void UpdateTether()
    {
        // Update line — top follows boss Y, X stays locked
        Vector3 top = new Vector3(tetherX, boss.transform.position.y, 0f);
        Vector3 bottom = new Vector3(tetherX, config.groundY, 0f);

        if (tetherLine != null)
            tetherLine.SetPositions(top, bottom);

        // Check if cannon is inside
        if (cannonTarget != null)
        {
            float halfWidth = config.tetherWidth * 0.5f;
            cannonIsInside = Mathf.Abs(cannonTarget.position.x - tetherX) <= halfWidth;
        }
        else
        {
            cannonIsInside = false;
        }

        // Debuff
        if (cannonIsInside && !debuffApplied)
        {
            debuffApplied = true;
            SetCannonSpeed(config.speedDebuffMultiplier);
        }
        else if (!cannonIsInside && debuffApplied)
        {
            debuffApplied = false;
            SetCannonSpeed(1f);
        }

        // Damage
        if (cannonIsInside)
        {
            damageTickTimer += Time.deltaTime;
            if (damageTickTimer >= damageTickInterval)
            {
                damageTickTimer -= damageTickInterval;
                DamageCannon(top, bottom);
            }
        }
        else
        {
            // Reset so re-entry gets immediate tick
            damageTickTimer = damageTickInterval;
        }
    }

    private void DamageCannon(Vector3 top, Vector3 bottom)
    {
        Vector2 center = new Vector2(tetherX, (top.y + bottom.y) * 0.5f);
        float height = Mathf.Abs(top.y - bottom.y);
        Vector2 size = new Vector2(config.tetherWidth, height);

        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, overlap, playerMask);
        for (int i = 0; i < count; i++)
        {
            var col = overlap[i];
            if (col != null && col.CompareTag("Player") && col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(config.damagePerTick);
        }
    }

    private void SetCannonSpeed(float multiplier)
    {
        if (cannonTarget != null && cannonTarget.TryGetComponent<BaseCannon>(out var cannon))
            cannon.ApplySpeedMultiplier(multiplier);
    }

    private Transform FindCannon()
    {
        var obj = GameObject.FindWithTag("Player");
        return obj != null ? obj.transform : null;
    }

    private static void PlayParticles(GameObject obj)
    {
        if (obj == null) return;
        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
        {
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

    private void Shutdown()
    {
        if (debuffApplied)
        {
            debuffApplied = false;
            SetCannonSpeed(1f);
        }

        if (activeTelegraph != null)
        {
            StopParticles(activeTelegraph);
            PoolManager.Return(activeTelegraph);
            activeTelegraph = null;
        }

        if (tetherLineObj != null)
        {
            if (tetherLine != null) tetherLine.Deactivate();
            Destroy(tetherLineObj);
            tetherLineObj = null;
            tetherLine = null;
        }

        if (activeGroundVfx != null)
        {
            StopParticles(activeGroundVfx);
            PoolManager.Return(activeGroundVfx);
            activeGroundVfx = null;
        }

        cannonIsInside = false;
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnStop()
    {
        StopAllCoroutines();
        Shutdown();
    }

    public override void OnCleanup() => OnStop();
}