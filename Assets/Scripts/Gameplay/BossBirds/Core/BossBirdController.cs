using UnityEngine;
using System.Collections.Generic;

public class BossBirdController : MonoBehaviour
{
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

    private void Start()
    {
        if (directTestConfig != null && !isInitialized)
        {
            Debug.Log($"[BossBird] AUTO-INIT from Inspector: {directTestConfig.bossName}", this);
            Initialize(directTestConfig, testAsLevel20);
        }
    }

    public void Initialize(BossBirdConfig cfg, bool isLevel20)
    {
        config = cfg;
        isDead = false;
        isInitialized = false;
        hasEnraged = false;

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
        float hp = isLevel20
            ? config.maxHealth * config.phase2HealthMultiplier
            : config.maxHealth;
        health.Initialize(hp, config.enrageThreshold);
        health.OnHealthChanged += OnHealthChanged;
        health.OnDeath += OnDeath;

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
                  $"HP:{hp} | Attacks:{attacks.Count} | " +
                  $"Phase:{(isLevel20 ? "1+2" : "1")}", this);
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

    private void OnAttackStarted()
    {
        animController?.Attack();
    }

    private void OnAttackComplete()
    {
        animController?.FlyNormal();
    }

    // ── Health / enrage ───────────────────────────────────────────────

    private void Update()
    {
        if (isDead || !isInitialized) return;

        float dt = Time.deltaTime;
        for (int i = 0; i < attacks.Count; i++)
            attacks[i].Tick(dt);
    }

    private void OnHealthChanged(float normalized)
    {
        BossEventBus.RaiseHealthChanged(normalized);

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

    private void OnDeath()
    {
        if (isDead) return;
        isDead = true;

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

        BossEventBus.RaiseBossDefeated(config.bossName, config.scoreValue);
        Debug.Log($"[BossBird] {config.bossName} DEFEATED", this);
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