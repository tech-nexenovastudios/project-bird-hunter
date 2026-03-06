using System;
using UnityEngine;
using Gameplay.Health;

namespace Gameplay.Eggs
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class Egg : MonoBehaviour
    {
        public EggTierConfig config;
        public int currentHp;

        public Action<Egg> OnDestroyed;

        [Header("Physics")]
        [SerializeField] private float jumpImpulse = 8f;       // Controls bounce height
        [SerializeField] private float wallKickImpulse = 2f;   // Extra sideways push on wall hit
        
        [Header("Center of Mass")]
        [SerializeField] private Vector2 centerOfMassOffset = new Vector2(0f, -0.15f);
        [SerializeField] private bool freezeUpright = false;
        
        [Header("Bounds Settings")]
        [SerializeField] private Camera mainCam;
        [SerializeField] private Collider2D leftWall;
        [SerializeField] private Collider2D rightWall;

        [Header("Legacy Bounds (Fallback)")]
        [SerializeField] private float minX = -4f;
        [SerializeField] private float maxX = 4f;

        [Header("VFX")]
        [SerializeField] private GameObject smokeParticle;

        // Internals
        private PolygonCollider2D _collider;
        private Rigidbody2D _rb;
        private float _dynamicMinX;
        private float _dynamicMaxX;
        private bool _useScreenBounds;
        private bool _wallBounceCooldown = false;
        private float _wallBounceCooldownTime = 0.1f;
        
        // ─────────────────────────────────────────────
        // Init
        // ─────────────────────────────────────────────

        public void Init(EggTierConfig tierConfig, int hp)
        {
            config = tierConfig;
            currentHp = hp;

            var eggHealth = GetComponent<Gameplay.Health.EggHealth>();
            if (eggHealth != null)
                eggHealth.Init(tierConfig, hp);

            // Override jump impulse from tier config if provided
            if (config != null && config.jumpImpulse > 0f)
                jumpImpulse = config.jumpImpulse;

            _collider = GetComponent<PolygonCollider2D>();
            _rb = GetComponent<Rigidbody2D>();

            if (mainCam == null) mainCam = Camera.main;

            CalculateBounds();
            ApplyCenterOfMass();
        }

        // ─────────────────────────────────────────────
        // Bounds
        // ─────────────────────────────────────────────

        private void CalculateBounds()
        {
            _useScreenBounds = !AreWallsVisible();

            float eggRadius = _collider.bounds.extents.x; // world-space half-width

            if (_useScreenBounds)
            {
                Vector3 leftEdge  = mainCam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, 0));
                Vector3 rightEdge = mainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, 0));
                _dynamicMinX = leftEdge.x  + eggRadius;
                _dynamicMaxX = rightEdge.x - eggRadius;
            }
            else
            {
                _dynamicMinX = leftWall.bounds.max.x  + eggRadius;
                _dynamicMaxX = rightWall.bounds.min.x - eggRadius;
            }

            // Fallback
            if (_dynamicMinX >= _dynamicMaxX)
            {
                _dynamicMinX = minX;
                _dynamicMaxX = maxX;
                Debug.LogWarning("[Egg] Bounds calculation failed — using fallback values.");
            }
        }
        /// <summary>
        /// Shifts center of mass downward in local space to simulate
        /// a yolk-heavy egg that self-rights after bouncing.
        /// centerOfMass is in LOCAL space and ignores transform.scale. [Rigidbody2D docs]
        /// </summary>
        private void ApplyCenterOfMass()
        {
            _rb.centerOfMass = centerOfMassOffset;

            if (freezeUpright)
            {
                // Completely prevents rotation — egg stays perfectly vertical always
                _rb.freezeRotation = true;
            }
            else
            {
                _rb.freezeRotation = false;
                // Allow rotation but dampen it so egg wobbles and self-rights naturally
                _rb.angularDamping = 5f; // higher = snaps upright faster, lower = more wobble
            }
        }

        private bool AreWallsVisible()
        {
            if (leftWall == null || rightWall == null || mainCam == null) return false;

            float halfW    = mainCam.orthographicSize * mainCam.aspect;
            float camLeft  = mainCam.transform.position.x - halfW;
            float camRight = mainCam.transform.position.x + halfW;

            bool leftVisible  = leftWall.bounds.max.x  >= camLeft && leftWall.bounds.min.x  <= camRight;
            bool rightVisible = rightWall.bounds.max.x >= camLeft && rightWall.bounds.min.x <= camRight;
            return leftVisible && rightVisible;
        }

        public void RecalculateBounds() => CalculateBounds();

        // ─────────────────────────────────────────────
        // FixedUpdate — wall reflection + slow-mo
        // ─────────────────────────────────────────────

        private void FixedUpdate()
        {
            HandleWallBounds();
            HandleCannonPowerGravity();
        }

        private void HandleWallBounds()
        {
            Vector2 pos = _rb.position;
            if (pos.x < _dynamicMinX || pos.x > _dynamicMaxX)
            {
                pos.x = Mathf.Clamp(pos.x, _dynamicMinX, _dynamicMaxX);
                _rb.position = pos;

                // Reflect horizontal velocity
                Vector2 v = _rb.linearVelocity;
                v.x = -v.x;
                _rb.linearVelocity = v;
            }
        }

        private void HandleCannonPowerGravity()
        {
            if (CannonPower.instance == null) return;

            if (CannonPower.instance.reduceGravity)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.gravityScale = _rb.linearVelocity.y > 0 ? 0.005f : 0.5f;
            }
            else
            {
                _rb.gravityScale = 0.5f;
            }
        }

        // ─────────────────────────────────────────────
        // Collision — bounce off ground, walls, forcefields
        // ─────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            switch (collision.transform.tag)
            {
                case "Ground":
                    BounceOffGround(collision);
                    break;

                case "Wall":
                    BounceOffWall(collision);
                    break;

                case "ForceField":
                    BounceOffForceField(collision);
                    break;
            }
        }

        /// <summary>
        /// Kills downward velocity, then applies a fixed upward impulse to maintain
        /// consistent bounce height regardless of incoming speed.
        /// jumpImpulse = sqrt(2 * g * h) → h = jumpImpulse² / (2g)
        /// </summary>
        private void BounceOffGround(Collision2D collision)
        {
            SpawnSmoke(collision.contacts[0].point);

            // Zero out Y so accumulated gravity doesn't stack with our impulse
            Vector2 v = _rb.linearVelocity;
            v.y = 0f;
            _rb.linearVelocity = v;

            // Preserve horizontal momentum, clamp sideways kick
            float sideKick = Mathf.Clamp(v.x, -wallKickImpulse, wallKickImpulse);
            _rb.AddForce(Vector2.up * jumpImpulse + Vector2.right * sideKick,
                         ForceMode2D.Impulse);
        }

        /// <summary>
        /// Directly reflects horizontal velocity off the wall normal.
        /// Uses a cooldown to prevent impulse stacking from multiple contacts
        /// in the same physics frame.
        /// </summary>
        private void BounceOffWall(Collision2D collision)
        {
            if (_wallBounceCooldown) return;

            // Average all contact normals to get a clean wall normal
            Vector2 normal = Vector2.zero;
            foreach (var contact in collision.contacts)
                normal += contact.normal;
            normal.Normalize();

            Vector2 v = _rb.linearVelocity;

            // Only reflect if egg is actually moving INTO the wall
            // (dot < 0 means velocity opposes the normal = moving toward wall)
            if (Vector2.Dot(v, normal) >= 0) return;

            // Reflect only the horizontal component off the wall normal
            // Keep vertical velocity (gravity / jump arc) completely untouched
            v.x = Mathf.Abs(v.x) * Mathf.Sign(normal.x);
            _rb.linearVelocity = v;

            // Snap egg just inside the bound it violated to clear the collider
            Vector2 pos = _rb.position;
            pos.x = Mathf.Clamp(pos.x, _dynamicMinX, _dynamicMaxX);
            _rb.position = pos;

            StartCoroutine(WallBounceCooldown());
        }

        private System.Collections.IEnumerator WallBounceCooldown()
        {
            _wallBounceCooldown = true;
            yield return new WaitForSeconds(_wallBounceCooldownTime);
            _wallBounceCooldown = false;
        }


        /// <summary>
        /// Half-height bounce — same pattern as ground but reduced impulse.
        /// </summary>
        private void BounceOffForceField(Collision2D collision)
        {
            Vector2 v = _rb.linearVelocity;
            v.y = 0f;
            _rb.linearVelocity = v;

            float sideKick = Mathf.Clamp(v.x, -wallKickImpulse, wallKickImpulse);
            _rb.AddForce(Vector2.up * (jumpImpulse * 0.5f) + Vector2.right * sideKick,
                         ForceMode2D.Impulse);
        }

        // ─────────────────────────────────────────────
        // Damage
        // ─────────────────────────────────────────────

        public void Hit(int damage)
        {
            currentHp -= damage;
            if (currentHp <= 0)
                OnDestroyed?.Invoke(this);
        }

        // ─────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────

        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            Destroy(Instantiate(smokeParticle, position, Quaternion.identity), 3f);
        }

        public Vector2[] GetWorldPoints()
        {
            Vector2[] local = _collider.GetPath(0);
            Vector2[] world = new Vector2[local.Length];
            for (int i = 0; i < local.Length; i++)
                world[i] = (Vector2)transform.TransformPoint(local[i]);
            return world;
        }

        private void OnDrawGizmos()
        {
            if (_collider == null) return;
            Vector2[] pts = GetWorldPoints();
            Gizmos.color = Color.green;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[(i + 1) % pts.Length];
                Gizmos.DrawLine(a, b);
                Vector2 tangent = (b - a).normalized;
                Vector2 normal  = new Vector2(tangent.y, -tangent.x); // clockwise winding
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay((a + b) * 0.5f, normal * 0.15f);
                Gizmos.color = Color.green;
            }
        }
    }
}
