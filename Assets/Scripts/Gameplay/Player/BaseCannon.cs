using Gameplay.Events;
using Gameplay.Interfaces;
using TMPro;
using UnityEngine;
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

        public int CurrentHp { get; private set; }
        public int MaxHp => CannonStats != null ? Mathf.RoundToInt(CannonStats.currentMaxHealth) : 100;
        public bool IsAlive => CurrentHp > 0;

        public bool IsFiring { get; private set; } = true;

        public CannonStats CannonStats { get; private set; }
        public GameObject BulletPrefab { get; set; }

        protected Rigidbody2D rb;
        private Collider2D leftWall;
        private Collider2D rightWall;
        
        private float fireTimer;
        private Vector2 movementInput;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public virtual void Configure(CannonStats stats, LevelReferences references, GameObject bulletPrefab)
        {
            CannonStats = stats;
            BulletPrefab = bulletPrefab;
            
            // Stats initialization
            CannonStats.InitRuntime();
            CurrentHp = MaxHp;
            
            // UI References
            var (bar, text) = references.GetCannonHealth();
            cannonHealthBar = bar;
            cannonHealthText = text;
            
            // Bounds
            var (left, right) = references.GetWall();
            leftWall = left;
            rightWall = right;

            UpdateUI();
            
            // Events
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

        private void OnLevelUp(int newLevel)
        {
            if (CannonStats != null)
            {
                CannonStats.ApplyProgression(newLevel);
                // Optionally heal on level up or just update max HP
                // CurrentHp = MaxHp; 
                UpdateUI();
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
            }
        }

        protected virtual void HandleMovement()
        {
            if (CannonStats == null) return;

            Vector2 oldPos = rb.position;
            Vector2 newPos = rb.position + movementInput * (CannonStats.currentMoveSpeed * Time.fixedDeltaTime);
            
            // Clamp within walls
            float leftBound = leftWall != null ? leftWall.bounds.max.x : -float.MaxValue;
            float rightBound = rightWall != null ? rightWall.bounds.min.x : float.MaxValue;
            
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
            GameObject go = Instantiate(BulletPrefab, position, rotation);
            
            // Handle New System
            if (go.TryGetComponent<BaseBullet>(out var newBullet))
            {
                newBullet.damage = CannonStats.currentBulletDamage;
                newBullet.bulletSpeed = CannonStats.currentBulletSpeed;
                
                // If it's a bouncing bullet, set bounce count
                if (newBullet is BouncingBullet bouncing)
                {
                    bouncing.bounceCount = CannonStats.bulletBounce;
                }
                
                // If it's an elemental bullet, we could set element here if it's not preset on prefab
                // Or pull from CannonStats if we add elemental stats there
            }
            // Fallback to legacy
            else if (go.TryGetComponent<Bullet>(out var bullet))
            {
                bullet.Damage = CannonStats.currentBulletDamage;
                bullet.bulletSpeed = CannonStats.currentBulletSpeed;
                bullet.bulletBounce = CannonStats.bulletBounce;
            }
        }

        public virtual void TakeDamage(int damage, Vector3 hitPoint)
        {
            if (!IsAlive) return;

            CurrentHp -= damage;
            CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
            
            UpdateUI();
            
            GameEvents.FireCannonHit(hitPoint, damage);

            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        protected virtual void UpdateUI()
        {
            if (cannonHealthBar != null)
            {
                float fill = (float)CurrentHp / MaxHp;
                cannonHealthBar.fillAmount = fill;
                
                // Color shift based on health
                if (fill > 0.6f) cannonHealthBar.color = Color.green;
                else if (fill > 0.3f) cannonHealthBar.color = Color.yellow;
                else cannonHealthBar.color = Color.red;
            }

            if (cannonHealthText != null)
            {
                cannonHealthText.text = $"{CurrentHp}/{MaxHp}";
            }
        }

        protected virtual void Die()
        {
            Debug.Log("Cannon Destroyed!");
            IsFiring = false;
            movementInput = Vector2.zero;
            
            GameEvents.FirePlayerDeath();
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            // Hazard detection is mostly handled by the hazard itself (e.g. EggDamage)
            // But we ensure it triggers feedback even if not handled elsewhere
            
            if (collision.CompareTag("Egg"))
            {
                // We don't take damage here because EggDamage script should be on the Egg
                // and it calls TakeDamage(damage, hitPoint) on us.
                // This method is just a fallback or for non-damageable trigger logic.
            }
        }

        #region IDamageEffect Implementation

        public virtual void ElectricDamage(float applyDamage, float effectTime)
        {
            // Apply damage over time or immediate electric effect
            TakeDamage(Mathf.RoundToInt(applyDamage), transform.position);
            // Additional electric effect logic (e.g. visual or slowdown) can be added here
        }

        public virtual void igniteDamage(float applyDamage, float effectTime)
        {
            // Apply fire/burn effect
            TakeDamage(Mathf.RoundToInt(applyDamage), transform.position);
            // Additional ignite effect logic can be added here
        }

        public virtual void PoisonDamage(float applyDamage, float effectTime)
        {
            // Apply poison damage
            TakeDamage(Mathf.RoundToInt(applyDamage), transform.position);
        }

        public virtual void FreezeEffect(float effectTime)
        {
            // Apply freeze effect (e.g. slow down movement)
            // We could reduce moveSpeed for effectTime
        }

        #endregion
    }
}