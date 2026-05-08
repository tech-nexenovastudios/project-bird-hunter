using UnityEngine;
using System.Collections.Generic;

public class BossBirdController : MonoBehaviour
{
    private const float RetreatThreshold = 0.5f;

    [Header("Direct Test Mode (remove after testing)")]
    [Tooltip("Drag a BossBirdConfig here to auto-initialize on Start")]
    [SerializeField] private BossBirdConfig directTestConfig;
    [SerializeField] private bool testAsLevel20 = false;

    private BossBirdConfig config;
    private readonly List<BaseAttackBehaviour> attacks = new();
    private BossMovementHandler movement;
    private BossHealthHandler health;
    private BossBirdAnimationController animController;
    private bool isDead;
    private bool isInitialized;
    private bool hasEnraged;

    // ── NEW: Invulnerability flag — set by SpawnController during enter/exit tweens ──
    private bool _invulnerable;
    public bool IsInvulnerable => _invulnerable;

    // ── NEW: Retreat flag — boss retreats at 50% HP on non-level-20 encounters ──
    private bool _hasRetreated;
    public bool HasRetreated => _hasRetreated;

    // ── NEW: Track whether this is the level-20 (phase 2) encounter ──
    private bool _isLevel20;
    public bool IsLevel20 => _isLevel20;

    public bool IsDead => isDead;

    private void Start()
    {
        if (directTestConfig != null && !isInitialized)
        {
            Debug.Log($"[BossBird] AUTO-INIT from Inspector: {directTestConfig.bossName}", this);
            Initialize(directTestConfig, testAsLevel20);
        }
    }

    // ── NEW: SpawnController calls these during enter/exit tweens ──
    public void SetInvulnerable(bool value)
    {
        _invulnerable = value;

        // Disable all colliders so taps/projectiles pass through
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
            col.enabled = !value;

        // Also check for 3D colliders just in case
        var colliders3D = GetComponents<Collider>();
        foreach (var col in colliders3D)
            col.enabled = !value;
    }

    public void Initialize(BossBirdConfig cfg, bool isLevel20)
    {
        config = cfg;
        isDead = false;
        isInitialized = false;
        hasEnraged = false;
        _hasRetreated = false;
        _isLevel20 = isLevel20;

        // ---- Validate required components ----
        health = GetComponent<BossHealthHandler>();
        if (health == null)
        {
            Debug.LogError($"[BossBird] {cfg.bossName}: BossHealthHandler missing on prefab!", this);
            return;
        }

        movement = GetComponent<BossMovementHandler>();
        if (movement == null)
        {
            Debug.LogError($"[BossBird] {cfg.bossName}: BossMovementHandler missing on prefab!", this);
            return;
        }

        // ---- Animation controller (optional — safe if not present) ----
        animController = GetComponent<BossBirdAnimationController>();
        if (animController == null)
            Debug.LogWarning($"[BossBird] {cfg.bossName}: No BossBirdAnimationController found — animations won't play", this);

        // ---- Visuals ----
        var spineComponent = GetComponent("SkeletonAnimation");
        if (spineComponent == null)
        {
            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                if (config.bossSprite != null)
                    sr.sprite = config.bossSprite;
                else
                    Debug.LogWarning($"[BossBird] {cfg.bossName}: bossSprite is null", this);
            }
        }

        if (config.animatorController != null && TryGetComponent<Animator>(out var anim))
            anim.runtimeAnimatorController = config.animatorController;

        // ---- Health ----
        // Phase 1: full HP. Direct level-20 spawn (test mode): scaled max, starting at the
        // post-retreat 50% mark so the boss matches the "returning at 50%" form.
        float maxHp = isLevel20
            ? config.maxHealth * config.phase2HealthMultiplier
            : config.maxHealth;
        float startHp = isLevel20 ? maxHp * RetreatThreshold : maxHp;
        health.OnHealthChanged += OnHealthChanged;
        health.OnDeath += OnDeath;
        health.Initialize(maxHp, startHp, config.enrageThreshold);

        // ---- Movement ----
        var moveConfig = isLevel20 && config.phase2Movement != null
            ? config.phase2Movement
            : config.phase1Movement;

        if (moveConfig != null)
            movement.Initialize(moveConfig);
        else
            Debug.LogWarning($"[BossBird] {cfg.bossName}: No movement config — boss won't move", this);

        // ---- Attacks ----
        var existingAttacks = GetComponents<BaseAttackBehaviour>();
        for (int i = 0; i < existingAttacks.Length; i++)
            Destroy(existingAttacks[i]);
        attacks.Clear();

        SpawnAttack(config.phase1Attack, "Phase1");
        if (isLevel20)
            SpawnAttack(config.phase2Attack, "Phase2");

        if (attacks.Count == 0)
            Debug.LogWarning($"[BossBird] {cfg.bossName}: No attacks spawned — boss is passive", this);

        isInitialized = true;
        BossEventBus.RaiseBossSpawned(config.bossName);

        Debug.Log($"[BossBird] {cfg.bossName} initialized — " +
                  $"HP:{startHp:F0}/{maxHp:F0} | Attacks:{attacks.Count} | " +
                  $"Phase:{(isLevel20 ? "2 (Level 20)" : "1 (Pre-20)")}", this);
    }

    /// <summary>
    /// Re-initialize for level 20 using remaining HP from phase 1.
    /// Called by SpawnController when re-spawning a retreated boss.
    /// </summary>
    public void ReinitializeForPhase2(float remainingHpNormalized)
    {
        isDead = false;
        isInitialized = false;
        hasEnraged = false;
        _hasRetreated = false;
        _isLevel20 = true;

        // ---- Health: phase 2 keeps the same normalized HP that phase 1 ended on
        //              (default: 50% remaining), and uses phase2HealthMultiplier as
        //              the new max so phase 2 can be a tougher form. ----
        health = GetComponent<BossHealthHandler>();
        float phase2MaxHp = config.maxHealth * config.phase2HealthMultiplier;
        float startHp = phase2MaxHp * Mathf.Clamp01(remainingHpNormalized);
        health.OnHealthChanged += OnHealthChanged;
        health.OnDeath += OnDeath;
        health.Initialize(phase2MaxHp, startHp, config.enrageThreshold);

        // ---- Movement: use phase2 config ----
        movement = GetComponent<BossMovementHandler>();
        var moveConfig = config.phase2Movement != null
            ? config.phase2Movement
            : config.phase1Movement;

        if (moveConfig != null)
            movement.Initialize(moveConfig);

        // ---- Animation ----
        animController = GetComponent<BossBirdAnimationController>();

        // ---- Attacks: add phase2 attack ----
        var existingAttacks = GetComponents<BaseAttackBehaviour>();
        for (int i = 0; i < existingAttacks.Length; i++)
            Destroy(existingAttacks[i]);
        attacks.Clear();

        SpawnAttack(config.phase1Attack, "Phase1");
        SpawnAttack(config.phase2Attack, "Phase2");

        isInitialized = true;
        BossEventBus.RaiseBossSpawned(config.bossName);

        Debug.Log($"[BossBird] {config.bossName} RE-INITIALIZED for Phase 2 — " +
                  $"HP:{startHp:F0} ({remainingHpNormalized:P0} remaining) | Attacks:{attacks.Count}", this);
    }

    private void SpawnAttack(BaseAttackConfig attackConfig, string phaseName)
    {
        if (attackConfig == null)
        {
            Debug.LogWarning($"[BossBird] {config.bossName}: {phaseName} attack is null — skipping", this);
            return;
        }

        var behaviour = attackConfig.CreateAttack(gameObject);
        if (behaviour == null)
        {
            Debug.LogError($"[BossBird] {config.bossName}: {phaseName}/{attackConfig.name} " +
                           $"CreateAttack() returned null!", this);
            return;
        }

        behaviour.Initialize(this, attackConfig.cooldown, attackConfig.duration);
        behaviour.OnAttackStarted += OnAttackStarted;
        behaviour.OnAttackComplete += OnAttackComplete;

        attacks.Add(behaviour);
    }

    // ── Animation callbacks ───────────────────────────────────────────

    private void OnAttackStarted() => animController?.Attack();
    private void OnAttackComplete() => animController?.FlyNormal();

    // ── Update ────────────────────────────────────────────────────────

    private void Update()
    {
        if (isDead || !isInitialized || _invulnerable) return;

        float dt = Time.deltaTime;
        for (int i = 0; i < attacks.Count; i++)
            attacks[i].Tick(dt);
    }

    // ── Health / enrage / retreat ─────────────────────────────────────

    private void OnHealthChanged(float normalized)
    {
        // Always propagate to listeners (UI bar) so the new value shows even when the
        // boss is invulnerable — Initialize fires this on phase-2 respawn while the
        // entrance tween still has _invulnerable = true.
        BossEventBus.RaiseHealthChanged(normalized);

        // Skip damage-side reactions (retreat/enrage) during entrance/exit tweens.
        if (_invulnerable) return;

        // ── Retreat at 50% HP on non-level-20 encounters; boss returns at level 20
        //    with the same normalized HP (i.e. 50%) on phase-2 max. ──
        if (!_isLevel20 && !_hasRetreated && normalized <= RetreatThreshold)
        {
            _hasRetreated = true;
            Debug.Log($"[BossBird] {config.bossName} RETREATING at {normalized:P0} HP", this);

            // Stop all attacks and movement
            StopAllBehaviours();

            // Fire retreated event — SpawnController listens to this
            BossEventBus.RaiseBossRetreated(config.bossName, normalized);
            return;
        }

        if (!hasEnraged && config.enrageThreshold > 0f && normalized <= config.enrageThreshold)
        {
            hasEnraged = true;
            movement.ApplySpeedMultiplier(config.enrageSpeedMultiplier);
            for (int i = 0; i < attacks.Count; i++)
                attacks[i].ApplyCooldownMultiplier(0.7f);
            BossEventBus.RaiseEnraged();

            Debug.Log($"[BossBird] {config.bossName} ENRAGED at {normalized:P0} HP", this);
        }
    }

    /// <summary>
    /// Stops all attacks and movement. Used during retreat and death.
    /// </summary>
    private void StopAllBehaviours()
    {
        for (int i = 0; i < attacks.Count; i++)
        {
            attacks[i].OnAttackStarted -= OnAttackStarted;
            attacks[i].OnAttackComplete -= OnAttackComplete;
            attacks[i].OnStop();
        }
        movement.Stop();
    }

    private void OnDeath()
    {
        if (isDead) return;
        isDead = true;

        // ── Stop all attacks and clean up ──
        for (int i = 0; i < attacks.Count; i++)
        {
            attacks[i].OnAttackStarted -= OnAttackStarted;
            attacks[i].OnAttackComplete -= OnAttackComplete;
            attacks[i].OnStop();
            attacks[i].OnCleanup();
            Destroy(attacks[i]);
        }
        attacks.Clear();

        movement.Stop();
        animController?.Death();

        if (config.deathVFX != null)
            PoolManager.Get(config.deathVFX, transform.position);

        // ── Notify the rest of the game ──
        BossEventBus.RaiseBossDefeated(config.bossName, config.scoreValue);
        Debug.Log($"[BossBird] {config.bossName} DEFEATED", this);

        // ── NOTE: No longer self-destroying here.
        //    SpawnController handles destruction via the exit animation.
        //    If this is level 20, SpawnController will destroy after exit.
        //    If not level 20, the boss retreats before death is ever called. ──
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
            health.OnDeath -= OnDeath;
        }
    }
}