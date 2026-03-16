using System;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using TMPro;
using UnityEngine;

namespace Gameplay.Player
{
    public interface IBullet
    {
        void Initialize(BulletConfig config, Vector2 direction, Action<IBullet> releaseAction);
        void Deactivate();
        GameObject gameObject { get; }
    }

    public interface IBulletBehaviour<TBullet> where TBullet : BaseBullet<TBullet>
    {
        void OnSpawn(TBullet bullet);
        void Tick(TBullet bullet, float dt);
        void OnHit(TBullet bullet, Collider2D collider);
        void OnDespawn(TBullet bullet);
    }

    public abstract class BaseBullet<TBullet> : MonoBehaviour, IBullet
        where TBullet : BaseBullet<TBullet>
    {
        [Header("VFX & UI")]
        public GameObject damageTextPrefab;

        // Runtime stats — written once per spawn by Initialize
        public float currentDamage  { get; private set; }
        public float currentSpeed   { get; private set; }
        public float currentSize    { get; private set; }
        public int   pierceRemaining { get; protected set; }

        protected Rigidbody2D rb;
        protected Vector2     direction;
        protected BulletConfig config;

        private IBulletBehaviour<TBullet> _behaviour;
        private Action<IBullet>           _releaseToPool;
        private float                     _lifetime;
        private float                     _elapsed;
        private bool                      _active;

        // ── Lifecycle ────────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            rb         = GetComponent<Rigidbody2D>();
            _behaviour = CreateBehaviour();
        }

        protected abstract IBulletBehaviour<TBullet> CreateBehaviour();

        // ── IBullet ──────────────────────────────────────────────────────────
        public void Initialize(BulletConfig cfg, Vector2 dir, Action<IBullet> releaseAction)
        {
            config         = cfg;
            direction      = dir.normalized;
            _releaseToPool = releaseAction;

            // Stats
            currentDamage    = cfg.baseDamage;
            currentSpeed     = cfg.baseSpeed;
            currentSize      = cfg.baseSize;
            pierceRemaining  = cfg.pierceCount;
            _lifetime        = cfg.lifetime;
            _elapsed         = 0f;
            _active          = true;

            // Physics
            transform.localScale = Vector3.one * currentSize;
            if (rb != null)
            {
                rb.mass            = cfg.mass;
                rb.angularVelocity = 0f;
                rb.linearVelocity  = direction * currentSpeed;
            }

            // Behaviour notified LAST — all data is ready
            _behaviour?.OnSpawn((TBullet)this);
        }

        public virtual void Deactivate()
        {
            if (!_active) return;
            _active = false;

            _behaviour?.OnDespawn((TBullet)this);

            if (rb != null)
            {
                rb.linearVelocity  = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (_releaseToPool != null)
                _releaseToPool.Invoke(this);
            else
                gameObject.SetActive(false);
        }

        // ── Unity Update ─────────────────────────────────────────────────────
        protected virtual void Update()
        {
            if (!_active) return;
            _elapsed += Time.deltaTime;
            _behaviour?.Tick((TBullet)this, Time.deltaTime);
            if (_elapsed >= _lifetime) Deactivate();
        }

        // ── Hit detection ────────────────────────────────────────────────────
        protected virtual void OnTriggerEnter2D(Collider2D col)
        {
            if (!_active) return;
            if (col.CompareTag("Wall"))
            {
                Deactivate(); 
                return;
            }
            if (col.CompareTag("Bird") || col.CompareTag("Egg"))
                _behaviour?.OnHit((TBullet)this, col);
        }

        protected virtual void OnCollisionEnter2D(Collision2D col)
        {
            if (!_active) return;
            if (col.gameObject.CompareTag("Ground")) Deactivate();

            if (col.gameObject.CompareTag("Egg") || col.gameObject.CompareTag("Bird"))
            {
                _behaviour?.OnHit((TBullet)this, col.collider);
            }
        }

        // ── Damage helpers ───────────────────────────────────────────────────
        public virtual void ApplyDamage(Collider2D col)
        {
            Vector3 hitPoint = col.ClosestPoint(transform.position);
            if (!col.TryGetComponent<IDamageable>(out var dmg)) return;

            dmg.TakeDamage((int)currentDamage, hitPoint);

            if (col.CompareTag("Egg"))  GameEvents.FireEggHit(dmg,  (int)currentDamage, hitPoint);
            else if (col.CompareTag("Bird")) GameEvents.FireBirdHit(dmg, (int)currentDamage, hitPoint);

            ShowDamageText(hitPoint, currentDamage);
            ApplyElementalEffects(col.gameObject);
        }

        protected virtual void ApplyElementalEffects(GameObject target) { }

        public void SetVelocity(Vector2 dir)
        {
            if (rb != null) rb.linearVelocity = dir.normalized * currentSpeed;
        }

        // ── Damage text ──────────────────────────────────────────────────────
        private void ShowDamageText(Vector3 position, float amount)
        {
            GameObject go = GamePoolManager.bulletDamageTextQueue.Count > 0
                ? GamePoolManager.bulletDamageTextQueue.Dequeue()
                : damageTextPrefab != null ? Instantiate(damageTextPrefab) : null;

            if (go == null) return;
            go.transform.position = position;
            go.SetActive(true);

            var tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) return;

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
