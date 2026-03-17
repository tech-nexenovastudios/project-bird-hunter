using DG.Tweening;
using System;
using UnityEngine;


//This class contains all type fo cannon power logic which appear inside slot machine
public class CannonPower : MonoBehaviour
{

    public static CannonPower instance;

    public float defaultSize;
    public float decreaseSize;

    [Header("Editor Only")]
    public bool electricBulletEffect;
    public bool igniteFireEffect;
    public bool laserCheck;
    public bool sideFire;
    public bool Invincible;
    public bool destroyDamageObj;
    public bool ChanceToHealKillBird;
    public bool swordCheck;
    public bool damageIncreaseBelowHealth;
    public float swordDamage;
    public float swordDelay;
    public float swordSpeed;
    public float laserDelayTime;
    public float probabilityToGetBackHealth;
    public bool chanceOfBulletPierceEnemies;
    public float chanceOfBulletPierceEnemiesProbability;

    [Header("Bullet")]
    public int numberOfBullets;
    public float bulletSpeed;
    public int bulletDamage;

    public GameObject laserObj;
    Laser laser;
    public int laserDamage;

    [Header("Sword")]
    public Transform swordHolder;
    public GameObject swordPrefab;
    Sword sword;

    [Header("CannonRef")]
    public CannonHealthOld health;
    public CannonFire cannonFire;
    public CannonMove cannonMove;


    [Header("FreezeThrower")]
    [SerializeField] Transform freezeHolder;
    [SerializeField] GameObject freezePrefab;
    [SerializeField] bool freezeCheck;
    FreezeThrower freezeThrower;

    [Header("PoisonThrower")]
    [SerializeField] Transform poisonHolder;
    [SerializeField] GameObject poisonPrefab;
    [SerializeField] bool poisonCheck;
    PoisonThrower poisonThrower;

    [Header("RocketThrower")]
    [SerializeField] Transform rocketHolder;
    [SerializeField] GameObject rocketPrefab;
    [SerializeField] bool rocketCheck;
    RocketThrower rocketThrower;

    public bool increaseDamageAfterHit;
    public float increaseHitDamage;
    public float increaseHitDuration;


    public bool ThunderSpellFlaskPower;


    public bool getBackHealth;

    public bool reduceGravity;
    public Vector2 changeVelocity;

    public GameObject miniCannonPrefab;
    private GameObject miniCannonRef;


    [SerializeField] GameObject forceFieldPrefab;
    GameObject forceFieldRef;


    [SerializeField] GameObject steelSpikeObj;
    GameObject steelSpikeRef;

    public GameObject barrier;

    public Transform orbHolder;
    public Transform orbParent;
    Transform orbParentRef;
    public GameObject poisonOrb;
    public GameObject freezingOrb;
    public GameObject fireOrb;

    GameObject poisonOrbRef;
    GameObject freezingOrbRef;
    GameObject fireOrbRef;

    public Transform shadowCannonHolder;
    public GameObject shadowCannon;
    GameObject shadowCannonRef;


    public static Action<float> sizeDecreaseCallBack;

    float currentCannonSize = 1;


    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        /*      SwordEquip(swordCheck, swordDamage, swordDelay, swordSpeed);
              ElectricBullet(electricBulletEffect);
              IgniteBullet(igniteFireEffect);
              DecreaseSize(decreaseSize);
              SideFire(sideFire);
              LaserEquip(laserCheck, laserDamage, laserDelayTime);
              InvincibleCannon(Invincible);
              
              AttackPowerIncrease(bulletDamage);
              FireSpeed(bulletSpeed);
              DestroyDamageObj(destroyDamageObj);
              GetBackHealth(getBackHealth,5f);
              DamageIncreaseBelowHealth(damageIncreaseBelowHealth);
              ChanceOfBulletPierceEnemies(chanceOfBulletPierceEnemies, chanceOfBulletPierceEnemiesProbability);
              PoisonEquip(poisonCheck);
              FreezeEquip(freezeCheck);
              IncreaseDamageAfterHit(increaseDamageAfterHit, increaseHitDamage, increaseHitDuration);*/
        //WheelFire(true, 1f, 3f, 5f);
        //FreeMove(true);
        //miniCannon();
        //BigBullet(true,5,1,0);
        //DamageObj(true, 40);
        //revivePlayer(true, 40);

        //ShadowCannon(5);
        //ShadowCannon(50);

        // SteelSpike(50);


    }

    // Equip laser weapon on the side of the cannon with given damage and delay settings.
    public void LaserEquip(bool laserCheck, float laserDamage, float laserDelayTime, float duration)
    {
        if (laserCheck)
        {
            if (laser == null)
            {
                laser = Instantiate(laserObj, transform).GetComponentInChildren<Laser>();
            }

            // Scale the laser based on the cannon�s current size.
            if (currentCannonSize != 1)
            {
                float scalePercentage = (1 - (currentCannonSize / 1));
                Debug.Log(scalePercentage);
                laser.scale = 200 * (1 / scalePercentage);
            }
            else
            {
                laser.scale = 200;
            }

            laser.damage = laserDamage;
            laser.laserDelay = laserDelayTime;
            laser.duration = duration;
            laser.cannonFire = cannonFire;
        }
    }

    // Instantly restore a certain amount of current HP.
    public void RestoreHP(float hp)
    {
        health.currentHealth = Mathf.Clamp(health.currentHealth + ((health.maxHealth * hp) / 100f), 0, health.maxHealth);
    }

    // Increase the player's maximum HP permanently.
    public void UpdateMaxHP(float hp)
    {
        health.maxHealth += hp;
    }

    // Reduce the player's size (used for dodging or fitting in tight spaces).
    public void DecreaseSize(float DecreaseSize)
    {
        currentCannonSize = DecreaseSize;
        transform.DOScale(Vector3.one * DecreaseSize, 0.2f);
        sizeDecreaseCallBack?.Invoke(DecreaseSize);
    }

    // Enable side bullets to fire at 45 and -45 degrees.
    public void SideFire(bool sideFire, float damage)
    {
        cannonFire.SideBullet = sideFire;
        cannonFire.sideBulletDamage = damage;
    }

    // Enable or disable electric effect on bullets.
    public void ElectricBullet(bool ElectricEffect, float duration)
    {
        cannonFire.electricEffect = ElectricEffect;
        cannonFire.electricEffectDuration = duration;

    }

    // Enable or disable ignite effect on bullets (causes burn damage).
    public void IgniteBullet(bool IgniteBullet, float duration)
    {
        cannonFire.igniteEffect = IgniteBullet;
        cannonFire.igniteEffectDuration = duration;
    }

    // Make the player invincible to the next hit.
    public void InvincibleCannon(bool Invincible, float duration)
    {
        health.invincible = Invincible;
        health.invincibleDuration = duration;
    }

    public void InvincibleForHits(float count)
    {
        health.maxInvincibleCount = count;
        health.currentInvincibleCount = count;
        health.InvincibleEffect();
    }

    // Add extra bullets per shot (multi-shot power).
    public void NumberOfBulletInPerShot(int numberOfBullets, float damage)
    {
        cannonFire.bulletCountInShot = numberOfBullets;
        cannonFire.damagePercentage = damage;
    }

    // Increase the base damage of each bullet.
    public void AttackPowerIncrease(float power)
    {
        cannonFire.Damage += ((cannonFire.Damage * power) / 100);
    }

    // Increase the speed at which bullets travel.
    public void FireSpeed(float fireSpeed)
    {
        cannonFire.bulletSpeed += fireSpeed;
    }

    // Destroy any object that collides with the player (like enemy projectiles).
    public void DestroyDamageObj(bool destroyObj)
    {
        health.destroyDamageObj = destroyObj;
    }

    public void DamageObj(bool damageObj, float damageAmount)
    {
        health.damageObject = damageObj;
        health.damageValueWhenHit = damageAmount;
    }


    // Equip a rotating sword that attacks nearby enemies on a cooldown.
    public void SwordEquip(bool swordCheck, float Damage, float DelayTime, float swordSpeed)
    {
        if (swordCheck)
        {
            if (sword == null)
            {
                sword = Instantiate(swordPrefab, swordHolder).GetComponent<Sword>();
                sword.holder = swordHolder;
                sword.cannonFire = cannonFire;
            }

            sword.speed = swordSpeed;
            sword.damage = Damage;
            sword.delayTime = DelayTime;
        }
    }

    // Enable health regeneration after killing a bird with a certain probability.
    public void GetBackHealth(bool getBackHealth, float probabilityToGetBackHealth)
    {
        this.getBackHealth = getBackHealth;
        this.probabilityToGetBackHealth = probabilityToGetBackHealth;
    }

    // When HP is below a certain threshold, increase bullet damage (clutch mode).
    public void DamageIncreaseBelowHealth(bool Increase, float value)
    {
        cannonFire.damageBelowHealth = Increase;
        cannonFire.damageBelowHealthValue = value;
    }

    // Enable chance for bullets to pierce through multiple enemies.
    public void ChanceOfBulletPierceEnemies(bool chance, float probability)
    {
        cannonFire.chanceOfBulletPierceEnemies = chance;
        cannonFire.chanceOfBulletPierceEnemiesProbability = probability;
    }

    // Equip poison bullet thrower which throws poison bullets periodically.
    public void PoisonEquip(bool poisonCheck)
    {
        if (poisonCheck)
        {
            if (poisonThrower == null)
            {
                poisonThrower = Instantiate(poisonPrefab, poisonHolder).GetComponent<PoisonThrower>();
            }
            poisonThrower.transform.localPosition = Vector3.zero;
        }
    }

    // Equip freeze bullet thrower that freezes enemy movement on hit.
    public void FreezeEquip(bool freezeCheck)
    {
        if (freezeCheck)
        {
            if (freezeThrower == null)
            {
                freezeThrower = Instantiate(freezePrefab, freezeHolder).GetComponent<FreezeThrower>();
            }
            freezeThrower.transform.localPosition = Vector3.zero;
        }
    }

    // Temporarily increases bullet damage for a few seconds after the player takes damage.
    public void IncreaseDamageAfterHit(bool check, float increaseDamage, float duration)
    {
        cannonFire.increaseDamageAfterHit = check;
        cannonFire.increaseHitDamage = increaseDamage;
        cannonFire.increaseHitDuration = duration;
    }


    public void WheelFire(bool fire, float damage, float duration, float destroyTime)
    {
        cannonMove.firePower = fire;
        cannonMove.fireDamage = damage;
        cannonMove.fireDamageDuration = duration;
        cannonMove.fireDestroyTime = destroyTime;
    }

    public void BulletBounce(int numberOfBounce)
    {
        cannonFire.bulletBounce = numberOfBounce;
    }

    public void FreeMove(bool freeMove)
    {
        cannonMove.FreeMove = freeMove;
    }

    public void BigBullet(bool activeBullet, float damage, float chargingTime, float coolDownTimer)
    {
        cannonFire.ActiveBigBullet = activeBullet;
        cannonFire.bigBulletDamage = damage;
        cannonFire.ChargingTime = chargingTime;
        cannonFire.coolDownTimer = coolDownTimer;
    }

    public void miniCannon()
    {
        if (miniCannonRef == null)
        {
            miniCannonRef = Instantiate(miniCannonPrefab);
        }
        miniCannonRef.GetComponent<MiniCannonFire>().damage = cannonFire.Damage;
        miniCannonRef.GetComponent<MiniCannonFire>().bulletSpeed = cannonFire.bulletSpeed;
        miniCannonRef.GetComponent<MiniCannonFire>().shotsPerSecond = cannonFire.shotsPerSecond;
        miniCannonRef.GetComponent<MiniCannonHealth>().health = health.maxHealth / 4;
    }


    public void ForceField()
    {
        if (forceFieldRef == null)
        {
            forceFieldRef = Instantiate(forceFieldPrefab);
        }
    }

    public void SteelSpike(float activeArea)
    {
        if (steelSpikeRef == null)
        {
            steelSpikeRef = Instantiate(steelSpikeObj);
        }
        float childCount = steelSpikeRef.transform.childCount;
        for (int i = 0; i < Mathf.RoundToInt((childCount * activeArea) / 100); i++)
        {
            int RandomNum = Mathf.RoundToInt(UnityEngine.Random.Range(0, childCount));
            if (!steelSpikeRef.transform.GetChild(RandomNum).gameObject.activeSelf)
            {
                steelSpikeRef.transform.GetChild(RandomNum).gameObject.SetActive(true);
            }
            else
            {
                while (steelSpikeRef.transform.GetChild(RandomNum).gameObject.activeSelf)
                {
                    RandomNum = Mathf.RoundToInt(UnityEngine.Random.Range(0, childCount));
                }
                steelSpikeRef.transform.GetChild(RandomNum).gameObject.SetActive(true);
            }
        }
        steelSpikeRef.SetActive(true);
    }

    public void RocketEquip(bool rocketCheck, float damage, float interval)
    {
        if (rocketCheck)
        {
            if (rocketThrower == null)
            {
                rocketThrower = Instantiate(rocketPrefab, rocketHolder).GetComponent<RocketThrower>();
            }
            rocketThrower.transform.localPosition = Vector3.zero;
            rocketThrower.Damage = damage;
            rocketThrower.DelayTime = interval;
            rocketThrower.cannonFire = cannonFire;
        }
    }

    public void fullHealBeforeBossBattle(bool heal, float healValue)
    {
        health.fullHealBeforeBoss = heal;
        health.healBeforeBossBattleValue = healValue;
    }

    public void standingStillIncreaseDamage(float damage, float duration)
    {
        cannonFire.increaseDamageWhenStand = damage;
    }

    public void revivePlayer(bool revive, float reviveValue)
    {
        health.revivePlayer = revive;
        health.revivePlayerHealth = reviveValue;
    }
    public void OrbHolderCheck()
    {
        if (orbParentRef == null)
        {
            orbParentRef = Instantiate(orbParent, orbHolder);
            orbParentRef.GetComponent<OrbRotation>().parent = orbHolder;
            orbParentRef.transform.localPosition = Vector3.zero;
            //float scalePercentage = (1 - (currentCannonSize / 1));
            //orbParentRef.transform.localScale -= (orbParentRef.transform.localScale * scalePercentage);
            //orbParentRef.transform.GlobalScale(orbParentRef.transform.localScale);
        }
    }
    public void PoisonOrb(float duration, float damage)
    {
        OrbHolderCheck();
        if (poisonOrbRef == null)
        {
            poisonOrbRef = Instantiate(poisonOrb, orbParentRef);
            poisonOrbRef.GetComponent<OrbParent>().cannonFire = cannonFire;
        }
        poisonOrbRef.GetComponent<OrbParent>().AssignValue(duration, damage);
    }
    public void FreezeOrb(float duration, float damage)
    {
        OrbHolderCheck();
        if (freezingOrbRef == null)
        {
            freezingOrbRef = Instantiate(freezingOrb, orbParentRef);
            freezingOrbRef.GetComponent<OrbParent>().cannonFire = cannonFire;
        }
        freezingOrbRef.GetComponent<OrbParent>().AssignValue(duration, damage);
    }
    public void FireOrb(float duration, float damage)
    {
        OrbHolderCheck();
        if (fireOrbRef == null)
        {
            fireOrbRef = Instantiate(fireOrb, orbParentRef);
            fireOrbRef.GetComponent<OrbParent>().cannonFire = cannonFire;
        }
        fireOrbRef.GetComponent<OrbParent>().AssignValue(duration, damage);
    }

    public void ShadowCannon(float damage)
    {
        if (shadowCannonRef == null)
        {
            shadowCannonRef = Instantiate(shadowCannon, shadowCannonHolder);
            shadowCannonRef.GetComponent<ShadowCannonFire>().cannonFire = cannonFire;
            shadowCannonRef.GetComponent<ShadowCannonMove>().cannonMove = cannonMove;
            shadowCannonRef.transform.localPosition = Vector3.zero;
        }
        shadowCannonRef.GetComponent<ShadowCannonFire>().damageShadowCannon = damage;
    }
}