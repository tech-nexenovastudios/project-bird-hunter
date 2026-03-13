using System.Collections.Generic;
using Gameplay.Events;
using Gameplay.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Gameplay.Player
{
    public abstract class BaseCannon : MonoBehaviour, IDamageable, IDamageEffect
    {
        [Header("References")]
        [SerializeField] protected Transform[] gunTips;
        [SerializeField] protected ParticleSystem[] fireParticles;
        [SerializeField] protected Transform[] wheels;
        [SerializeField] protected float wheelRadius = 0.5f;

        [Header("Runtime UI (Assigned by Spawner)")]
        [SerializeField] private Image cannonHealthBar;
        [SerializeField] private TextMeshProUGUI cannonHealthText;

        [Header("Collision Detection")]
        [Tooltip("Only GameObjects on these layers will be reported.")]
        [SerializeField] private LayerMask detectableLayers = ~0;
        [Tooltip("React to trigger colliders.")]
        [SerializeField] private bool useTriggers = true;
        [Tooltip("React to solid (non-trigger) colliders.")]
        [SerializeField] private bool useColliders = true;
        [Tooltip("Minimum relative velocity magnitude to register a collision. 0 = any.")]
        [SerializeField] private float minimumImpactForce = 0f;
        
        [Header("Bounds Settings")]
        [SerializeField] private Camera mainCam;
        private float halfWidth;

        // ─────────────────────────────────────────────
        //  Properties
        // ─────────────────────────────────────────────

        public int CurrentHp { get; private set; }
        public int MaxHp => CannonStats != null ? Mathf.RoundToInt(CannonStats.currentMaxHealth) : 100;
        public bool IsAlive => CurrentHp > 0;
        public bool IsFiring { get; private set; } = true;
        public CannonStats CannonStats { get; private set; }
        public GameObject BulletPrefab { get; set; }

        // ─────────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────────

        protected Rigidbody2D rb;
        private float fireTimer;
        private Vector2 movementInput;
        
        private ObjectPool<BaseBullet> bulletPool;

        private readonly HashSet<Collider2D> _activeOverlaps = new ();

        // ─────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public virtual void Configure(CannonStats stats, GameObject bulletPrefab)
        {
            mainCam = Camera.main;
            
            var col = GetComponent<Collider2D>();
            halfWidth = col.bounds.extents.x;
            
            CannonStats  = stats;
            BulletPrefab = bulletPrefab;

            CannonStats.InitRuntime();
            CurrentHp = MaxHp;

            bulletPool = new ObjectPool<BaseBullet>(CreateProjectile, OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject, true, 10, 20);

            UpdateUI();
            GameEvents.OnPlayerLevelUp += OnLevelUp;
        }

        private void OnDestroy()
        {
            GameEvents.OnPlayerLevelUp -= OnLevelUp;
        }

        protected virtual void Update()
        {
            HandleInput();
            HandleFiring();
        }

        protected virtual void FixedUpdate()
        {
            HandleMovement();
        }

        // ─────────────────────────────────────────────
        //  Child Part Relay
        //  Called by CannonPartCollider on each child part.
        //  All colliders live on child parts — no root collider needed.
        // ─────────────────────────────────────────────

        internal void ReceivePartCollisionEnter(CannonPartCollider part, Collision2D col)
        {
            if (!useColliders) return;
            if (!IsLayerDetectable(col.gameObject.layer)) return;
            if (col.relativeVelocity.magnitude < minimumImpactForce) return;

            HandleCollisionEnter(BuildData(
                col.gameObject,
                col.contacts[0].point,
                col.contacts[0].normal,
                col.relativeVelocity.magnitude,
                CollisionEventType.CollisionEnter,
                part));
        }

        internal void ReceivePartCollisionExit(CannonPartCollider part, Collision2D col)
        {
            if (!useColliders) return;
            if (!IsLayerDetectable(col.gameObject.layer)) return;

            HandleCollisionExit(BuildData(
                col.gameObject,
                transform.position,
                Vector2.zero, 0f,
                CollisionEventType.CollisionExit,
                part));
        }

        internal void ReceivePartTriggerEnter(CannonPartCollider part, Collider2D other)
        {
            if (!useTriggers) return;
            if (!IsLayerDetectable(other.gameObject.layer)) return;
            if (_activeOverlaps.Contains(other)) return;

            _activeOverlaps.Add(other);

            HandleTriggerEnter(BuildData(
                other.gameObject,
                other.ClosestPoint(transform.position),
                Vector2.zero, 0f,
                CollisionEventType.TriggerEnter,
                part));
        }

        internal void ReceivePartTriggerExit(CannonPartCollider part, Collider2D other)
        {
            _activeOverlaps.Remove(other);

            if (!useTriggers) return;
            if (!IsLayerDetectable(other.gameObject.layer)) return;

            HandleTriggerExit(BuildData(
                other.gameObject,
                transform.position,
                Vector2.zero, 0f,
                CollisionEventType.TriggerExit,
                part));
        }

        // ─────────────────────────────────────────────
        //  Protected Virtual Collision Hooks
        //
        //  Damage is NOT applied here.
        //  Egg.HandleCollision() calls TakeDamage() on IDamageable directly,
        //  scaled by data.hitPart.damageMultiplier before reaching these hooks.
        //
        //  Override in subclasses to add reactions only:
        //  VFX, audio, animation, screen shake, etc.
        //
        //  data.hitPart           — CannonPartCollider that was hit
        //  data.HitPartType       — Body / Barrel / Wheel / Shield / Base
        //  data.hitPart.damageMultiplier — multiplier already used by Egg
        // ─────────────────────────────────────────────

        /// <summary>Called when a child part is hit by a solid collider.</summary>
        protected virtual void HandleCollisionEnter(CannonCollisionData data) { }

        /// <summary>Called when a child part separates from a solid collider.</summary>
        protected virtual void HandleCollisionExit(CannonCollisionData data) { }

        /// <summary>Called when a child part is overlapped by a trigger.</summary>
        protected virtual void HandleTriggerEnter(CannonCollisionData data) { }

        /// <summary>Called when a trigger stops overlapping a child part.</summary>
        protected virtual void HandleTriggerExit(CannonCollisionData data) { }

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        private bool IsLayerDetectable(int layer) =>
            (detectableLayers.value & (1 << layer)) != 0;

        private CannonCollisionData BuildData(GameObject other, Vector2 point,
                                              Vector2 normal, float force,
                                              CollisionEventType type,
                                              CannonPartCollider hitPart)
        {
            return new CannonCollisionData
            {
                sourceCannon  = this,
                hitPart       = hitPart,
                otherObject   = other,
                contactNormal = normal,
                impactForce   = force,
                eventType     = type,
                timestamp     = Time.time
            };
        }

        // ─────────────────────────────────────────────
        //  Input / Movement / Firing
        // ─────────────────────────────────────────────

        private void OnLevelUp(int newLevel)
        {
            if (CannonStats != null)
            {
                CannonStats.ApplyProgression(newLevel);
                UpdateUI();
            }
        }

        protected virtual void HandleInput()
        {
            if (Gameplay.Managers.GameManager.Instance != null &&
                Gameplay.Managers.GameManager.Instance.state != GameState.Gameplay)
            {
                movementInput = Vector2.zero;
                return;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float direction  = mousePos.x > transform.position.x ? 1f : -1f;

                movementInput = Mathf.Abs(mousePos.x - transform.position.x) < 0.1f
                    ? Vector2.zero
                    : new Vector2(direction, 0);
            }
            else
            {
                movementInput = Vector2.zero;
            }
        }

        protected virtual void HandleMovement()
        {
            if (CannonStats == null) return;

            Vector2 oldPos = rb.position;
            Vector2 newPos = rb.position + movementInput * (CannonStats.currentMoveSpeed * Time.fixedDeltaTime);

            float leftBound  = mainCam.ViewportToWorldPoint(Vector3.zero).x  + halfWidth;
            float rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;
            
            newPos.x = Mathf.Clamp(newPos.x, leftBound, rightBound);

            rb.MovePosition(newPos);

            if (wheels != null && wheels.Length > 0)
            {
                float distanceMoved = newPos.x - oldPos.x;
                float rotationAngle = -(distanceMoved / (2 * Mathf.PI * wheelRadius)) * 360f;
                foreach (var wheel in wheels)
                    if (wheel != null) wheel.Rotate(0, 0, rotationAngle);
            }
        }

        public void StartFiring() => IsFiring = true;
        public void StopFiring()  => IsFiring = false;

        protected virtual void HandleFiring()
        {
            if (CannonStats == null || BulletPrefab == null || !IsFiring) return;

            fireTimer += Time.deltaTime;
            if (fireTimer >= 1f / CannonStats.currentFireRate)
            {
                Shoot();
                fireTimer = 0;
            }
        }

        protected virtual void Shoot()
        {
            if (gunTips == null || gunTips.Length == 0)
            {
                SpawnBullet(transform.position, transform.rotation);
            }
            else
            {
                foreach (var tip in gunTips)
                    if (tip != null) SpawnBullet(tip.position, tip.rotation);
            }

            if (fireParticles != null)
                foreach (var p in fireParticles)
                    if (p != null) p.Play();
        }

        protected virtual void SpawnBullet(Vector3 position, Quaternion rotation)
        {
            if (bulletPool != null)
            {
                BaseBullet pooledBullet = bulletPool.Get();
                pooledBullet.transform.SetPositionAndRotation(position, rotation);
                pooledBullet.Init(CannonStats);
                pooledBullet.damage      = CannonStats.currentBulletDamage;
                pooledBullet.bulletSpeed = CannonStats.currentBulletSpeed;

                if (pooledBullet is BouncingBullet bouncing)
                    bouncing.bounceCount = CannonStats.bulletBounce;
                return;
            }
        }

        // ─────────────────────────────────────────────
        //  IDamageable
        // ─────────────────────────────────────────────

        public virtual void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            CurrentHp = Mathf.Clamp(CurrentHp - damage, 0, MaxHp);
            UpdateUI();
            GameEvents.FireCannonHit(damage);

            if (CurrentHp <= 0) Die();
        }

        protected virtual void UpdateUI()
        {
            if (cannonHealthBar != null)
            {
                float fill = (float)CurrentHp / MaxHp;
                cannonHealthBar.fillAmount = fill;
                cannonHealthBar.color = fill > 0.6f ? Color.green
                                      : fill > 0.3f ? Color.yellow
                                      : Color.red;
            }

            if (cannonHealthText != null)
                cannonHealthText.text = $"{CurrentHp}/{MaxHp}";
        }

        protected virtual void Die()
        {
            IsFiring      = false;
            movementInput = Vector2.zero;
            GameEvents.FirePlayerDeath();
        }

        // ─────────────────────────────────────────────
        //  IDamageEffect
        // ─────────────────────────────────────────────

        public virtual void ElectricDamage(float applyDamage, float effectTime) => TakeDamage(Mathf.RoundToInt(applyDamage));

        public virtual void igniteDamage(float applyDamage, float effectTime) => TakeDamage(Mathf.RoundToInt(applyDamage));

        public virtual void PoisonDamage(float applyDamage, float effectTime) => TakeDamage(Mathf.RoundToInt(applyDamage));

        public virtual void FreezeEffect(float effectTime) { }

        // ─────────────────────────────────────────────
        //  Object Pool
        // ─────────────────────────────────────────────

        private BaseBullet CreateProjectile()
        {
            GameObject go     = Instantiate(BulletPrefab);
            BaseBullet bullet = go.GetComponent<BaseBullet>();
            bullet.SetReleaseAction(ReleaseBulletToPool);
            go.SetActive(false);
            return bullet;
        }

        private void OnGetFromPool(BaseBullet obj)
        {
            if (obj != null) obj.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(BaseBullet obj)
        {
            if (obj == null) return;
            obj.transform.SetParent(null);
            if (obj.TryGetComponent<Rigidbody2D>(out var r))
            {
                r.linearVelocity  = Vector2.zero;
                r.angularVelocity = 0f;
            }
            obj.gameObject.SetActive(false);
        }

        private void OnDestroyPooledObject(BaseBullet obj)
        {
            if (obj != null) Destroy(obj.gameObject);
        }

        private void ReleaseBulletToPool(BaseBullet bullet)
        {
            if (bulletPool != null) bulletPool.Release(bullet);
            else if (bullet != null) Destroy(bullet.gameObject);
        }

        // ─────────────────────────────────────────────
        //  Editor Gizmo
        // ─────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            foreach (var part in GetComponentsInChildren<CannonPartCollider>())
            {
                var col = part.GetComponent<Collider2D>();
                if (col == null) continue;
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
                Gizmos.DrawWireCube(part.transform.position, col.bounds.size * 1.1f);
            }
        }
#endif
    }
}