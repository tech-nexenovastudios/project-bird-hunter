using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Player
{
    public interface ICannonBase
    {
        string CannonName { get; }
        bool   IsAlive    { get; }
        bool   IsFiring   { get; }
        void   Configure(CannonStats stats);
        void   StartFiring();
        void   StopFiring();
        void   ReceivePartCollisionEnter(CannonPartCollider part, Collision2D col);
        void   ReceivePartCollisionExit (CannonPartCollider part, Collision2D col);
        void   ReceivePartTriggerEnter  (CannonPartCollider part, Collider2D other);
        void   ReceivePartTriggerExit   (CannonPartCollider part, Collider2D other);
    }

    public abstract class BaseCannon<TBullet> : MonoBehaviour, IDamageable, ICannonBase
        where TBullet : BaseBullet<TBullet>        // ✅ correct constraint
    {
        [Header("References")]
        [SerializeField] protected Transform[]      gunTips;
        [SerializeField] protected ParticleSystem[] fireParticles;
        [SerializeField] protected Transform[]      wheels;
        [SerializeField] protected float            wheelRadius = 0.5f;

        [Header("Collision Detection")]
        [SerializeField] private LayerMask detectableLayers   = ~0;
        [SerializeField] private bool      useTriggers        = true;
        [SerializeField] private bool      useColliders       = true;
        [SerializeField] private float     minimumImpactForce = 0f;

        [Header("Bounds Settings")]
        [SerializeField] private Camera mainCam;
        private float halfWidth;

        public string     CannonName  => gameObject.name;
        public int        CurrentHp   { get; private set; }
        public int        MaxHp       => CannonStats != null ? Mathf.RoundToInt(CannonStats.currentMaxHealth) : 100;
        public bool       IsAlive     => CurrentHp > 0;
        public bool       IsFiring    { get; private set; } = true;
        public CannonStats CannonStats { get; private set; }

        protected Rigidbody2D rb;
        private float         fireTimer;
        private Vector2       movementInput;
        private readonly HashSet<Collider2D> _activeOverlaps = new();

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType               = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public virtual void Configure(CannonStats stats)
        {
            mainCam = Camera.main;
            var col = GetComponent<Collider2D>();
            if (col != null) halfWidth = col.bounds.extents.x;
            CannonStats = stats;
            CannonStats.InitRuntime();
            CurrentHp = MaxHp;
            GameEvents.OnPlayerLevelUp += OnLevelUp;
        }

        private void OnDestroy() => GameEvents.OnPlayerLevelUp -= OnLevelUp;

        protected virtual void Update()      { HandleInput(); HandleFiring(); }
        protected virtual void FixedUpdate() => HandleMovement();

        protected virtual void HandleFiring()
        {
            if (CannonStats == null || !IsFiring) return;
            fireTimer += Time.deltaTime;
            if (fireTimer >= 1f / CannonStats.currentFireRate) { Shoot(); fireTimer = 0f; }
        }

        protected virtual void Shoot()
        {
            if (gunTips == null || gunTips.Length == 0)
                Fire(transform.position, transform.up);
            else
                foreach (var tip in gunTips) if (tip != null) Fire(tip.position, tip.up);
            PlayFireParticles();
        }

        protected abstract void Fire(Vector2 position, Vector2 direction);

        protected void PlayFireParticles()
        {
            if (fireParticles == null) return;
            foreach (var p in fireParticles) if (p != null) p.Play();
        }

        // ── SpawnBullet helpers ───────────────────────────────────────────
        protected TBullet SpawnBullet(Vector2 position, Vector2 direction)
            => BulletManager.Instance.SpawnBullet<TBullet>(CannonStats.baseBulletConfig, position, direction);

        protected TOther SpawnBullet<TOther>(BulletConfig config, Vector2 position, Vector2 direction)
            where TOther : BaseBullet<TOther>      // ✅ FIXED — was missing generic param
            => BulletManager.Instance.SpawnBullet<TOther>(config, position, direction);

        protected virtual void HandleInput()
        {
            if (Managers.GameManager.Instance != null &&
                Managers.GameManager.Instance.state != GameState.Gameplay)
            { movementInput = Vector2.zero; return; }

            if (Input.GetMouseButton(0))
            {
                StartFiring();
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float   dir      = mousePos.x > transform.position.x ? 1f : -1f;
                movementInput    = Mathf.Abs(mousePos.x - transform.position.x) < 0.1f
                    ? Vector2.zero : new Vector2(dir, 0);
            }
            else { movementInput = Vector2.zero; StopFiring(); }
        }

        protected virtual void HandleMovement()
        {
            if (CannonStats == null) return;
            Vector2 oldPos     = rb.position;
            Vector2 newPos     = rb.position + movementInput * (CannonStats.currentMoveSpeed * Time.fixedDeltaTime);
            float   leftBound  = mainCam.ViewportToWorldPoint(Vector3.zero).x  + halfWidth;
            float   rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;
            newPos.x           = Mathf.Clamp(newPos.x, leftBound, rightBound);
            rb.MovePosition(newPos);

            if (wheels != null)
            {
                float dist = newPos.x - oldPos.x;
                float rot  = -(dist / (2f * Mathf.PI * wheelRadius)) * 360f;
                foreach (var w in wheels) if (w != null) w.Rotate(0, 0, rot);
            }
        }

        public void StartFiring() => IsFiring = true;
        public void StopFiring()  => IsFiring = false;

        public void ReceivePartCollisionEnter(CannonPartCollider part, Collision2D col)
        {
            if (!useColliders || !IsLayerDetectable(col.gameObject.layer)) return;
            if (col.relativeVelocity.magnitude < minimumImpactForce) return;
            HandleCollisionEnter(BuildData(col.gameObject, col.contacts[0].point,
                col.contacts[0].normal, col.relativeVelocity.magnitude, CollisionEventType.CollisionEnter, part));
        }

        public void ReceivePartCollisionExit(CannonPartCollider part, Collision2D col)
        {
            if (!useColliders || !IsLayerDetectable(col.gameObject.layer)) return;
            HandleCollisionExit(BuildData(col.gameObject, transform.position,
                Vector2.zero, 0f, CollisionEventType.CollisionExit, part));
        }

        public void ReceivePartTriggerEnter(CannonPartCollider part, Collider2D other)
        {
            if (!useTriggers || !IsLayerDetectable(other.gameObject.layer)) return;
            if (_activeOverlaps.Contains(other)) return;
            _activeOverlaps.Add(other);
            HandleTriggerEnter(BuildData(other.gameObject, other.ClosestPoint(transform.position),
                Vector2.zero, 0f, CollisionEventType.TriggerEnter, part));
        }

        public void ReceivePartTriggerExit(CannonPartCollider part, Collider2D other)
        {
            _activeOverlaps.Remove(other);
            if (!useTriggers) return;
            HandleTriggerExit(BuildData(other.gameObject, transform.position,
                Vector2.zero, 0f, CollisionEventType.TriggerExit, part));
        }

        protected virtual void HandleCollisionEnter(CannonCollisionData data) { }
        protected virtual void HandleCollisionExit (CannonCollisionData data) { }
        protected virtual void HandleTriggerEnter  (CannonCollisionData data) { }
        protected virtual void HandleTriggerExit   (CannonCollisionData data) { }

        public virtual void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Clamp(CurrentHp - damage, 0, MaxHp);
            GameEvents.FireCannonHit(damage);
            if (CurrentHp <= 0) Die();
        }

        public void TakeDamage(int damage, Vector3 hitPoint) => TakeDamage(damage);

        protected virtual void Die()
        {
            IsFiring      = false;
            movementInput = Vector2.zero;
            GameEvents.FirePlayerDeath();
        }

        private void OnLevelUp(int newLevel) => CannonStats?.ApplyProgression(newLevel);

        public virtual void ElectricDamage(float dmg, float t) => TakeDamage(Mathf.RoundToInt(dmg));
        public virtual void igniteDamage  (float dmg, float t) => TakeDamage(Mathf.RoundToInt(dmg));
        public virtual void PoisonDamage  (float dmg, float t) => TakeDamage(Mathf.RoundToInt(dmg));
        public virtual void FreezeEffect  (float t) { }

        private bool IsLayerDetectable(int layer) => (detectableLayers.value & (1 << layer)) != 0;

        private CannonCollisionData BuildData(GameObject other, Vector2 point, Vector2 normal,
            float force, CollisionEventType type, CannonPartCollider hitPart) => new()
        {
            sourceCannon  = this, hitPart = hitPart, otherObject = other,
            contactPoint  = point, contactNormal = normal,
            impactForce   = force, eventType = type, timestamp = Time.time
        };

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
