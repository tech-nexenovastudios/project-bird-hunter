using System;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using TMPro;
using UnityEngine;

namespace Gameplay.Player
{
    public interface IInitializable
    {
        void Initialize(BulletConfig config, Vector2 direction, Action<IBullet> releaseAction = null);
    }
    public interface IBulletBehaviour<TBullet>
    {
        void OnSpawn(TBullet bullet);
        void Tick(TBullet bullet, float dt);
        void OnHit(TBullet bullet, Collider2D collider);
        void OnDespawn(TBullet bullet);
    }

    public interface IBullet
    {
        void Deactivate();
        GameObject gameObject { get; }
        void SetReleaseAction(Action<IBullet> releaseBulletToPool);
    }

    public abstract class BaseBullet<TBullet, TBehaviour> : MonoBehaviour, IBullet, IInitializable
        where TBullet    : BaseBullet<TBullet, TBehaviour>
        where TBehaviour : IBulletBehaviour<TBullet>, new()
    {
        [Header("Runtime Stats")]
        public float currentDamage;
        public float currentSpeed;
        public float currentSize;
        public int pierceRemaining;

        [Header("VFX & UI")]
        public GameObject damageTextPrefab;  // ← MOVED HERE

        protected Rigidbody2D rb;
        protected TBehaviour behaviour;
        protected BulletConfig config;
        protected Vector2 direction;
        protected float lifetime;
        protected float currentLifetime;
        protected bool isDeactivated;
        protected Action<IBullet> releaseToPool;
        protected Vector2 startPosition;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            behaviour = new TBehaviour();
        }

        public virtual void Initialize(
            BulletConfig bulletConfig,
            Vector2 shootDirection,
            Action<IBullet> releaseAction = null)
        {
            config = bulletConfig;
            direction = shootDirection.normalized;
            releaseToPool = releaseAction;
            ApplyConfig();
            ResetState();
            behaviour.OnSpawn((TBullet)this);
        }

        protected virtual void ApplyConfig()
        {
            if (config == null) return;
            currentDamage = config.baseDamage;
            currentSpeed = config.baseSpeed;
            currentSize = config.baseSize;
            pierceRemaining = config.pierceCount;
            lifetime = config.lifetime;
            transform.localScale = Vector3.one * currentSize;
            if (rb != null) rb.mass = config.mass;
        }

        protected virtual void ResetState()
        {
            isDeactivated = false;
            currentLifetime = 0f;
            startPosition = transform.position;
            if (rb != null)
            {
                rb.angularVelocity = 0f;
                rb.linearVelocity = direction * currentSpeed;
            }
        }

        protected virtual void Update()
        {
            if (isDeactivated) return;
            float dt = Time.deltaTime;
            currentLifetime += dt;
            behaviour.Tick((TBullet)this, dt);
            if (currentLifetime >= lifetime) Deactivate();
        }

        // ─────────────────────────────────────────
        // Tag-based hit detection (ALL bullets)
        // ─────────────────────────────────────────
        protected virtual void OnTriggerEnter2D(Collider2D collider)
        {
            if (isDeactivated) return;

            if (collider.CompareTag("Wall"))
            {
                Deactivate();
                return;
            }

            if (collider.CompareTag("Bird") || collider.CompareTag("Egg"))
            {
                behaviour.OnHit((TBullet)this, collider);
            }
        }

        // ─────────────────────────────────────────
        // Core hit handler (ALL bullets use this)
        // ─────────────────────────────────────────
        public virtual void ApplyDamage(Collider2D collider)
        {
            Vector3 hitPoint = collider.ClosestPoint(transform.position);

            if (!collider.TryGetComponent<IDamageable>(out var damageable)) return;

            damageable.TakeDamage((int)currentDamage, hitPoint);

            // Fire events
            if (collider.CompareTag("Egg"))
                GameEvents.FireEggHit(damageable, (int)currentDamage, hitPoint);
            else if (collider.CompareTag("Bird"))
                GameEvents.FireBirdHit(damageable, (int)currentDamage, hitPoint);

            // ← ALL bullets get this automatically
            ShowDamageText(hitPoint, currentDamage);
            ApplyElementalEffects(collider.gameObject);
        }

        // ─────────────────────────────────────────
        // Damage text (ALL bullets inherit this)
        // ─────────────────────────────────────────
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

            if (go == null) return;

            var tmp = go.GetComponent<TextMeshPro>();
            if (tmp == null) return;

            tmp.text = $"-{amount:0}";
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
            go.transform.DOMoveY(position.y + 2f, 1f);
            tmp.DOFade(0f, 1f).OnComplete(() =>
            {
                go.SetActive(false);
                GamePoolManager.bulletDamageTextQueue.Enqueue(go);
            });
        }

        // ─────────────────────────────────────────
        // Elemental hook (ALL bullets, override in subclass)
        // ─────────────────────────────────────────
        protected virtual void ApplyElementalEffects(GameObject target) { }

        public virtual void SetVelocity(Vector2 dir)
        {
            if (rb == null) return;
            rb.linearVelocity = dir.normalized * currentSpeed;
        }

        public virtual void StopMovement()
        {
            if (rb == null) return;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        public virtual void Deactivate()
        {
            if (isDeactivated) return;
            isDeactivated = true;
            behaviour.OnDespawn((TBullet)this);
            StopMovement();
            if (releaseToPool != null) { releaseToPool.Invoke(this); return; }
            gameObject.SetActive(false);
        }

        public void SetReleaseAction(Action<IBullet> releaseBulletToPool)
            => releaseToPool = releaseBulletToPool;

        protected virtual void OnDisable() => StopMovement();

        public Vector2 GetDirection() => direction;
        public Vector2 GetVelocity() => rb != null ? rb.linearVelocity : Vector2.zero;
        public Vector2 GetTravelVector() => (Vector2)transform.position - startPosition;
    }
}
