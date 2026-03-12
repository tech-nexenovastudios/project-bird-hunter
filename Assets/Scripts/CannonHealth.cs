using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Obsolete]
public class CannonHealthOld : MonoBehaviour, IDamageable, IDamageEffect
{
    public float maxHealth;
    public float currentHealth;
    private bool _invincible;
    public List<SpriteRenderer> materials;

    public static Action<float> increaseHealthCallBack;
    public static Action fullHealCallBack;

    [SerializeField] private Image healthBarSlider;
    [SerializeField] private TextMeshProUGUI healthText; // New TextMeshPro text

    public CannonStats cannonStats;

    public Material healthMaterial;

    [Header("Health Bar Sprites")]
    [SerializeField] private Sprite greenBar;  // 76�100%
    [SerializeField] private Sprite yellowBar; // 51�75%
    [SerializeField] private Sprite orangeBar; // 26�50%
    [SerializeField] private Sprite redBar;    // 0�25%
    public bool invincible
    {
        get
        {
            return _invincible;
        }
        set
        {
            _invincible = value;
            InvincibleEffect();

        }
    }
    public float maxInvincibleCount;
    public float currentInvincibleCount;
    public float invincibleDuration;
    public bool destroyDamageObj;
    public bool damageObject;
    public float damageValueWhenHit;
    public static Action cannonHitCallBack;
    private static readonly int Value = Shader.PropertyToID("_Value");


    public bool fullHealBeforeBoss;
    public float healBeforeBossBattleValue;

    public bool revivePlayer;
    public float revivePlayerHealth;


    private void Awake()
    {
        if (cannonStats != null)
            maxHealth = cannonStats.currentMaxHealth;
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        increaseHealthCallBack += IncreaseHealth;
        fullHealCallBack += FullHeal;
    }
    private void OnDisable()
    {
        increaseHealthCallBack -= IncreaseHealth;
        fullHealCallBack -= FullHeal;
    }

    private void Start()
    {
        healthMaterial = new Material(healthBarSlider.material);
        healthBarSlider.material = healthMaterial;
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void SetHealthUI(Image healthBar, TextMeshProUGUI healthText)
    {
        healthBarSlider =  healthBar;
        this.healthText = healthText;
    }
    
    //Call This funcion everytime level change
    public void resetInvincibleCount()
    {
        currentInvincibleCount = maxInvincibleCount;
        InvincibleEffect();
    }
    public void FullHeal()
    {
        if (fullHealBeforeBoss)
        {
            currentHealth = Mathf.Clamp((maxHealth * healBeforeBossBattleValue) / 100, 0, maxHealth);
            UpdateUI();
        }
    }

    public void IncreaseHealth(float health)
    {
        currentHealth = Mathf.Clamp(currentHealth + ((maxHealth / 25) * 100), 0, maxHealth);
        UpdateUI();
    }
    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateUI() ;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (revivePlayer)
        {
            currentHealth = Mathf.Clamp((maxHealth * revivePlayerHealth) / 100,0,maxHealth);
            UpdateUI();
            revivePlayer = false;
        }
        else
        {
            //  Debug.Log("Die");
        }

    }



    public void InvincibleEffect()
    {

        if(materials == null) Debug.Log("Materials is null");
        else
        {
            foreach (var mat in materials)
            {
                if (invincible || currentInvincibleCount > 0)
                {
                    mat.sharedMaterial.SetFloat("_Opacity", 0.7f);
                }
                else if (!invincible && currentInvincibleCount <= 0)
                {
                    mat.sharedMaterial.SetFloat("_Opacity", 1f);
                }

            }
        }

        StartCoroutine(InvincibleAgain());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("Egg"))
        {
            if (!invincible && currentInvincibleCount <= 0)
            {
                GameEvents.FireEggHit(collision.GetComponent<IDamageable>(), (int)cannonStats._baseBulletDamage);

            }
            else
            {
                if (currentInvincibleCount > 0)
                {
                    currentInvincibleCount--;
                }
                else
                {
                    InvincibleEffect();
                }
            }

            if (destroyDamageObj)
            {
                GameEvents.FireEggDestroyed(collision.GetComponent<IDamageable>(), 0, collision.transform.position);

            }

            if (damageObject)
            {
                foreach (var eggs in EggManager.eggsList)
                {
                    GameEvents.FireEggHit(collision.GetComponent<IDamageable>(), (int)(eggs.GetComponent<EggHealth>().Health * damageValueWhenHit) / 100);
                    //eggs.GetComponent<IDamageable>().TakeDamage((int)(eggs.GetComponent<EggHealth>().Health * damageValueWhenHit) / 100, transform.position);
                }
            }
            cannonHitCallBack?.Invoke();

        }
    }

    public void GetDamage(EggHealth health)
    {
        if (!invincible && currentInvincibleCount <= 0)
        {
            TakeDamage((int)health.Health,  health.transform.position);
        }
        else
        {
            if (currentInvincibleCount > 0)
            {
                currentInvincibleCount--;
            }
            else
            {
                InvincibleEffect();
            }
        }

        if (destroyDamageObj)
        {
            health.DestroyEggs();

        }

        if (damageObject)
        {
            foreach (var eggs in EggManager.eggsList)
            {
                eggs.GetComponent<IHealthManager>().TakeDamage((eggs.GetComponent<EggHealth>().Health * damageValueWhenHit) / 100);
            }
        }
        cannonHitCallBack?.Invoke();


    }
    
    IEnumerator InvincibleAgain()
    {
        yield return new WaitForSeconds(invincibleDuration);
        invincible = false;
        yield return new WaitForSeconds(20);
        invincible = true;
    }

    private void UpdateUI()
    {
        healthBarSlider.fillAmount = currentHealth / maxHealth;
        healthBarSlider.DOFillAmount((currentHealth / maxHealth), 1f).SetSpeedBased();
        healthMaterial.SetFloat(Value, (currentHealth / maxHealth));
        healthText.text = ((currentHealth / maxHealth) * 100).ToString(CultureInfo.InvariantCulture);
        float healthPercent = (currentHealth / maxHealth) * 100;

        // Pick sprite + text color
        if (healthPercent > 75f) // 76�100%
        {
            healthBarSlider.sprite = greenBar;
            healthText.color = new Color32(0x68, 0xF5, 0x00, 0xFF);
        }
        else if (healthPercent > 50f) // 51�75%
        {
            healthBarSlider.sprite = yellowBar;
            healthText.color = new Color32(0xE1, 0xF5, 0x00, 0xFF);
        }
        else if (healthPercent > 25f) // 26�50%
        {
            healthBarSlider.sprite = orangeBar;
            healthText.color = new Color32(0xF5, 0x4F, 0x00, 0xFF);
        }
        else if (healthPercent > 10f) // 11�25%
        {
            healthBarSlider.sprite = redBar;
            healthText.color = new Color32(0xF5, 0x1F, 0x00, 0xFF);
        }
        else // 0�10%
        {
            healthBarSlider.sprite = redBar;
            healthText.color = new Color32(0xBF, 0x08, 0x00, 0xFF);
        }
    }


    public void ElectricDamage(float applyDamage, float effectTime)
    {
        throw new System.NotImplementedException();
    }

    public void igniteDamage(float applyDamage, float effectTime)
    {
        StartCoroutine(FireDamageCoroutine(applyDamage,effectTime));
        Debug.Log("Ignite");
    }

    public void PoisonDamage(float applyDamage, float effectTime)
    {
        throw new System.NotImplementedException();
    }

    public void FreezeEffect(float effectTime)
    {
        this.GetComponent<CannonMove>().FreezeEffect(effectTime);
    }

    IEnumerator FireDamageCoroutine(float applyDamage,float effectTime)
    {
        float damage = 0;
        yield return null;
        float time = effectTime;
        while(time > 0)
        {
            time -= 1;
            TakeDamage((int)(applyDamage/effectTime), transform.position);
            damage += applyDamage / effectTime;
            yield return new WaitForSeconds(1);
        }

        Debug.Log(damage);
    }

    public int CurrentHp { get; }
    public int MaxHp { get; }
    public bool IsAlive { get; }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateUI();
    }
}
