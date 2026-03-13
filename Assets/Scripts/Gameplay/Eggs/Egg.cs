using System;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Eggs
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Egg : MonoBehaviour
    {
        public EggTierConfig config;

        public Action<Egg> OnDestroyed;

        [Header("Bounds Settings")]
        [SerializeField] private Camera mainCam;
        private float halfWidth;

        [Header("VFX")]
        [SerializeField] private GameObject smokeParticle;

        private Rigidbody2D _rb;

        // ─────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            if (mainCam == null)
                mainCam = Camera.main;

            var col = GetComponent<Collider2D>();
            halfWidth = col.bounds.extents.x;
        }

        public void Init(EggTierConfig tierConfig, int hp)
        {
            config = tierConfig;

            var eggHealth = GetComponent<Health.EggHealth>();
            eggHealth.Init(tierConfig, hp);

            if (mainCam == null) mainCam = Camera.main;

            ApplyPhysicsSettings();
        }

        private void ApplyPhysicsSettings()
        {
            _rb.gravityScale    = config.gravityScale;
            _rb.mass            = config.mass;
            _rb.linearDamping   = config.linearDamping;
        }

        // ─────────────────────────────────────────────
        //  Physics Loop
        // ─────────────────────────────────────────────

        private void FixedUpdate()
        {
            _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, config.maxSpeed);
            HandleScreenEdges();
        }

        private void HandleScreenEdges()
        {
            Vector3 pos = transform.position;

            float leftBound  = mainCam.ViewportToWorldPoint(Vector3.zero).x  + halfWidth;
            float rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;

            if (pos.x < leftBound)
            {
                pos.x = leftBound;
                _rb.linearVelocity = new Vector2(Mathf.Abs(_rb.linearVelocity.x), _rb.linearVelocity.y);
            }
            else if (pos.x > rightBound)
            {
                pos.x = rightBound;
                _rb.linearVelocity = new Vector2(-Mathf.Abs(_rb.linearVelocity.x), _rb.linearVelocity.y);
            }

            transform.position = pos;
        }

        // ─────────────────────────────────────────────
        //  Collision
        // ─────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                if (collision.contactCount > 0)
                    SpawnSmoke(collision.contacts[0].point);

                Vector2 velocity = _rb.linearVelocity;
                velocity.y = config.bounceVelocity;
                _rb.linearVelocity = velocity;
                return;
            }

            HandleCollision(collision);
        }

        /// <summary>
        /// Handles non-ground collisions. Checks if the hit object is a
        /// CannonPartCollider, resolves IDamageable from its parent, and
        /// applies damage scaled by the part's damageMultiplier.
        /// </summary>
        private void HandleCollision(Collision2D collision)
        {
            var damageable = GetDamageable(collision.collider);
            if (damageable == null || !damageable.IsAlive) return;

            // Read damage from this egg's own SO config
            var rawDamage = config.cannonDamage;

            // Scale by the specific part that was hit (barrel, body, wheel, etc.)
            if (collision.collider.TryGetComponent<CannonPartCollider>(out var part))
                rawDamage = Mathf.RoundToInt(rawDamage * part.damageMultiplier);

            damageable.TakeDamage(rawDamage);
        }

        /// <summary>
        /// Resolves IDamageable from the collided object.
        /// Checks the object itself first, then walks up the parent hierarchy —
        /// this covers both a root cannon collider and child CannonPartColliders.
        /// </summary>
        private IDamageable GetDamageable(Collider2D other)
        {
            return other.GetComponentInParent<IDamageable>();
        }

        // ─────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────

        public void ApplyBulletHitForce(Vector2 hitDirection, float hitForce)
        {
            if (hitDirection.sqrMagnitude <= 0.0001f) return;
            _rb.AddForce(hitDirection.normalized * hitForce, ForceMode2D.Impulse);
        }

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            Destroy(Instantiate(smokeParticle, position, Quaternion.identity), 3f);
        }
    }
}