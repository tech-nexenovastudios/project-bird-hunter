using System;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.Eggs;
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

        [Header("Hit Impulse")]
        [SerializeField] protected float maxHitImpulseForce = 8f;
        [SerializeField] protected float minHitImpulseForce = 2f;
        [SerializeField] protected float maxForceDistance   = 10f;
        [SerializeField] protected bool  applyHitImpulse    = true;

        [Header("VFX & UI")]
        public GameObject damageTextPrefab;

        protected Rigidbody2D rb;
        protected float       currentLifetime;
        protected bool isDeactivated;
        protected Vector2 startPosition;

        private Action<BaseBullet> releaseToPool;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void SetReleaseAction(Action<BaseBullet> releaseAction)
        {
            releaseToPool = releaseAction;
        }

        public void Init(CannonStats stats)
        {
            bulletSpeed = stats.currentBulletSpeed;
            damage = stats.currentBulletDamage;

            if (rb != null)
            {
                rb.mass = stats.baseBulletMass;
            }

            startPosition = transform.position;

            if (rb != null)
            {
                rb.linearVelocity = (Vector2)transform.up * bulletSpeed;
                rb.angularVelocity = 0f;
            }
        }

        protected virtual void OnEnable()
        {
            currentLifetime = 0f;
            isDeactivated = false;
            startPosition = transform.position;

            if (rb != null)
            {
                rb.angularVelocity = 0f;
            }
        }

        protected virtual void Update()
        {
            if (isDeactivated) return;

            currentLifetime += Time.deltaTime;
            if (currentLifetime >= lifetime)
                Deactivate();
            else
                HandleMovement();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDeactivated) return;

            if (other.gameObject.CompareTag("Bird"))
            {
                OnHitTarget(other);
            }
        }

        protected void OnCollisionEnter2D(Collision2D other)
        {
            if (isDeactivated) return;

            if (other.gameObject.CompareTag("Wall"))
            {
                Deactivate();
                return;
            }
            
            if (other.gameObject.CompareTag("Egg"))
            {
                OnHitTarget(other.collider);
            }
        }

        protected virtual void OnHitTarget(Collider2D collision)
        {
            Vector3 hitPoint = collision.ClosestPoint(transform.position);

            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage((int)damage);

                if (collision.CompareTag("Egg"))
                    GameEvents.FireEggHit(damageable, (int)damage, hitPoint);
                else if (collision.CompareTag("Bird"))
                    GameEvents.FireBirdHit(damageable, (int)damage, hitPoint);

                ShowDamageText(hitPoint, damage);
                ApplyElementalEffects(collision.gameObject);

                // Only apply hit impulse to eggs — birds handle their own knockback
                if (collision.CompareTag("Egg"))
                    ApplyHitImpulse(collision);
            }

            HandlePostHit(collision);
        }

        protected virtual void ApplyHitImpulse(Collider2D collision)
        {
            if (!applyHitImpulse) return;

            Egg egg = collision.GetComponent<Egg>();
            if (egg == null) return;

            Vector2 bulletDir = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : (Vector2)transform.up.normalized;
            
            float distance = Vector2.Distance(startPosition, transform.position);
            float t = maxForceDistance > 0f ? Mathf.Clamp01(distance / maxForceDistance) : 1f;
            float impulseForce = Mathf.Lerp(maxHitImpulseForce, minHitImpulseForce, t);

            egg.ApplyBulletHitForce(bulletDir, impulseForce);
        }

        protected virtual void HandlePostHit(Collider2D collision)
        {
            Deactivate();
        }

        protected abstract void HandleMovement();

        protected virtual void ApplyElementalEffects(GameObject target)
        {
            // Override in subclass for fire, electric, poison, freeze
        }

        private void ShowDamageText(Vector3 position, float amount)
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
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text  = $"-{amount:0}";
                    tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
                    go.transform.DOMoveY(position.y + 2f, 1f);
                    tmp.DOFade(0f, 1f).OnComplete(() =>
                    {
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

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (releaseToPool != null)
            {
                releaseToPool.Invoke(this);
                return;
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}