using UnityEngine;

[CreateAssetMenu(fileName = "CannonStats", menuName = "BirdHunter/Cannon/Stats")]
public class CannonStats : ScriptableObject
{
    [Header("Base Stats")]           // ← Numbers ONLY, no prefabs
    public float baseBulletDamage   = 10f;
    public float baseFireRate       = 1f;
    public float baseBulletSpeed    = 15f;
    public float baseMoveSpeed      = 5f;
    public float baseMaxHealth      = 100f;
    public int   baseMinBulletShot  = 1;
    public int   baseMaxBulletShot  = 1;
    public int   baseBulletBounce   = 0;
    public float baseBulletMass;
    public bool  randomBulletFire;

    [Header("Upgrade Increments")]
    public float damageIncrement    = 1f;
    public float healthIncrement    = 5f;
    public float fireRateIncrement  = 0.05f;

    // ── Runtime values ──
    [HideInInspector] public float bulletDamage;
    [HideInInspector] public float fireRate;
    [HideInInspector] public float bulletSpeed;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float maxHealth;
    [HideInInspector] public int   numberOfMinBulletInOneShot;
    [HideInInspector] public int   numberOfMaxBulletInOneShot;
    [HideInInspector] public int   bulletBounce;

    // ── current* aliases ──
    public float currentBulletDamage            => bulletDamage;
    public float currentFireRate                => fireRate;
    public float currentBulletSpeed             => bulletSpeed;
    public float currentMoveSpeed               => moveSpeed;
    public float currentMaxHealth               => maxHealth;
    public int   currentNumberOfMinBulletInShot => numberOfMinBulletInOneShot;
    public int   currentNumberOfMaxBulletInShot => numberOfMaxBulletInOneShot;

    // ── Multipliers ──
    [HideInInspector] public float damageMultiplier    = 1f;
    [HideInInspector] public float fireRateMultiplier  = 1f;
    [HideInInspector] public float healthMultiplier    = 1f;
    [HideInInspector] public float moveSpeedMultiplier = 1f;

    // ── Base cache ──
    [HideInInspector] public float _baseBulletDamage;
    [HideInInspector] public float _baseFireRate;
    [HideInInspector] public float _baseMaxHealth;
    private bool _baseCached;

    public void InitRuntime()
    {
        bulletDamage               = baseBulletDamage;
        fireRate                   = baseFireRate;
        bulletSpeed                = baseBulletSpeed;
        moveSpeed                  = baseMoveSpeed;
        maxHealth                  = baseMaxHealth;
        numberOfMinBulletInOneShot = baseMinBulletShot;
        numberOfMaxBulletInOneShot = baseMaxBulletShot;
        bulletBounce               = baseBulletBounce;
    }

    public void ApplyProgression(int globalLevel)
    {
        float t = Mathf.Clamp01(globalLevel / 600f);
        damageMultiplier    = 1f + 8.5f * t;
        fireRateMultiplier  = 1f + 4.0f * t;
        healthMultiplier    = 1f + 11f * (1f - Mathf.Exp(-2f * t));
        moveSpeedMultiplier = 1f + 0.5f * t;
        UpdateRuntime();
    }

    public void UpdateRuntime()
    {
        bulletDamage               = baseBulletDamage * damageMultiplier;
        fireRate                   = baseFireRate     * fireRateMultiplier;
        maxHealth                  = baseMaxHealth    * healthMultiplier;
        moveSpeed                  = baseMoveSpeed    * moveSpeedMultiplier;
        bulletSpeed                = baseBulletSpeed;
        numberOfMinBulletInOneShot = baseMinBulletShot;
        numberOfMaxBulletInOneShot = baseMaxBulletShot;
        bulletBounce               = baseBulletBounce;
    }

    public void CacheBaseValues()
    {
        if (_baseCached) return;
        _baseBulletDamage = baseBulletDamage;
        _baseFireRate     = baseFireRate;
        _baseMaxHealth    = baseMaxHealth;
        InitRuntime();
        _baseCached = true;
    }

    public void ResetToBase()
    {
        if (!_baseCached) { CacheBaseValues(); return; }
        baseBulletDamage = _baseBulletDamage;
        baseFireRate     = _baseFireRate;
        baseMaxHealth    = _baseMaxHealth;
        InitRuntime();
    }

    public void Upgrade(int currentLevel)
    {
        baseBulletDamage += damageIncrement;
        baseMaxHealth    += healthIncrement;
        baseFireRate     += fireRateIncrement;
        InitRuntime();
    }
}
