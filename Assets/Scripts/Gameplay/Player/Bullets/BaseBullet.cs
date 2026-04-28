using System;
using System.Collections.Generic;
using BirdHunter.Inventory.Stats;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.Eggs;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;

namespace Gameplay.Player
{
    public abstract class BaseBullet : MonoBehaviour 
    {
        [HideInInspector] public float instantKillChance;
        [Header("Base Settings")]
        public float bulletSpeed;
        public float damage;
        public float lifetime = 5f;

        [Header("Hit Impulse")]
        [SerializeField] protected float maxHitImpulseForce = 8f;
        [SerializeField] protected float minHitImpulseForce = 2f;
        [SerializeField] protected float maxForceDistance = 10f;
        [SerializeField] protected bool applyHitImpulse = true;

        [Header("VFX & UI")]
        public GameObject damageTextPrefab;

        protected Rigidbody2D rb;
        protected float currentLifetime;
        protected bool isDeactivated;
        protected Vector2 startPosition;

        private Action<BaseBullet> releaseToPool;

        // ── On-hit effects ───────────────────────────────────
        // Populated per-bullet by BulletElementModifier.
        // Each bullet gets its own effect instances (cloned from
        // the modifier's template) so timers don't conflict.
        private readonly List<IEffect<IEntity>> onHitEffects = new();

        // ── Attached element VFX ─────────────────────────────
        // Visual prefabs parented to the bullet (lightning sparks,
        // fire trail, etc). Destroyed when bullet deactivates.
        private readonly List<GameObject> attachedVfx = new();

        /// <summary>
        /// Called by projectile modifiers to attach an effect
        /// that will fire when this bullet hits a target.
        /// </summary>
        public void AddOnHitEffect(IEffect<IEntity> effect)
        {
            if (effect != null)
                onHitEffects.Add(effect);
        }

        /// <summary>
        /// Called by BulletElementModifier to attach a VFX prefab
        /// instance to the bullet. Gets destroyed on deactivate.
        /// </summary>
        public void AttachElementVfx(GameObject vfx)
        {
            if (vfx != null)
                attachedVfx.Add(vfx);
        }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void SetReleaseAction(Action<BaseBullet> releaseAction)
        {
            releaseToPool = releaseAction;
        }

        protected virtual void OnInitComplete() { }

        public void Init(StatSheet stats, Vector2 fireDirection)
        {
            bulletSpeed = stats.Get(StatType.BulletSpeed);
            damage = stats.Get(StatType.Damage);

            if (rb != null) rb.mass = 0.25f;

            startPosition = transform.position;

            if (rb != null)
            {
                rb.linearVelocity = fireDirection.normalized * bulletSpeed;
                rb.angularVelocity = 0f;
            }

            OnInitComplete();
        }

        protected virtual void OnEnable()
        {
            currentLifetime = 0f;
            isDeactivated = false;
            instantKillChance = 0f;
           

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            ClearOldVfx();
            onHitEffects.Clear();
        }

        private void ClearOldVfx()
        {
            for (int i = attachedVfx.Count - 1; i >= 0; i--)
            {
                if (attachedVfx[i] != null)
                    Destroy(attachedVfx[i]);
            }
            attachedVfx.Clear();
        }

        public virtual void Deactivate()
        {
            if (isDeactivated) return;
            isDeactivated = true;

            for (int i = onHitEffects.Count - 1; i >= 0; i--)
                onHitEffects[i]?.Cancel();
            onHitEffects.Clear();

            ClearOldVfx();

            if (releaseToPool != null)
            {
                releaseToPool.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
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
                OnHitTarget(other);
            else if (other.gameObject.CompareTag("BossWeapons"))
                OnHitBossWeapon(other);
            else if (other.gameObject.CompareTag("BossBird"))   // ← ADD THIS
                OnHitBossBird(other);
        }
        private void OnHitBossBird(Collider2D collision)
        {
            if (isDeactivated) return;

            if (collision.TryGetComponent<IDamageablee>(out var bossHealth))
            {
                bool instantKilled = false;
                float dealDamage = damage;

                if (instantKillChance > 0f && UnityEngine.Random.value <= instantKillChance)
                {
                    // IDamageablee doesn't expose CurrentHp, so just deal massive damage
                    dealDamage = float.MaxValue;
                    instantKilled = true;
                }

                bossHealth.TakeDamage(dealDamage);

                Vector3 hitPoint = collision.ClosestPoint(transform.position);
                ShowDamageText(hitPoint, instantKilled ? damage : dealDamage);

                // Apply on-hit effects (burn, freeze, chain lightning, etc.)
                if (onHitEffects.Count > 0 && collision.TryGetComponent<IEntity>(out var entity))
                {
                    for (int i = 0; i < onHitEffects.Count; i++)
                        onHitEffects[i]?.Apply(entity);
                }
            }

            Deactivate();
        }
        // Handles boss-side objects that implement IDamageablee (double-e)
        private void OnHitBossWeapon(Collider2D collision)
        {
            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                //damageable.TakeDamage(damage);
                Destroy(gameObject);
                //ShowDamageText(collision.ClosestPoint(transform.position), damage);
            }

            //Deactivate();
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
                OnHitTarget(other.collider);
        }
      

        protected virtual void OnHitTarget(Collider2D collision)
        {
            Vector3 hitPoint = collision.ClosestPoint(transform.position);

            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                // ── Instant kill check (Pierce power-up) ─────
                // Roll once per hit. If it procs, deal damage
                // equal to the target's remaining HP = guaranteed kill.
                bool instantKilled = false;
                if (instantKillChance > 0f && UnityEngine.Random.value <= instantKillChance)
                {
                    damageable.TakeDamage(damageable.CurrentHp);
                    instantKilled = true;
                }
                else
                {
                    damageable.TakeDamage((int)damage);
                }

                float shownDamage = instantKilled ? damageable.MaxHp : damage;

                if (collision.CompareTag("Egg"))
                    GameEvents.FireEggHit(damageable, (int)damage, hitPoint);
                else if (collision.CompareTag("Bird"))
                    GameEvents.FireBirdHit(damageable, (int)damage, hitPoint);

                ShowDamageText(hitPoint, shownDamage);

                // Apply on-hit effects from power-ups (chain lightning, burn, freeze, etc.)
                if (onHitEffects.Count > 0 && collision.TryGetComponent<IEntity>(out var entity))
                {
                    for (int i = 0; i < onHitEffects.Count; i++)
                        onHitEffects[i]?.Apply(entity);
                }

                if (collision.CompareTag("Egg"))
                    ApplyHitImpulse(collision);
            }

            HandlePostHit(collision);
        }

        protected virtual void ApplyHitImpulse(Collider2D collision)
        {
            if (!applyHitImpulse) return;

            collision.TryGetComponent<Egg>(out var egg);
            if (egg == null) return;
            
            egg.ApplyBulletHitForce();
        }

        protected virtual void HandlePostHit(Collider2D collision)
        {
            Deactivate();
        }

        protected abstract void HandleMovement();

        protected void ShowDamageText(Vector3 position, float amount)
        {
            GameObject go = null;

            // Drain destroyed pooled entries (scene reloads can leave dangling refs).
            while (go == null && GamePoolManager.bulletDamageTextQueue.Count > 0)
            {
                go = GamePoolManager.bulletDamageTextQueue.Dequeue();
                if (go == null) continue;
                go.transform.position = position;
                go.SetActive(true);
            }

            if (go == null && damageTextPrefab != null)
            {
                go = Instantiate(damageTextPrefab, position, Quaternion.identity);
            }

            if (go != null)
            {
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = $"-{amount:0}";
                    tmp.fontSize = 8;
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

        private void DestroyAttachedVfx()
        {
            for (int i = attachedVfx.Count - 1; i >= 0; i--)
            {
                if (attachedVfx[i] != null)
                    Destroy(attachedVfx[i]);
            }
            attachedVfx.Clear();
        }
    }
}