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

    // Base max health rolled from config.maxHealthRange once per spawn. Phase 1 and the
    // phase-2 re-init share this value so the boss's HP is consistent across the fight.
    private float _baseMaxHealth = -1f;

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
        Debug.Log($"[BossBird][DBG] Start() — directTestConfig={(directTestConfig != null ? directTestConfig.bossName : "NULL")}, " +
                  $"testAsLevel20={testAsLevel20}, isInitialized={isInitialized}", this);

        if (directTestConfig != null && !isInitialized)
        {
            Debug.Log($"[BossBird] AUTO-INIT from Inspector: {directTestConfig.bossName} (testAsLevel20={testAsLevel20})", this);
            Initialize(directTestConfig, testAsLevel20);
        }
        else
        {
            Debug.Log("[BossBird][DBG] Start() did NOT auto-init — boss will be initialized by SpawnController " +
                      "(testAsLevel20 is IGNORED on that path; isLevel20 comes from the level number).", this);
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

        // Roll the boss's max health once for this spawn; reused by the phase-2 re-init.
        _baseMaxHealth = config.RollMaxHealth();

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
        // Phase 1: full HP. Direct level-20 spawn (test mode): same max as phase 1, starting
        // at the post-retreat return mark so the boss matches the "returns partially healed" form.
        float maxHp = _baseMaxHealth;
        float startHp = isLevel20 ? _baseMaxHealth * config.returnHealthPercent : maxHp;
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
        Debug.Log($"[BossBird][DBG] Initialize — isLevel20={isLevel20} | " +
                  $"phase1Attack={(config.phase1Attack != null ? config.phase1Attack.name : "NULL")} | " +
                  $"phase2Attack={(config.phase2Attack != null ? config.phase2Attack.name : "NULL")} | " +
                  $"destroying {existingAttacks.Length} pre-existing attack component(s).", this);
        for (int i = 0; i < existingAttacks.Length; i++)
            Destroy(existingAttacks[i]);
        attacks.Clear();

        SpawnAttack(config.phase1Attack, "Phase1");
        if (isLevel20)
            SpawnAttack(config.phase2Attack, "Phase2");
        else
            Debug.LogWarning($"[BossBird][DBG] isLevel20=false → phase2Attack " +
                             $"({(config.phase2Attack != null ? config.phase2Attack.name : "NULL")}) NOT spawned. " +
                             "This is why a phase-2 attack like Hellfire Dive won't appear.", this);

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

        // ---- Health: phase 2 reuses the SAME max health rolled in phase 1 (no re-roll), and
        //              the boss returns partially healed at config.returnHealthPercent of that
        //              max, regardless of the exact HP it had when it retreated. ----
        health = GetComponent<BossHealthHandler>();
        // Reuse the value rolled in phase-1 Initialize; roll now only as a fallback if that never ran.
        if (_baseMaxHealth < 0f) _baseMaxHealth = config.RollMaxHealth();
        float phase2MaxHp = _baseMaxHealth;
        float startHp = phase2MaxHp * config.returnHealthPercent;
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
                  $"HP:{startHp:F0}/{phase2MaxHp:F0} (returned at {config.returnHealthPercent:P0}, " +
                  $"parked at {remainingHpNormalized:P0}) | Attacks:{attacks.Count}", this);
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
        Debug.Log($"[BossBird][DBG] Spawned {phaseName} attack '{attackConfig.name}' → {behaviour.GetType().Name} " +
                  $"(cooldown={attackConfig.cooldown}s, first fire after {attackConfig.cooldown}s). Total attacks now {attacks.Count}.", this);
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

            // Boss isn't dead yet (it returns at level 20) — play the looping fly animation
            // so it flees alive during the exit tween, never the death animation.
            animController?.FlyNormal();

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

        // A phase-1 mid-boss (L10) retreats at 50% and must never resolve as a kill.
        // If a single overshooting hit drives currentHealth below 0, BossHealthHandler
        // fires OnHealthChanged (→ retreat) and the currentHealth<=0 death check in the
        // same TakeDamage call. Ignore the death so retreat and death can't both fire.
        if (_hasRetreated && !_isLevel20) return;

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