using System;
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
        private float _dynamicMinX;
        private float _dynamicMaxX;
        
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
            _rb.gravityScale = config.gravityScale;
            _rb.mass = config.mass;
            _rb.linearDamping = config.linearDamping;
        }
        private void FixedUpdate()
        {
            _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, config.maxSpeed);
            HandleScreenEdges();
        }

        void HandleScreenEdges()
        {
            Vector3 pos = transform.position;

            // Get camera bounds in world units
            float leftBound = mainCam.ViewportToWorldPoint(Vector3.zero).x + halfWidth;
            float rightBound = mainCam.ViewportToWorldPoint(Vector3.right).x - halfWidth;

            // Bounce off left/right edges
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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                if (collision.contactCount > 0)
                    SpawnSmoke(collision.contacts[0].point);

                Vector2 velocity = _rb.linearVelocity;
                velocity.y = config.bounceVelocity;
                _rb.linearVelocity = velocity;
            }
        }

        public void ApplyBulletHitForce(Vector2 hitDirection, float hitForce)
        {
            if (hitDirection.sqrMagnitude <= 0.0001f)
                return;

            _rb.AddForce(hitDirection.normalized * hitForce, ForceMode2D.Impulse);
        }
        
        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            Destroy(Instantiate(smokeParticle, position, Quaternion.identity), 3f);
        }
    }
}
