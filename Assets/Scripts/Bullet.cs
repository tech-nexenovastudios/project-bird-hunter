using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Gameplay.Interfaces;


public enum BulletType
{
    Normal = 0,
    Fire = 1,
}
public class Bullet : MonoBehaviour
{
    public BulletType type;
    Transform target;
    public bool hit = false;

    [HideInInspector] public bool IgniteEffect;
    [HideInInspector] public float igniteEffectDuration;
    [HideInInspector] public bool electricEffect;
    [HideInInspector] public float electricEffectDuration;
    [HideInInspector] public float bulletSpeed;
    [HideInInspector] public float Damage;
    [HideInInspector] public int bulletBounce;
    public LayerMask eggMask;

    public List<GameObject> alreadyHitObj = new();

    bool findingTarget = false;

    [HideInInspector] public bool chanceOfBulletPierceEnemies;
    [HideInInspector] public float chanceOfBulletPierceEnemiesProbability;

    public List<GameObject> collideObjects = new();

    [SerializeField] GameObject damageText;

    public CannonFire cannonFire;

    bool alreadyTrigger;

    //Checking for the bulllet stage that bullet is active or not
    bool bulletCheck = true;


    private void OnEnable()
    {
        bulletCheck = true;
    }
    private void Start()
    {

    }
    private void Update()
    {
        if (!hit)
        {
            transform.position += transform.up * bulletSpeed * Time.deltaTime;
        }
        else
        {
            if (target != null)
            {
                transform.position = Vector2.MoveTowards(transform.position, target.position, Time.deltaTime * 20);
                if (transform.position == target.position)
                {
                    this.GetComponent<Collider>().enabled = true;
                }
                Vector2 dir = transform.position - target.position;
                float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                //transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0, 0, rot+90),1000*Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, 0f, rot + 90f);
            }
            // transform.LookAt(target.position);
            else
            {
                // Debug.Log(target);
                BounceCheck();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Check If bullet collide with wall
        if (collision.transform.CompareTag(TagManager.WallTag))
        {
            DestroyBullet();
        }

        //If bullet is already destroyed then just return the function
        if (!bulletCheck) return;

        //1: Checking if bullet collide with bird or egg
        //2: if collide then store the ref of game object inside the bullet so bullet can give damage only single object at a time
        //3: then we use one more list to store already hit object so if bullet bounce then not hit same object 
        if (collision.transform.CompareTag(TagManager.BirdTag) || collision.transform.CompareTag(TagManager.EggTag))
        {
            collideObjects.Add(collision.gameObject);
            if (collideObjects.Count > 0)
            {
                if (!alreadyHitObj.Contains(collideObjects[0]))
                {
                    alreadyHitObj.Add(collision.gameObject);
                    Vector3 hitPoint = collision.ClosestPoint(transform.position);
                    var damageable = collideObjects[0].GetComponent<IDamageable>();
                    var legacyHealth = collideObjects[0].GetComponent<IHealthManager>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage((int)Damage, hitPoint);
                        if (GamePoolManager.bulletDamageTextQueue.Count <= 0)
                        {

                            GameObject go = Instantiate(damageText, transform.position, Quaternion.identity);
                            go.GetComponent<TextMeshPro>().text = "-" + Damage.ToString("0");
                            go.transform.DOMoveY(transform.position.y + 2f, 1f);
                            go.GetComponent<TextMeshPro>().DOFade(0, 1);
                            cannonFire.DestroyText(go);
                        }
                        else
                        {
                            GameObject go = GamePoolManager.bulletDamageTextQueue.Dequeue();
                            go.transform.position = transform.position;
                            go.SetActive(true);
                            go.GetComponent<TextMeshPro>().text = "-" + Damage.ToString("0");
                            go.transform.DOMoveY(transform.position.y + 2f, 1f);
                            go.GetComponent<TextMeshPro>().DOFade(0, 1);
                            cannonFire.DestroyText(go);
                        }

                        IDamageEffect damageEffect = collideObjects[0].GetComponent<IDamageEffect>();
                        if (damageEffect != null)
                        {
                            if (IgniteEffect)
                            {
                                damageEffect.igniteDamage(Damage, igniteEffectDuration);
                            }
                            if (electricEffect)
                            {
                                damageEffect.ElectricDamage(Damage, electricEffectDuration);
                            }
                        }

                        if (chanceOfBulletPierceEnemies && Random.Range(0, 100) < chanceOfBulletPierceEnemiesProbability)
                        {
                            damageable.TakeDamage(int.MaxValue, hitPoint);
                            DestroyBullet();
                        }
                        else
                        {
                            BounceCheck();
                        }
                    }
                    else if (legacyHealth != null)
                    {
                        legacyHealth.TakeDamage(Damage);
                        if (GamePoolManager.bulletDamageTextQueue != null && cannonFire != null && damageText != null)
                        {
                            if (GamePoolManager.bulletDamageTextQueue.Count <= 0)
                            {
                                GameObject go = Instantiate(damageText, transform.position, Quaternion.identity);
                                go.GetComponent<TextMeshPro>().text = "-" + Damage.ToString("0");
                                go.transform.DOMoveY(transform.position.y + 2f, 1f);
                                go.GetComponent<TextMeshPro>().DOFade(0, 1);
                                cannonFire.DestroyText(go);
                            }
                            else
                            {
                                GameObject go = GamePoolManager.bulletDamageTextQueue.Dequeue();
                                go.transform.position = transform.position;
                                go.SetActive(true);
                                go.GetComponent<TextMeshPro>().text = "-" + Damage.ToString("0");
                                go.transform.DOMoveY(transform.position.y + 2f, 1f);
                                go.GetComponent<TextMeshPro>().DOFade(0, 1);
                                cannonFire.DestroyText(go);
                            }
                        }
                        var damageEffect = collideObjects[0].GetComponent<IDamageEffect>();
                        if (damageEffect != null)
                        {
                            if (IgniteEffect) damageEffect.igniteDamage(Damage, igniteEffectDuration);
                            if (electricEffect) damageEffect.ElectricDamage(Damage, electricEffectDuration);
                        }
                        if (chanceOfBulletPierceEnemies && Random.Range(0, 100) < chanceOfBulletPierceEnemiesProbability)
                        {
                            legacyHealth.TakeDamage(float.MaxValue);
                            DestroyBullet();
                        }
                        else
                        {
                            BounceCheck();
                        }
                    }
                }
            }
        }
    }
    /*    private void OnTriggerEnter2D(Collider2D collision)
        {

            if (collision.transform.CompareTag("Bird") || collision.transform.CompareTag("Egg"))
            {

                if (!collideObjects.Contains(collision.gameObject))
                {
                    collideObjects.Add(collision.gameObject);
                }


                ContactPoint2D[] contacts = new ContactPoint2D[1];
                *//*       this.GetComponent<Collider2D>().enabled = false;
                       Debug.Log(collideObjects.Count);*//*
                if (collideObjects[0] != null)
                {

                    if (collideObjects.Count > 0)
                    {

                        IHealthManager Health = collideObjects[0].GetComponent<IHealthManager>();
                        if (Health != null)
                        {
                            Health.TakeDamage(Damage);
                            Debug.Log("Damage");
                            if (CannonFire.bulletDamageTextQueue.Count <= 0)
                            {

                                GameObject go = Instantiate(damageText, transform.position, Quaternion.identity);
                                go.GetComponent<TextMeshPro>().text = "-" + Damage.ToString("0");
                                go.transform.DOMoveY(transform.position.y + 2f, 1f);
                                go.GetComponent<TextMeshPro>().DOFade(0, 1);
                                cannonFire.DestroyText(go);
                            }
                            else
                            {
                                GameObject go = CannonFire.bulletDamageTextQueue.Dequeue();
                                go.transform.position = transform.position;
                                go.SetActive(true);
                                go.GetComponent<TextMeshPro>().text = "-" + Damage.ToString("0");
                                go.transform.DOMoveY(transform.position.y + 2f, 1f);
                                go.GetComponent<TextMeshPro>().DOFade(0, 1);
                                cannonFire.DestroyText(go);
                            }
                        }
                    }


                    if (alreadyHitObj.Contains(collideObjects[0]))
                    {
                        alreadyHitObj.Add(collideObjects[0]);
                    }

                    IDamageEffect damageEffect = collideObjects[0].GetComponent<IDamageEffect>();
                    if (damageEffect != null)
                    {
                        if (IgniteEffect)
                        {
                            damageEffect.igniteDamage(Damage, igniteEffectDuration);
                        }
                        if (electricEffect)
                        {
                            damageEffect.ElectricDamage(Damage, electricEffectDuration);
                        }
                    }

                    if (chanceOfBulletPierceEnemies && Random.Range(0, 100) < chanceOfBulletPierceEnemiesProbability)
                    {
                        IHealthManager Health = collideObjects[0].GetComponent<IHealthManager>();
                        if (Health != null)
                        {
                            Health.TakeDamage(float.MaxValue);
                            DestroyBullet();
                        }
                    }

                    else
                    {

                        BounceCheck();
                    }

                }



            }
            if (collision.transform.CompareTag("Egg"))
            {
                var Egg = collision.GetComponent<EggHealth>();
                if (Egg != null)
                {
                    // Egg.ElectricEffect();
                }


            }
            if (collision.transform.CompareTag("Wall"))
            {
                DestroyBullet();
            }

        }
    */

    public void BounceCheck()
    {

        // collideObjects.Clear();
        if (!findingTarget)
        {
            findingTarget = true;
            target = null;
            Damage /= 2;
            if (bulletBounce > 0)
            {
                collideObjects.Clear();
                bulletBounce--;
                var nearTargets = Physics2D.OverlapCircleAll(transform.position, 5f, eggMask);

                //Debug.Log("Nearby targets: " + nearTargets.Length);

                if (nearTargets.Length > 0)
                {
                    foreach (var ntarget in nearTargets)
                    {
                        if (!alreadyHitObj.Contains(ntarget.gameObject))
                        {
                            if (target == null)
                            {
                                target = ntarget.transform;
                                break;
                            }
                            if (ntarget.gameObject != target.gameObject)
                            {

                                // Debug.Log(ntarget.gameObject == target.transform);
                                target = ntarget.transform;  // ✅ Correct assignment
                                                             //Debug.Log("New target: " + target.name);
                                break;
                            }


                        }


                    }
                    hit = true;

                }
                else
                {
                    DestroyBullet();
                }
            }
            else
            {
                DestroyBullet();
            }
            if (target == null)
            {
                DestroyBullet();
            }
            findingTarget = false;



        }

    }



    public void DestroyBullet()
    {
        if (GamePoolManager.bulletQueue.ContainsKey(type))
        {
            if (!GamePoolManager.bulletQueue[type].Contains(this.gameObject))
            {
                bulletCheck = false;
                this.gameObject.SetActive(false);
                findingTarget = false;
                alreadyHitObj.Clear();
                collideObjects.Clear();
                hit = false;
                GamePoolManager.bulletQueue[type].Enqueue(this.gameObject);
                transform.localScale = Vector3.one;
            }

        }
        else
        {
            GamePoolManager.bulletQueue[type] = new Queue<GameObject>();
            GamePoolManager.bulletQueue[type].Enqueue(this.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
