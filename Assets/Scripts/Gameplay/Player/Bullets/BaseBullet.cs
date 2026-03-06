using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using TMPro;
using UnityEngine;

namespace Gameplay.Player
{
    public abstract class BaseBullet : MonoBehaviour
    {
        [Header("Base Settings")]
        public float bulletSpeed;
        public float damage;
        public float lifetime = 5f;
        
        [Header("VFX & UI")]
        public GameObject damageTextPrefab;
        
        protected Rigidbody2D rb;
        protected float currentLifetime;
        protected bool isDeactivated;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        protected virtual void OnEnable()
        {
            currentLifetime = 0;
            isDeactivated = false;
        }

        protected virtual void Update()
        {
            if (isDeactivated) return;

            HandleMovement();
            
            currentLifetime += Time.deltaTime;
            if (currentLifetime >= lifetime)
            {
                Deactivate();
            }
        }

        protected virtual void HandleMovement()
        {
            transform.position += transform.up * bulletSpeed * Time.deltaTime;
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (isDeactivated) return;

            if (collision.CompareTag("Wall"))
            {
                Deactivate();
                return;
            }

            if (collision.CompareTag("Bird") || collision.CompareTag("Egg"))
            {
                OnHitTarget(collision);
            }
        }

        protected virtual void OnHitTarget(Collider2D collision)
        {
            Vector3 hitPoint = collision.ClosestPoint(transform.position);
            
            // Try IDamageable (New System)
            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage((int)damage, hitPoint);
                
                // Fire Events
                if (collision.CompareTag("Egg"))
                    GameEvents.FireEggHit(damageable, (int)damage, hitPoint);
                else if (collision.CompareTag("Bird"))
                    GameEvents.FireBirdHit(damageable, (int)damage, hitPoint);
                
                ShowDamageText(hitPoint, damage);
                ApplyElementalEffects(collision.gameObject);
            }
            
            HandlePostHit(collision);
        }

        protected virtual void HandlePostHit(Collider2D collision)
        {
            // Default behavior is to deactivate on hit
            Deactivate();
        }

        protected virtual void ApplyElementalEffects(GameObject target)
        {
            // To be implemented by subclasses or specialized components
        }

        protected virtual void ShowDamageText(Vector3 position, float amount)
        {
            GameObject go = null;
            if (GamePoolManager.bulletDamageTextQueue.Count > 0)
            {
                go = GamePoolManager.bulletDamageTextQueue.Dequeue();
                go.transform.position = position;
                go.SetActive(true);
            }
            else if (damageTextPrefab != null)
            {
                go = Instantiate(damageTextPrefab, position, Quaternion.identity);
            }

            if (go != null)
            {
                var tmp = go.GetComponent<TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = $"-{amount:0}";
                    tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1);
                    go.transform.DOMoveY(position.y + 2f, 1f);
                    tmp.DOFade(0, 1).OnComplete(() => {
                        go.SetActive(false);
                        GamePoolManager.bulletDamageTextQueue.Enqueue(go);
                    });
                }
            }
        }

        public virtual void Deactivate()
        {
            if (isDeactivated) return;
            isDeactivated = true;
            
            Destroy(gameObject);

            // If this bullet has a specific type, we can return it to the legacy pool
            // to maintain compatibility with legacy CannonFire if they share the same prefab.
            // But usually, the new system should handle its own pooling.
        }
    }
}
