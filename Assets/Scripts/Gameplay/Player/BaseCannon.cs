using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.Pool;

namespace Gameplay.Player
{
    public abstract class BaseCannon : MonoBehaviour, ICannon
    {
        [Header("References")]
        [SerializeField] protected Transform[] gunTips;
        [SerializeField] protected Transform[] wheels;
        [SerializeField] protected float wheelRadius = 0.5f;

        // ── Health ───────────────────────────────────────────
        public int CurrentHp { get; private set; }

        private int maxHpBonus;
        public int MaxHp
        {
            get
            {
                int baseMax = CannonStats != null ? Mathf.RoundToInt(CannonStats.currentMaxHealth) : 100;
                return baseMax + maxHpBonus;
            }
        }

        public bool IsAlive => CurrentHp > 0;
        public bool IsFiring { get; private set; } = true;
        public bool SuppressBullets { get; set; }

        // ── Attack modifiers ─────────────────────────────────
        public int BaseAttack => CannonStats != null
            ? Mathf.RoundToInt(CannonStats.currentBulletDamage) : 1;

        private float flatAttackBonus;
        private float percentAttackBonus;

        public int CurrentAttack
        {
            get
            {
                float modified = (BaseAttack + flatAttackBonus) * (1f + percentAttackBonus);
                return Mathf.Max(1, Mathf.RoundToInt(modified));
            }
        }

        // ── Defensive properties ─────────────────────────────
        public bool IsInvincible { get; set; }
        private Vector3 originalLocalScale;  // originalBoxSize(cannon)
        private float originalWheelRadius ;
        private float hitboxScale = 1f;
        public float HitboxScale
        {
            get => hitboxScale;
            set
            {
                hitboxScale = Mathf.Clamp(value, 0.1f, 2f);
                ApplyHitboxScale();
            }
        }

        public int ShieldHits { get; set; }
        public bool HasRevive { get; set; }
        public float ReviveHealthPercent { get; set; }
        public Transform Transform => transform;

        // ── Stats & Prefab ───────────────────────────────────
        public CannonStats CannonStats { get; private set; }
        public float ManaFillRateBonus { get; set; }
        public GameObject BulletPrefab { get; set; }

        protected Rigidbody2D rb;

        private float fireTimer;
        private Vector2 movementInput;
        private ObjectPool<BaseBullet> bulletPool;
        private Camera mainCam;
        private float halfWidth;

        private Collider2D cannonCollider;
        private Vector2 originalBoxSize;
        private float originalCircleRadius;

        private readonly List<IProjectileModifier> activeProjectileMods = new();

        // ── VFX hooks ────────────────────────────────────────
        protected virtual void OnShootVFX() { }
        protected virtual void OnDamageTakenVFX(int damage) { }
        protected virtual void OnDeathVFX() { }
        protected virtual void OnHealthChanged(int currentHp, int maxHp) { }
        protected virtual void OnHealVFX(int amount) { }
        protected virtual void OnShieldAbsorbVFX() { }
        protected virtual void OnReviveVFX() { }

        // ════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════════════════════

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            mainCam = Camera.main;

            cannonCollider = GetComponent<Collider2D>();
            halfWidth = cannonCollider != null ? cannonCollider.bounds.extents.x : 0.5f;

            if (cannonCollider is BoxCollider2D box)
                originalBoxSize = box.size;
            else if (cannonCollider is CircleCollider2D circle)
                originalCircleRadius = circle.radius;
            originalLocalScale = transform.localScale;
            originalWheelRadius = wheelRadius;
        }

        public virtual void Configure(CannonStats stats, GameObject bulletPrefab)
        {
            ManaFillRateBonus = 0f;
            CannonStats = stats;
            BulletPrefab = bulletPrefab;
            CannonStats.InitRuntime();

            flatAttackBonus = 0f;
            percentAttackBonus = 0f;
            maxHpBonus = 0;
            IsInvincible = false;
            hitboxScale = 1f;
            transform.localScale = originalLocalScale;
            wheelRadius = originalWheelRadius;
            ShieldHits = 0;
            HasRevive = false;
            ReviveHealthPercent = 0f;

            CurrentHp = MaxHp;

            if (BulletPrefab != null && BulletPrefab.GetComponent<BaseBullet>() != null)
            {
                bulletPool = new ObjectPool<BaseBullet>(
                    CreateProjectile, OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
                    collectionCheck: true, defaultCapacity: 10, maxSize: 20);
            }
            else
            {
                bulletPool = null;
            }

            GameEvents.FireCannonStatsUpdated(CannonStats);
            GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp);
            GameEvents.OnPlayerLevelUp += OnLevelUp;
        }
        public bool IsMoving => movementInput.sqrMagnitude > 0.0001f;
        protected virtual void OnDestroy()
        {
            GameEvents.OnPlayerLevelUp -= OnLevelUp;
            transform.DOKill();
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
        // ── Speed modifier ────────────────────────────────────
        private float moveSpeedMultiplier = 1f;

        public void ApplySpeedMultiplier(float multiplier)
        {
            Debug.Log("Applying speed multiplier: " + multiplier);
            moveSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void ResetSpeedMultiplier()
        {
            Debug.Log("Resetting speed multiplier to 1");
            moveSpeedMultiplier = 1f;
        }
        // ════════════════════════════════════════════════════════
        //  ICannon — ATTACK MODIFIERS
        // ════════════════════════════════════════════════════════

        public void AddAttackModifier(float flatBonus, float percentBonus)
        {
            flatAttackBonus += flatBonus;
            percentAttackBonus += percentBonus/100;
            Debug.Log($"Damage per bullet increased by - {percentBonus} % ");
        }

        public void RemoveAttackModifier(float flatBonus, float percentBonus)
        {
            flatAttackBonus -= flatBonus;
            percentAttackBonus -= percentBonus/100;
        }

        public void IncreaseMaxHp(int amount)
        {
            if (amount <= 0) return;
            maxHpBonus += amount;
            CurrentHp += amount;
            OnHealthChanged(CurrentHp, MaxHp);
            GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp);
            Debug.Log("Max HP increased by " + amount + ". New Max HP: " + MaxHp);
        }

        // ════════════════════════════════════════════════════════
        //  IEntity — HEAL
        // ════════════════════════════════════════════════════════

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
            OnHealVFX(amount);
            OnHealthChanged(CurrentHp, MaxHp);
            GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp);
        }

        // ════════════════════════════════════════════════════════
        //  DAMAGE & DEATH
        // ════════════════════════════════════════════════════════

        public virtual void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            if (IsInvincible)
            {
                Debug.Log("Cannon is invincible -> Not taking any damage!");
                return;
            }
            if (ShieldHits > 0)
            {
                ShieldHits--;
                OnShieldAbsorbVFX();
                return;
            }

            CurrentHp -= damage;
            CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
            Debug.Log("Cannon took " + damage + "damage");
            OnDamageTakenVFX(damage);
            OnHealthChanged(CurrentHp, MaxHp);
            GameEvents.FireCannonHit(damage);
            GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp);

            if (CurrentHp <= 0) Die();
        }

        protected virtual void Die()
        {
            if (HasRevive)
            {
                HasRevive = false;
                CurrentHp = Mathf.Max(1, Mathf.CeilToInt(MaxHp * ReviveHealthPercent));
                OnReviveVFX();
                OnHealthChanged(CurrentHp, MaxHp);
                GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp);
                return;
            }

            IsFiring = false;
            movementInput = Vector2.zero;
            OnDeathVFX();
            GameEvents.FirePlayerDeath();
        }

        // ════════════════════════════════════════════════════════
        //  HITBOX SCALING
        // ════════════════════════════════════════════════════════

        private void ApplyHitboxScale()
        {
            // Scale the visual (transform)
            transform.localScale = originalLocalScale * hitboxScale;

            // Scale the collider independently (since transform scale
            // already affects collider bounds, we reset collider to
            // original values so we don't double-scale)
            if (cannonCollider is BoxCollider2D box)
                box.size = originalBoxSize;
            else if (cannonCollider is CircleCollider2D circle)
                circle.radius = originalCircleRadius;
            wheelRadius = originalWheelRadius * hitboxScale;
        }

        // ════════════════════════════════════════════════════════
        //  PROJECTILE MODIFIER REGISTRATION
        // ════════════════════════════════════════════════════════

        public void RegisterProjectileModifier(IProjectileModifier mod)
        {
            if (mod != null && !activeProjectileMods.Contains(mod))
                activeProjectileMods.Add(mod);
        }

        public void UnregisterProjectileModifier(IProjectileModifier mod)
        {
            if (mod != null) activeProjectileMods.Remove(mod);
        }

        // ════════════════════════════════════════════════════════
        //  INPUT & MOVEMENT
        // ════════════════════════════════════════════════════════

        protected virtual void HandleInput()
        {
            if (Gameplay.Managers.GameManager.Instance != null && Gameplay.Managers.GameManager.Instance.state != GameState.Gameplay)
            {
                movementInput = Vector2.zero;
                return;
            }

            if (Input.GetMouseButton(0))
            {
                StartFiring();
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float delta = mousePos.x - transform.position.x;

                if (Mathf.Abs(delta) < 0.1f)
                {
                    movementInput = Vector2.zero;
                }
                else
                {
                    float inputStrength = Mathf.InverseLerp(0.1f, 1.5f, Mathf.Abs(delta));
                    movementInput = new Vector2(Mathf.Sign(delta) * inputStrength, 0);
                }
            }
            else
            {
                movementInput = Vector2.zero;
                StopFiring();
            }
        }

        protected virtual void HandleMovement()
        {
            if (CannonStats == null) return;

            Vector2 oldPos = rb.position;
            // was:  movementInput * (CannonStats.currentMoveSpeed * Time.fixedDeltaTime)
            Vector2 newPos = rb.position + movementInput * (CannonStats.currentMoveSpeed * moveSpeedMultiplier * Time.fixedDeltaTime);

            float leftBound = mainCam.ViewportToWorldPoint(Vector3.zero).x + halfWidth;
            float rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;

            newPos.x = Mathf.Clamp(newPos.x, leftBound, rightBound);
            rb.MovePosition(newPos);

            if (wheels != null && wheels.Length > 0)
            {
                float distanceMoved = newPos.x - oldPos.x;
                float rotationAngle = -(distanceMoved / (2 * Mathf.PI * wheelRadius)) * 360f;

                for (int i = 0; i < wheels.Length; i++)
                    if (wheels[i] != null)
                        wheels[i].Rotate(0, 0, rotationAngle);
            }
        }

        // ════════════════════════════════════════════════════════
        //  FIRING
        // ════════════════════════════════════════════════════════

        public void StartFiring() => IsFiring = true;
        public void StopFiring() => IsFiring = false;

        protected virtual void HandleFiring()
        {
            if (CannonStats == null || BulletPrefab == null || !IsFiring) return;
            if (SuppressBullets) return;

            fireTimer += Time.deltaTime;
            float interval = 1f / CannonStats.currentFireRate;

            if (fireTimer >= interval)
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
                for (int i = 0; i < gunTips.Length; i++)
                    if (gunTips[i] != null)
                        SpawnBullet(gunTips[i].position, gunTips[i].rotation);
            }
            OnShootVFX();
        }

        /// <summary>
        /// Spawns a bullet, applies all projectile modifiers, spawns extras.
        /// Protected virtual so subclasses (TripleCannon, etc.) can call it.
        /// </summary>
        protected virtual void SpawnBullet(Vector3 position, Quaternion rotation)
        {
            BaseBullet bullet = CreateBullet(position, rotation);
            if (bullet == null) return;

            for (int i = 0; i < activeProjectileMods.Count; i++)
            {
                IProjectileModifier mod = activeProjectileMods[i];
                if (mod == null || !mod.IsActive) continue;

                mod.ModifyBullet(bullet, CurrentAttack);

                int extras = mod.ExtraProjectiles;
                if (extras <= 0) continue;

                float[] angles = mod.GetExtraAngles();
                for (int e = 0; e < extras && e < angles.Length; e++)
                {
                    Quaternion extraRot = rotation * Quaternion.Euler(0, 0, angles[e]);
                    CreateBullet(position, extraRot);
                }
            }
        }

        private BaseBullet CreateBullet(Vector3 position, Quaternion rotation)
        {
            BaseBullet bullet;

            if (bulletPool != null)
            {
                bullet = bulletPool.Get();
                bullet.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                GameObject go = Instantiate(BulletPrefab, position, rotation);
                bullet = go.GetComponent<BaseBullet>();
                if (bullet == null) return null;
            }

            // Derive the fire direction from the spawn rotation so Init
            // gets the exact direction the bullet is aimed at, regardless
            // of when transform.up is read later.
            Vector2 fireDirection = rotation * Vector2.up;

            bullet.Init(CannonStats, fireDirection);   // ← updated signature
            bullet.damage = CurrentAttack;
            bullet.bulletSpeed = CannonStats.currentBulletSpeed;

            if (bullet is BouncingBullet bouncing)
                bouncing.bounceCount = CannonStats.bulletBounce;

            return bullet;
        }

        // ════════════════════════════════════════════════════════
        //  PROGRESSION
        // ════════════════════════════════════════════════════════

        private void OnLevelUp(int newLevel)
        {
            if (CannonStats != null)
            {
                CannonStats.ApplyProgression(newLevel);
                OnHealthChanged(CurrentHp, MaxHp);
            }
        }

        // ════════════════════════════════════════════════════════
        //  POOL CALLBACKS
        // ════════════════════════════════════════════════════════

        private void OnDestroyPooledObject(BaseBullet obj)
        {
            if (obj != null) Destroy(obj.gameObject);
        }

        private void OnReleaseToPool(BaseBullet obj)
        {
            if (obj == null) return;
            obj.transform.SetParent(null);
            if (obj.TryGetComponent<Rigidbody2D>(out var bulletRb))
            {
                bulletRb.linearVelocity = Vector2.zero;
                bulletRb.angularVelocity = 0f;
            }
            obj.gameObject.SetActive(false);
        }

        private void OnGetFromPool(BaseBullet obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(true);
        }

        private BaseBullet CreateProjectile()
        {
            GameObject go = Instantiate(BulletPrefab);
            BaseBullet bullet = go.GetComponent<BaseBullet>();
            bullet.SetReleaseAction(ReleaseBulletToPool);
            go.SetActive(false);
            return bullet;
        }

        private void ReleaseBulletToPool(BaseBullet bullet)
        {
            if (bulletPool != null)
                bulletPool.Release(bullet);
            else if (bullet != null)
                Destroy(bullet.gameObject);
        }

        // ════════════════════════════════════════════════════════
        //  GIZMOS
        // ════════════════════════════════════════════════════════

        protected virtual void OnDrawGizmos()
        {
            if (wheels == null) return;
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.DrawWireDisc(wheels[i].position, Vector3.back, wheelRadius);
#endif
                }
            }
        }
    }
}