using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public enum EggType
{
    E1 = 0,
    E2 = 1,
    E3 = 2,
    E4 = 3,
    GoldenEgg = 4,
    IceEgg = 5,
    FireEgg = 6,
}

[Obsolete("Use Gameplay.Health.EggHealth with IDamageable instead. Legacy flow - will be removed.")]
public class EggHealth : MonoBehaviour, IHealthManager, IDamageEffect
{



    public float Health;
    private float maxHealth;
    public EggType eggType;

    [SerializeField] TMP_Text healthText;
    [SerializeField] GameObject EggE1, EggE2, EggE3;
    [SerializeField] GameObject blastparticle;
    [SerializeField] int minEggHealth, maxEggHealth;
    [SerializeField] LayerMask eggMask;
    [SerializeField] GameObject spellFlask;


    //  [SerializeField]LineRenderer lineRenderer;

    bool EggDestroyed;

    Material mat;

    public static event Action<EggType> OnEggDestroyed;


    public bool spikeHit;



    bool HitEffect = false;
    [SerializeField] float hitDuration;

    [Header("PoisonEffect")]
    public float poisonTime;
    bool poisonEffect;

    [Header("ElectricEffect")]
    bool ElectricEffect;
    float ElectricDuration;

    [Header("Fire")]
    bool fireDamage;
    float fireDuration;

    [SerializeField] LayerMask groundMask;
    [SerializeField] float distanceToGround;

    private void Start()
    {

        mat = GetComponent<SpriteRenderer>().material;
    }
    private void OnEnable()
    {
        EggManager.EggsAdd(this.gameObject);
        //GameManager.LevelEnd += DestroyEggs;
        IntialValueSet();
    }

    private void OnDisable()
    {
        //GameManager.LevelEnd -= DestroyEggs;

    }


    public void IntialValueSet()
    {
        EggDestroyed = false;
        Health = UnityEngine.Random.Range(minEggHealth, maxEggHealth);

        maxHealth = Health;
        healthText.text = Health.ToString();
    }

    public void EggDataSet(int Health)
    {
        this.Health = Health;
        maxHealth = Health;
        healthText.text = Health.ToString("0");
    }
    public void TakeDamage(float ApplyDamage)
    {
        if (!EggDestroyed)
        {
            //ScoreManager.scoreUpdateCallBack?.Invoke(Mathf.Min(Health, ApplyDamage)); // Send Score To scoreManager
            Health -= ApplyDamage;


            if (Health <= 0)
            {
                StopAllCoroutines();
                EggDestroyed = true;
                LevelManager.Instance.destroyEggs += 1;

                // Trigger the event for level progress tracking
                OnEggDestroyed?.Invoke(eggType);

                CheckEgg();
                GameObject go = Instantiate(blastparticle, transform.position, Quaternion.identity);
                go.transform.localScale = transform.localScale;
                Destroy(go, 3f);
            }
            else
            {
                StartCoroutine(PlayHitEffect());
                healthText.text = Health < 1 ? "1" : Health.ToString("0");
            }
        }
    }

    IEnumerator PlayHitEffect()
    {
        /*       if (!HitEffect)
               {
                   if (!EggDestroyed)
                   {

                       var id = mat.GetInstanceID();
                       DOTween.Kill(id + "_HitEffect");

                       // Start from 0
                       mat.SetFloat("_HitEffect", 0f);

                       // Animate 0 → 1 → 0
                       DOTween.Sequence()
                           .Append(DOTween.To(() => mat.GetFloat("_HitEffect"),
                                              x => mat.SetFloat("_HitEffect", x),
                                              1f, 0.3f))
                           .Append(DOTween.To(() => mat.GetFloat("_HitEffect"),
                                              x => mat.SetFloat("_HitEffect", x),
                       1f, 0.1f))
                           .SetId(mat.GetInstanceID() + "_HitEffect").OnComplete(() =>
                           {
                               HitEffect = false;
                           }); // So you can kill it safely later
                   }
               }
               yield return null;*/
        if (!HitEffect)
        {
            HitEffect = true;
            float time = 0;
            while (time <= 1)
            {
                mat.SetFloat("_HitEffect", Mathf.Lerp(0, 1, time));
                time += Time.deltaTime * hitDuration;
                yield return null;
            }
            while (time >= 0)
            {
                mat.SetFloat("_HitEffect", Mathf.Lerp(0, 1, time));
                time -= Time.deltaTime * hitDuration;
                yield return null;

            }

            mat.SetFloat("_HitEffect", 0f);
            HitEffect = false;

        }
    }
    IEnumerator ElectricGiveDamage(float applyDamage)
    {
        yield return new WaitForSeconds(1);
        float i = ElectricDuration;
        while (i > 0)
        {
            if (!EggDestroyed)
            {
                TakeDamage(applyDamage);
                yield return new WaitForSeconds(1f);
                i--;
            }
        }
        GetComponent<SpriteRenderer>().material.SetInt("_ElectricShock", 0);
        ElectricEffect = false;
    }

    public void PoisonDamage(float applyDamage, float effectTime)
    {

        poisonTime = effectTime;
        StartCoroutine(PoisonCoroutine(applyDamage));
    }

    IEnumerator PoisonCoroutine(float applyDamage)
    {
        if (!poisonEffect)
        {
            poisonEffect = true;
            while (poisonTime > 0)
            {
                TakeDamage(applyDamage);
                yield return new WaitForSeconds(1f);
                poisonTime -= 1;

            }
            poisonEffect = false;
        }
    }
    public void DestroyEggs()
    {
        GameObject go = Instantiate(blastparticle, transform.position, Quaternion.identity);
        go.transform.localScale = transform.localScale;
        Destroy(go, 3f);
        if (GamePoolManager.EggPool.ContainsKey(eggType))
        {
            GamePoolManager.EggPool[eggType].Enqueue(this.gameObject);
        }
        else
        {
            GamePoolManager.EggPool.Add(eggType, new());
            GamePoolManager.EggPool[eggType].Enqueue(this.gameObject);
        }
        //Debug.Log(GamePoolManager.EggPool[eggType].Count);
        this.gameObject.SetActive(false);
        transform.rotation = Quaternion.Euler(Vector3.zero);
        GetComponent<Rigidbody2D>().linearVelocity = Vector3.zero;
        //Destroy(gameObject);
    }
    public void CheckEgg()
    {
        StopCoroutine(PlayHitEffect());
        GameObject Go;
        if (eggType == EggType.E2)
        {

            Go = CheckEggPool(EggE1, 1);
            OnDestroyEgg(Go, 2f);
            Go = CheckEggPool(EggE1, -1);
            OnDestroyEgg(Go, -2f);

        }
        else if (eggType == EggType.E3)
        {
            if (CannonPower.instance.ThunderSpellFlaskPower && spellFlask != null && !ThunderSpellFlask.instance.alreadyExist)
            {
                //GameObject go = Instantiate(spellFlask, transform.position, Quaternion.identity);
                ThunderSpellFlask.instance.transform.position = transform.position;
                ThunderSpellFlask.instance.gameObject.SetActive(true);

            }
            Go = CheckEggPool(EggE2, 1);
            OnDestroyEgg(Go, 2f);
            Go = CheckEggPool(EggE2, -1);
            OnDestroyEgg(Go, -2f);
        }
        else if (eggType == EggType.E4)
        {
            if (CannonPower.instance.ThunderSpellFlaskPower && spellFlask != null && !ThunderSpellFlask.instance.alreadyExist)
            {
                //GameObject go = Instantiate(spellFlask, transform.position, Quaternion.identity);
                ThunderSpellFlask.instance.transform.position = transform.position;
                ThunderSpellFlask.instance.gameObject.SetActive(true);

            }
            Go = CheckEggPool(EggE3, 1);
            OnDestroyEgg(Go, 2f);
            Go = CheckEggPool(EggE3, -1);
            OnDestroyEgg(Go, -2f);
        }
        EggManager.RemoveEggs(this.gameObject);
        DestroyEggs();
    }


    public GameObject CheckEggPool(GameObject egg, float dirMultiplier)
    {
        var eggtype = egg.GetComponent<EggHealth>().eggType;
        if (GamePoolManager.EggPool.ContainsKey(eggtype) && GamePoolManager.EggPool[eggtype].Count > 0)
        {
            var go = GamePoolManager.EggPool[eggtype].Dequeue();
            go.transform.position = transform.position + Vector3.right * (0.5f * dirMultiplier);
            go.SetActive(true);
            return go;
        }
        else
        {
            var go = Instantiate(egg, transform.position + Vector3.right * (0.5f * dirMultiplier), Quaternion.identity);
            return go;
        }
    }
    public void OnDestroyEgg(GameObject Egg, float speed)
    {

        var sprite = Egg.GetComponent<SpriteRenderer>();
        sprite.material.SetColor("_Color", sprite.color);
        sprite.color = Color.white;
        Egg.GetComponent<Rigidbody2D>().AddForce(Vector3.up * 2f + Vector3.right * speed, ForceMode2D.Impulse);
        int health = Mathf.FloorToInt(maxHealth / 2);
        Egg.GetComponent<EggHealth>().EggDataSet(health);
    }

    public void ElectricDamage(float ApplyDamage, float effectTime)
    {
        if (!ElectricEffect)
        {
            ElectricEffect = true;
            mat.SetInt("_ElectricShock", 1);
            StartCoroutine(ElectricGiveDamage(ApplyDamage));
        }
    }

    public void igniteDamage(float ApplyDamage, float effectTime)
    {
        fireDuration = effectTime;
        if (!fireDamage)
        {

            fireDamage = true;
            StartCoroutine(FireDamage(ApplyDamage));
        }


    }


    private void OnTriggerEnter2D(Collider2D collision)
    {

        /* if (collision.transform.tag == "Fire")
         {
             fireDamage = true;

         }*/
        /*   if (collision.transform.tag == "Player")
           {
               var health = collision.GetComponent<CannonHealth>();
               if (health != null)
               {
                   health.GetDamage(this);
               }
               if (eggType == EggType.IceEgg)
               {
                   var damageEffect = collision.GetComponent<IDamageEffect>();
                   damageEffect.FreezeEffect(3f);
               }
           }*/
        if (collision.transform.tag == "Spike")
        {

            if (!spikeHit)
            {
                Debug.Log("Spike");
                spikeHit = true;
                StartCoroutine(SpikeReset());
                TakeDamage(1f);
            }


        }
    }

    IEnumerator SpikeReset()
    {
        yield return new WaitForSeconds(0.5f);
        spikeHit = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        /*  if (collision.transform.CompareTag("Fire"))
          {
              fireDamage = false;
          }*/

    }

    IEnumerator FireDamage(float damage)
    {
        while (fireDuration > 0)
        {
            TakeDamage(damage);
            yield return new WaitForSeconds(1);
            fireDuration--;
        }

        fireDamage = false;
        yield break;

    }

    public void FreezeEffect(float effectTime)
    {
        StartCoroutine(FreezeRoutine());
    }
    IEnumerator FreezeRoutine()
    {
        var hits = Physics2D.Raycast(transform.position, Vector2.down, distanceToGround, groundMask);
        if (hits)
        {
            transform.DOMoveY(0.5f, 0.1f);
        }
        var rb = this.GetComponent<Rigidbody2D>();
        var velocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        yield return new WaitForSeconds(3f);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = velocity;

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanceToGround);
    }
    /*  [ContextMenu("Play")]
 public void ElectricEffect()
 {
     var nearEggs = Physics2D.CircleCastAll(transform.position,5f,transform.position,5f, eggMask);
     if(nearEggs.Length > 0)
     {

         if (nearEggs[1].transform.tag == "Egg")
         {
             lineRenderer.positionCount = 2;
             lineRenderer.SetPosition(0,transform.position);
             lineRenderer.SetPosition(1, nearEggs[0].transform.position);
         }
     }
 }*/


}
