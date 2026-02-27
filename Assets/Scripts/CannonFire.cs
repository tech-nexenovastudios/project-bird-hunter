using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class CannonFire : MonoBehaviour
{
    // ─────────────────────────────────────────
    // References
    // ─────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Transform    GunTipParent;
    [SerializeField] private Transform    bulletContainer;
    [SerializeField] private Transform[]  cannonHead;

    private Transform[]      GunTips;
    private ParticleSystem[] fireParticles;

    // ✅ Set by CannonSpawner from CannonData — single bullet type per cannon
    [HideInInspector] public GameObject bulletPrefab;

    // ─────────────────────────────────────────
    // Stats (set by AssignValue from CannonStats)
    // ─────────────────────────────────────────
    [Header("Stats")]
    [HideInInspector] public float shotsPerSecond  = 1f;
    [HideInInspector] public float bulletSpeed     = 5f;
    [HideInInspector] public float Damage          = 1f;
    [HideInInspector] public float initialDamage   = 1f;
    [HideInInspector] public float damagePercentage = 100f;

    [Header("Idle Damage")]
    public float increaseDamageWhenStand;
    public float idleTime;
    public bool  idleDamageIncrease;

    [Header("Below Health Damage Boost")]
    [HideInInspector] public bool  damageBelowHealth;
    [HideInInspector] public float damageBelowHealthValue;

    [Header("Pierce")]
    [HideInInspector] public bool  chanceOfBulletPierceEnemies;
    [HideInInspector] public float chanceOfBulletPierceEnemiesProbability;

    // ─────────────────────────────────────────
    // Shot Count
    // ─────────────────────────────────────────
    [Header("Shot Count")]
    [HideInInspector] public int  numberOfMinBulletInShot = 1;
    [HideInInspector] public int  numberOfMaxBulletInShot = 1;
    private int                   numberOfBulletInShot    = 1;  // ✅ declared
    private int                   bulletCount             = 1;
    public int                   bulletCountInShot       = 1;
    private bool                  randomBulletFire;

    // ─────────────────────────────────────────
    // Side Bullet
    // ─────────────────────────────────────────
    [Header("Side Bullet")]
    public bool  SideBullet;
    public float sideBulletDamage;

    // ─────────────────────────────────────────
    // Bounce & Effects
    // ─────────────────────────────────────────
    [Header("Bullet Properties")]
    [HideInInspector] public int   bulletBounce;
    [HideInInspector] public bool  electricEffect;
    [HideInInspector] public float electricEffectDuration;
    [HideInInspector] public bool  igniteEffect;
    [HideInInspector] public float igniteEffectDuration;

    // ─────────────────────────────────────────
    // Big Bullet
    // ─────────────────────────────────────────
    [Header("Big Bullet")]
    [SerializeField] private GameObject bigBulletPrefab;
    public float ChargingTime;
    public bool  ActiveBigBullet;
    public float bigBulletDamage;
    public float coolDownTimer;

    // ─────────────────────────────────────────
    // Hit Damage Increase
    // ─────────────────────────────────────────
    [HideInInspector] public bool  increaseDamageAfterHit;
    [HideInInspector] public bool  alreadyIncreaseDamage;
    [HideInInspector] public float increaseHitDamage;
    [HideInInspector] public float increaseHitDuration;

    // ─────────────────────────────────────────
    // Cannon Stats SO
    // ─────────────────────────────────────────
    public CannonStats cannonStats;

    // ─────────────────────────────────────────
    // Internal state
    // ─────────────────────────────────────────
    internal float     fireDelayBetweenBullet;
    private Coroutine fireCoroutine;

    // Bullet pool key (one type per cannon)
    private BulletType bulletKey;

    public static Action startFireCallBack;
    public static Action stopFireCallBack;

    // ─────────────────────────────────────────
    // Unity Events
    // ─────────────────────────────────────────
    private void Start()
    {
        // AssignValue already called by CannonSpawner before Start()
        // This is a safety fallback for editor/testing
        if (cannonStats != null && GunTips == null && bulletPrefab != null)
            AssignValue();

        //StartFiring();
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            idleTime           = 0f;
            idleDamageIncrease = false;
        }
        else
        {
            idleTime += Time.deltaTime;
            idleDamageIncrease = idleTime >= 3f;
        }
    }

    private void OnEnable()
    {
        CannonHealth.cannonHitCallBack += DamageIncreaseWhenHit;
    }

    private void OnDisable()
    {
        CannonHealth.cannonHitCallBack -= DamageIncreaseWhenHit;
        GamePoolManager.bulletQueue.Clear();
        GamePoolManager.bulletDamageTextQueue.Clear();
    }

    // ─────────────────────────────────────────
    // AssignValue — called by CannonSpawner
    // ─────────────────────────────────────────
    public CannonFire AssignValue()
    {
        // ── Validate ──
        if (GunTipParent == null)
        {
            Debug.LogError("[CannonFire] GunTipParent not assigned on: " + gameObject.name);
            return this;
        }
        if (cannonStats == null)
        {
            Debug.LogError("[CannonFire] CannonStats is null on: " + gameObject.name);
            return this;
        }
        if (bulletPrefab == null)
        {
            Debug.LogError("[CannonFire] bulletPrefab is null! Assign it in CannonData.");
            return this;
        }

        // ── Resolve GunTips + particles ──
        GunTips       = GunTipParent.Cast<Transform>().ToArray();
        fireParticles = GunTipParent.GetComponentsInChildren<ParticleSystem>();
        
        

        if (GunTips.Length == 0)
        {
            Debug.LogError("[CannonFire] No GunTip children found under GunTipParent!");
            return this;
        }

        // ── Pull stats from CannonStats ──
        Damage                    = cannonStats.bulletDamage;
        initialDamage             = cannonStats.bulletDamage;
        shotsPerSecond            = cannonStats.fireRate;
        bulletSpeed               = cannonStats.bulletSpeed;
        numberOfMinBulletInShot   = cannonStats.numberOfMinBulletInOneShot;
        numberOfMaxBulletInShot   = cannonStats.numberOfMaxBulletInOneShot;
        numberOfBulletInShot      = numberOfMaxBulletInShot;
        bulletCount               = numberOfMaxBulletInShot;
        bulletCountInShot         = 1;
        bulletBounce              = cannonStats.bulletBounce;
        randomBulletFire          = cannonStats.randomBulletFire;
        damagePercentage          = 100f;

        // ── Cache bullet pool key ──
        var b = bulletPrefab.GetComponent<Bullet>();
        if (b != null)
            bulletKey = b.type;
        else
            Debug.LogWarning("[CannonFire] bulletPrefab has no Bullet component!");

        // ── Fire delay between each bullet in a burst ──
        fireDelayBetweenBullet = (shotsPerSecond > 0 && numberOfMaxBulletInShot > 0)
            ? 1f / (shotsPerSecond * numberOfMaxBulletInShot)
            : 0.1f;

        return this;
    }

    // ─────────────────────────────────────────
    // Fire Control
    // ─────────────────────────────────────────
    public void StartFiring()
    {
        if (fireCoroutine != null) return;
        fireCoroutine = StartCoroutine(FireLoop());
        startFireCallBack?.Invoke();
    }

    public void StopFiring()
    {
        if (fireCoroutine == null) return;
        StopCoroutine(fireCoroutine);
        fireCoroutine = null;
        stopFireCallBack?.Invoke();
    }

    // ─────────────────────────────────────────
    // FireLoop Coroutine
    // ─────────────────────────────────────────
    private IEnumerator FireLoop()
    {
        // Wait until setup is complete
        yield return new WaitUntil(() =>
            bulletPrefab != null &&
            GunTips      != null &&
            GunTips.Length > 0);

        while (true)
        {
            if (!ActiveBigBullet)
            {
                // ── Decide how many bullets this shot ──
                if (randomBulletFire)
                {
                    int r = Random.Range(0, 100);
                    bulletCount = r > 90 ? numberOfMaxBulletInShot
                                : r > 60 ? Mathf.Max(1, numberOfMaxBulletInShot / 2)
                                         : numberOfMinBulletInShot;
                }
                else
                {
                    bulletCount = Random.Range(numberOfMinBulletInShot, numberOfMaxBulletInShot + 1);
                }

                numberOfBulletInShot = bulletCount;

                // ── Side bullets (fixed 2 angled shots) ──
                if (SideBullet)
                {
                    SpawnBullet(GunTips[0].position, Quaternion.Euler(0, 0, -45f),
                        Damage + sideBulletDamage / 100f);
                    SpawnBullet(GunTips[0].position, Quaternion.Euler(0, 0,  45f),
                        Damage + sideBulletDamage / 100f);
                }

                // ── Main burst ──
                for (int i = 0; i < bulletCount; i++)
                {
                    // Clamp tip index so cannons with fewer tips still work
                    int tipIndex = Mathf.Clamp(i, 0, GunTips.Length - 1);

                    // Cannon head squish animation
                    if (cannonHead != null && tipIndex < cannonHead.Length && cannonHead[tipIndex] != null)
                    {
                        var ch = cannonHead[tipIndex];
                        Vector2 cs = ch.transform.localScale;
                        ch.DOScale(new Vector2(cs.x, cs.y * 0.9f), 0.05f)
                          .OnComplete(() => ch.DOScale(cs, 0.05f).SetEase(Ease.InOutCubic));
                    }

                    yield return new WaitForSeconds(0.05f);

                    // Damage calculation
                    float dmgCalc = Damage * damagePercentage / 100f;
                    float damage  = dmgCalc + (idleDamageIncrease ? increaseDamageWhenStand / 100f : 0f);

                    // Spawn bullet(s) per tip
                    for (int j = 0; j < bulletCountInShot; j++)
                    {
                        SpawnBullet(GunTips[tipIndex].position, Quaternion.identity, damage);
                    }

                    // Play muzzle particle
                    if (fireParticles != null && tipIndex < fireParticles.Length && fireParticles[tipIndex] != null)
                        fireParticles[tipIndex].Play();

                    yield return new WaitForSeconds(fireDelayBetweenBullet);
                }

                // Wait before next full shot
                float shotInterval = shotsPerSecond > 0 ? 1f / shotsPerSecond : 1f;
                yield return new WaitForSeconds(shotInterval);
            }
            else
            {
                // ── Big Bullet mode ──
                float charge = ChargingTime;
                while (charge > 0f)
                {
                    charge -= Time.deltaTime;
                    yield return null;
                }

                if (bigBulletPrefab != null && GunTips.Length > 0)
                {
                    var go = Instantiate(bigBulletPrefab,
                        GunTips[0].position, Quaternion.identity, bulletContainer);
                    var bb = go.GetComponent<BigBullet>();
                    if (bb != null) bb.Damage = bigBulletDamage;
                }

                yield return new WaitForSeconds(coolDownTimer);
                ActiveBigBullet = false;
            }
        }
    }

    // ─────────────────────────────────────────
    // Spawn Bullet (pool-aware, single prefab)
    // ─────────────────────────────────────────
    private void SpawnBullet(Vector3 pos, Quaternion rot, float damage)
    {
        if (bulletPrefab == null) return;

        GameObject go;

        bool hasPooled = GamePoolManager.bulletQueue.ContainsKey(bulletKey)
                      && GamePoolManager.bulletQueue[bulletKey].Count > 0;

        if (hasPooled)
        {
            go = GamePoolManager.bulletQueue[bulletKey].Dequeue();
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(bulletPrefab, pos, rot, bulletContainer);
        }

        var bullet = go.GetComponent<Bullet>();
        if (bullet != null)
            BulletValueAssign(bullet, damage);
    }

    // ─────────────────────────────────────────
    // Bullet value setup
    // ─────────────────────────────────────────
    public void BulletValueAssign(Bullet bullet, float damage)
    {
        bullet.cannonFire   = this;
        bullet.bulletSpeed  = bulletSpeed;
        bullet.bulletBounce = bulletBounce;

        bullet.chanceOfBulletPierceEnemies            = chanceOfBulletPierceEnemies;
        bullet.chanceOfBulletPierceEnemiesProbability = chanceOfBulletPierceEnemiesProbability;

        bullet.Damage = damageBelowHealth
            ? (damage < Damage * damageBelowHealthValue / 100f ? damage : Damage)
            : damage;

        bullet.electricEffect         = electricEffect;
        bullet.electricEffectDuration = electricEffectDuration;
        bullet.IgniteEffect           = igniteEffect;
        bullet.igniteEffectDuration   = igniteEffectDuration;

        Destroy(bullet.gameObject, 5f);
    }

    // ─────────────────────────────────────────
    // Damage Text pool return
    // ─────────────────────────────────────────
    public void DestroyText(GameObject go) =>
        StartCoroutine(DestroyTextCoroutine(go));

    private IEnumerator DestroyTextCoroutine(GameObject go)
    {
        yield return new WaitForSeconds(2f);
        GamePoolManager.bulletDamageTextQueue.Enqueue(go);
        go.SetActive(false);
        go.GetComponent<TextMeshPro>().alpha = 1f;
    }

    // ─────────────────────────────────────────
    // Hit damage boost
    // ─────────────────────────────────────────
    public void DamageIncreaseWhenHit()
    {
        if (!increaseDamageAfterHit) return;
        StartCoroutine(DamageIncreaseRoutine());
    }

    private IEnumerator DamageIncreaseRoutine()
    {
        if (alreadyIncreaseDamage) yield break;
        alreadyIncreaseDamage = true;
        Damage += increaseHitDamage;
        yield return new WaitForSeconds(increaseHitDuration);
        Damage                = initialDamage;
        alreadyIncreaseDamage = false;
    }
}
