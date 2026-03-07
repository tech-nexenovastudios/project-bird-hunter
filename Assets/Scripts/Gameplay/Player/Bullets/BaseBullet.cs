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
        [SerializeField] protected float hitImpulseForce    = 8f;   // horizontal nudge strength
        [SerializeField] protected float verticalFreezeTime = 0.15f; // seconds egg floats in place
        [SerializeField] protected bool  applyHitImpulse    = true;

        [Header("VFX & UI")]
        public GameObject damageTextPrefab;

        protected Rigidbody2D rb;
        protected float       currentLifetime;
        protected bool        isDeactivated;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        protected virtual void OnEnable()
        {
            currentLifetime = 0;
            isDeactivated   = false;
        }

        protected virtual void Update()
        {
            if (isDeactivated) return;

            HandleMovement();

            currentLifetime += Time.deltaTime;
            if (currentLifetime >= lifetime)
                Deactivate();
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
                OnHitTarget(collision);
        }

        protected virtual void OnHitTarget(Collider2D collision)
        {
            Vector3 hitPoint = collision.ClosestPoint(transform.position);

            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage((int)damage, hitPoint);

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

        // ─────────────────────────────────────────────
        // Hit Impulse — freezes egg vertically, nudges horizontally
        // ─────────────────────────────────────────────

        protected virtual void ApplyHitImpulse(Collider2D collision)
        {
            if (!applyHitImpulse) return;

            var egg = collision.GetComponent<Egg>();
            if (egg == null) return;

            // Pass bullet travel direction so Egg can compute horizontal nudge
            Vector2 bulletDir = transform.up.normalized;
            egg.ApplyHitFreeze(verticalFreezeTime, hitImpulseForce, bulletDir);
        }

        protected virtual void HandlePostHit(Collider2D collision)
        {
            Deactivate();
        }

        protected virtual void ApplyElementalEffects(GameObject target)
        {
            // Override in subclass for fire, electric, poison, freeze
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
            gameObject.SetActive(false);
        }
    }
}
