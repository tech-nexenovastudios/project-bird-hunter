using Gameplay.Player;
using UnityEngine;

public enum CannonSpecial
{
    None,
    ShotgunSpread,  // Shotgun Cannon
    CritChance,     // Lucky Cannon
    AoEExplosion,   // Big Bartha
    SplitOnHit      // Double / Triple
}

[CreateAssetMenu(fileName = "CannonStats", menuName = "BirdHunter/Cannon/Stats")]
public class CannonStats : ScriptableObject
{
    // ─────────────────────────────────────────
    // Cannon-only base stats
    // ─────────────────────────────────────────
    [Header("Cannon Base Stats")]
    public float baseFireRate    = 1f;
    public float baseMoveSpeed   = 5f;
    public float baseMaxHealth   = 100f;
    public bool  randomBulletFire;
    public int   baseMinBulletShot = 1;
    public int   baseMaxBulletShot = 1;

    [Header("Bullet Config")]
    public BulletConfig baseBulletConfig;   // ← all bullet values live here

    [Header("Special")]
    public CannonSpecial specialEffect;

    [Header("Upgrade Increments")]
    public float healthIncrement   = 5f;
    public float fireRateIncrement = 0.05f;
    public float moveSpeedIncrement = 0.1f;

    [Header("Upgrade Limits")]
    public float maxFireRate  = 10f;
    public float maxMoveSpeed = 12f;

    // ─────────────────────────────────────────
    // Runtime values (cannon only)
    // ─────────────────────────────────────────
    [HideInInspector] public float fireRate;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float maxHealth;
    [HideInInspector] public int   numberOfMinBulletInOneShot;
    [HideInInspector] public int   numberOfMaxBulletInOneShot;

    // ─────────────────────────────────────────
    // current* aliases
    // ─────────────────────────────────────────
    public float currentFireRate    => fireRate;
    public float currentMoveSpeed   => moveSpeed;
    public float currentMaxHealth   => maxHealth;
    public int   currentNumberOfMinBulletInShot => numberOfMinBulletInOneShot;
    public int   currentNumberOfMaxBulletInShot => numberOfMaxBulletInOneShot;

    // ─────────────────────────────────────────
    // Multipliers (cannon level only)
    // ─────────────────────────────────────────
    [HideInInspector] public float fireRateMultiplier  = 1f;
    [HideInInspector] public float healthMultiplier    = 1f;
    [HideInInspector] public float moveSpeedMultiplier = 1f;

    // ─────────────────────────────────────────
    // Base cache
    // ─────────────────────────────────────────
    [HideInInspector] public float _baseFireRate;
    [HideInInspector] public float _baseMaxHealth;
    private bool _baseCached;

    // ─────────────────────────────────────────
    // Init / Progression / Upgrade
    // ─────────────────────────────────────────
    public void InitRuntime()
    {
        fireRate                    = baseFireRate;
        moveSpeed                   = baseMoveSpeed;
        maxHealth                   = baseMaxHealth;
        numberOfMinBulletInOneShot  = baseMinBulletShot;
        numberOfMaxBulletInOneShot  = baseMaxBulletShot;
    }

    public void ApplyProgression(int globalLevel)
    {
        float t = Mathf.Clamp01(globalLevel / 600f);
        fireRateMultiplier  = 1f + 4.0f * t;
        healthMultiplier    = 1f + 11f  * (1f - Mathf.Exp(-2f * t));
        moveSpeedMultiplier = 1f + 0.5f * t;
        UpdateRuntime();

        // Also scale bullet config at runtime
        baseBulletConfig?.ApplyProgression(globalLevel);
    }

    public void UpdateRuntime()
    {
        fireRate  = Mathf.Min(baseFireRate  * fireRateMultiplier,  maxFireRate);
        moveSpeed = Mathf.Min(baseMoveSpeed * moveSpeedMultiplier, maxMoveSpeed);
        maxHealth = baseMaxHealth * healthMultiplier;
        numberOfMinBulletInOneShot = baseMinBulletShot;
        numberOfMaxBulletInOneShot = baseMaxBulletShot;
    }

    public void CacheBaseValues()
    {
        if (_baseCached) return;
        _baseFireRate  = baseFireRate;
        _baseMaxHealth = baseMaxHealth;
        InitRuntime();
        _baseCached = true;
    }

    public void ResetToBase()
    {
        if (!_baseCached) { CacheBaseValues(); return; }
        baseFireRate  = _baseFireRate;
        baseMaxHealth = _baseMaxHealth;
        InitRuntime();
        baseBulletConfig?.ResetToBase();
    }

    public void Upgrade(int currentLevel)
    {
        baseMaxHealth = baseMaxHealth + healthIncrement;
        baseFireRate  = Mathf.Min(baseFireRate + fireRateIncrement, maxFireRate);
        baseMoveSpeed = Mathf.Min(baseMoveSpeed + moveSpeedIncrement, maxMoveSpeed);
        InitRuntime();

        // Bullet upgrades go through BulletConfig
        baseBulletConfig?.Upgrade();
    }
}
