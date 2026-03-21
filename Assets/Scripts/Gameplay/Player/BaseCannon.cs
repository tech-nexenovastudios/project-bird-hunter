using Gameplay.Eggs;
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
        

        [Header("Egg Hit VFX")]
        [SerializeField] private ParticleSystem hitVFX;         // Drag your VFX child here
        [SerializeField] private Transform hitVFXPosition;       // Drag an empty child Transform here
        // positioned where you want the effect
        [Header("Rest")]
        public int CurrentHp { get; private set; }
        public int MaxHp => CannonStats != null ? Mathf.RoundToInt(CannonStats.currentMaxHealth) : 100;
        public bool IsAlive => CurrentHp > 0;

        public bool IsFiring { get; private set; } = true;

        public CannonStats CannonStats { get; private set; }
        public GameObject BulletPrefab { get; set; }

        protected Rigidbody2D rb;

        private float fireTimer;
        private Vector2 movementInput;
        private ObjectPool<BaseBullet> bulletPool;
        private Camera mainCam;
        private float halfWidth;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            mainCam = Camera.main;

            var col = GetComponent<Collider2D>();
            halfWidth = col.bounds.extents.x;
        }

        //public virtual void Configure(CannonStats stats, LevelReferences references, GameObject bulletPrefab)
        //{
        //    CannonStats = stats;
        //    BulletPrefab = bulletPrefab;

        //    CannonStats.InitRuntime();
        //    CurrentHp = MaxHp;

        //    var (bar, text) = references.GetCannonHealth();

        //    var (left, right) = references.GetWall();
        //    leftWall = left;
        //    rightWall = right;

        public virtual void Configure(CannonStats stats, GameObject bulletPrefab)
        {
            CannonStats = stats;
            BulletPrefab = bulletPrefab;
            CannonStats.InitRuntime();
            CurrentHp = MaxHp;

            if (BulletPrefab != null && BulletPrefab.GetComponent<BaseBullet>() != null)
            {
                bulletPool = new ObjectPool<BaseBullet>(
                    CreateProjectile,
                    OnGetFromPool,
                    OnReleaseToPool,
                    OnDestroyPooledObject,
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 20);
            }
            else
            {
                bulletPool = null;
            }
            GameEvents.FireCannonStatsUpdated(CannonStats);

            GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp);

            GameEvents.OnPlayerLevelUp += OnLevelUp;
        }

        private void OnDestroyPooledObject(BaseBullet obj)
        {
            if (obj != null)
            {
                Destroy(obj.gameObject);
            }
        }

        private void OnReleaseToPool(BaseBullet obj)
        {
            if (obj == null) return;

            var bulletTransform = obj.transform;
            bulletTransform.SetParent(null);

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
            {
                bulletPool.Release(bullet);
            }
            else if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }
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

        private void OnLevelUp(int newLevel)
        {
            if (CannonStats != null)
            {
                CannonStats.ApplyProgression(newLevel);
                // Optionally heal on level up or just update max HP
                // CurrentHp = MaxHp; 
                //UpdateUI();
            }
        }

        protected virtual void HandleInput()
        {
            if (Gameplay.Managers.GameManager.Instance != null && Gameplay.Managers.GameManager.Instance.state != GameState.Gameplay)
            {
                movementInput = Vector2.zero;
                return;
            }

            // Simple touch/mouse input for movement
            if (Input.GetMouseButton(0))
            {
                StartFiring();
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float direction = mousePos.x > transform.position.x ? 1f : -1f;
                
                // If very close to mouse, stop jittering
                if (Mathf.Abs(mousePos.x - transform.position.x) < 0.1f)
                    movementInput = Vector2.zero;
                else
                    movementInput = new Vector2(direction, 0);
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
            Vector2 newPos = rb.position + movementInput * (CannonStats.currentMoveSpeed * Time.fixedDeltaTime);

            Vector3 pos = transform.position;

            // Get camera bounds in world units
            float leftBound = mainCam.ViewportToWorldPoint(Vector3.zero).x + halfWidth;
            float rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;

            // Assuming cannon has its own width, we might need an offset. 
            // For now, simple point clamp or use collider bounds if available
            newPos.x = Mathf.Clamp(newPos.x, leftBound, rightBound);
            
            rb.MovePosition(newPos);

            // Rotate wheels based on distance moved
            if (wheels != null && wheels.Length > 0)
            {
                float distanceMoved = newPos.x - oldPos.x;
                float rotationAngle = -(distanceMoved / (2 * Mathf.PI * wheelRadius)) * 360f;

                foreach (var wheel in wheels)
                {
                    if (wheel != null)
                        wheel.Rotate(0, 0, rotationAngle);
                }
            }
        }

        public void StartFiring() => IsFiring = true;
        public void StopFiring() => IsFiring = false;

        protected virtual void HandleFiring()
        {
            if (CannonStats == null || BulletPrefab == null || !IsFiring) return;

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
                foreach (var tip in gunTips)
                {
                    if (tip != null)
                        SpawnBullet(tip.position, tip.rotation);
                }
            }

            if (fireParticles != null)
            {
                foreach (var p in fireParticles)
                {
                    if (p != null) p.Play();
                }
            }
        }

        protected virtual void SpawnBullet(Vector3 position, Quaternion rotation)
        {
            if (bulletPool != null)
            {
                BaseBullet pooledBullet = bulletPool.Get();
                Transform bulletTransform = pooledBullet.transform;

                bulletTransform.SetPositionAndRotation(position, rotation);
                pooledBullet.Init(CannonStats);
                pooledBullet.damage = CannonStats.currentBulletDamage;
                pooledBullet.bulletSpeed = CannonStats.currentBulletSpeed;

                if (pooledBullet is BouncingBullet bouncing)
                {
                    bouncing.bounceCount = CannonStats.bulletBounce;
                }

                return;
            }

            GameObject go = Instantiate(BulletPrefab, position, rotation);

            if (go.TryGetComponent<BaseBullet>(out var newBullet))
            {
                newBullet.Init(CannonStats);
                newBullet.damage = CannonStats.currentBulletDamage;
                newBullet.bulletSpeed = CannonStats.currentBulletSpeed;

                if (newBullet is BouncingBullet bouncing)
                {
                    bouncing.bounceCount = CannonStats.bulletBounce;
                }

                if (newBullet is ElementalBullet)
                {
                }
            }
            else if (go.TryGetComponent<Bullet>(out var bullet))
            {
                bullet.Damage = CannonStats.currentBulletDamage;
                bullet.bulletSpeed = CannonStats.currentBulletSpeed;
                bullet.bulletBounce = CannonStats.bulletBounce;
            }
        }

        public virtual void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            CurrentHp -= damage;
            CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
            
            // ── Play hit VFX at the designated position ──
            PlayHitVFX();

            GameEvents.FireCannonHit(damage);
            GameEvents.FireCannonHealthChanged(CurrentHp, MaxHp); //

            if (CurrentHp <= 0) Die();
        }

        private void PlayHitVFX()
        {
            if (hitVFX == null) return;

            // Move VFX to the hit position (if you assigned one)
            if (hitVFXPosition != null)
            {
                hitVFX.transform.position = hitVFXPosition.position;
            }

            // Stop any currently playing instance so rapid hits restart cleanly
            hitVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitVFX.Play(true);
        }

        //protected virtual void UpdateUI()
        //{

        //}
        protected virtual void Die()
        {
            Debug.Log("Cannon Destroyed!");
            IsFiring = false;
            movementInput = Vector2.zero;
            
            GameEvents.FirePlayerDeath();
        }

      

        #region IDamageEffect Implementation

        public virtual void ElectricDamage(float applyDamage, float effectTime)
        {
            // Apply damage over time or immediate electric effect
            TakeDamage(Mathf.RoundToInt(applyDamage));
            // Additional electric effect logic (e.g. visual or slowdown) can be added here
        }

        public virtual void igniteDamage(float applyDamage, float effectTime)
        {
            // Apply fire/burn effect
            TakeDamage(Mathf.RoundToInt(applyDamage));
            // Additional ignite effect logic can be added here
        }

        public virtual void PoisonDamage(float applyDamage, float effectTime)
        {
            // Apply poison damage
            TakeDamage(Mathf.RoundToInt(applyDamage));
        }

        public virtual void FreezeEffect(float effectTime)
        {
            // Apply freeze effect (e.g. slow down movement)
            // We could reduce moveSpeed for effectTime
        }

        #endregion
    }
}