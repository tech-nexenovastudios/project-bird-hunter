using System;
using System.Collections;
using UnityEngine;
using Gameplay.Health;

namespace Gameplay.Eggs
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Egg : MonoBehaviour
    {
        public EggTierConfig config;
        public int currentHp;

        public Action<Egg> OnDestroyed;

        [Header("Physics")]
        [SerializeField] private float targetBounceHeight = 5f;
        [SerializeField] private float horizontalSpeedMin = 1.5f;
        [SerializeField] private float horizontalSpeedMax = 3f;

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
        private CircleCollider2D _collider;
        private Rigidbody2D      _rb;
        private float            _dynamicMinX;
        private float            _dynamicMaxX;
        private bool             _wallBounceCooldown;
        private float            _wallBounceCooldownTime = 0.1f;
        private float            _jumpImpulse;
        private float            _horizontalSpeed;   // single source of truth for X direction
        private bool             _initialized;
        private bool             _hitFreezeActive;

        // ─────────────────────────────────────────────
        // Start — fallback if SpawnController never called Init
        // ─────────────────────────────────────────────
        private void Start()
        {
            if (!_initialized)
            {
                int fallbackHp = config != null
                    ? UnityEngine.Random.Range(config.baseHpMin, config.baseHpMax + 1)
                    : 100;
                Init(config, fallbackHp);
            }
        }

        // ─────────────────────────────────────────────
        // Init
        // ─────────────────────────────────────────────
        public void Init(EggTierConfig tierConfig, int hp)
        {
            _initialized = true;
            config       = tierConfig;
            currentHp    = hp;

            _collider = GetComponent<CircleCollider2D>();
            _rb       = GetComponent<Rigidbody2D>();

            var eggHealth = GetComponent<Health.EggHealth>();
            if (eggHealth != null)
            {
                eggHealth.Init(tierConfig, hp);
                eggHealth.OnHpChanged += (oldHp, newHp) => currentHp = Mathf.Max(0, newHp);
            }

            targetBounceHeight = UnityEngine.Random.Range(5f, 6.5f);
            if (mainCam == null) mainCam = Camera.main;

            CalculateBounds();
            ApplyCenterOfMass();
            RecalculateJumpImpulse();

            // Set horizontal speed — random direction, consistent magnitude
            float dir        = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            _horizontalSpeed = UnityEngine.Random.Range(horizontalSpeedMin, horizontalSpeedMax) * dir;
            _rb.linearVelocity = new Vector2(_horizontalSpeed, 0f);
        }

        // ─────────────────────────────────────────────
        // Jump Impulse — sqrt(2 * g * h)
        // ─────────────────────────────────────────────
        private void RecalculateJumpImpulse()
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * _rb.gravityScale;
            _jumpImpulse  = gravity > 0f
                ? Mathf.Sqrt(2f * gravity * targetBounceHeight)
                : 3f;
        }

        // ─────────────────────────────────────────────
        // Bounds
        // ─────────────────────────────────────────────
        private void CalculateBounds()
        {
            float eggRadius = _collider.bounds.extents.x;

            if (leftWall != null && rightWall != null)
            {
                _dynamicMinX = leftWall.bounds.max.x  + eggRadius;
                _dynamicMaxX = rightWall.bounds.min.x - eggRadius;
            }
            else if (mainCam != null)
            {
                Vector3 left  = mainCam.ScreenToWorldPoint(new Vector3(0,            Screen.height / 2f, 0));
                Vector3 right = mainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, 0));
                _dynamicMinX  = left.x  + eggRadius;
                _dynamicMaxX  = right.x - eggRadius;
            }
            else
            {
                _dynamicMinX = minX;
                _dynamicMaxX = maxX;
            }

            if (_dynamicMinX >= _dynamicMaxX)
            {
                _dynamicMinX = minX;
                _dynamicMaxX = maxX;
                Debug.LogWarning("[Egg] Bounds calculation failed — using fallback values.");
            }
        }

        private void ApplyCenterOfMass()
        {
            _rb.centerOfMass   = centerOfMassOffset;
            _rb.freezeRotation = freezeUpright;
            if (!freezeUpright) _rb.angularDamping = 5f;
        }

        public void RecalculateBounds() => CalculateBounds();

        // ─────────────────────────────────────────────
        // FixedUpdate — enforce horizontal speed every frame
        // This is the key: nothing can corrupt _horizontalSpeed mid-flight
        // ─────────────────────────────────────────────
        private void FixedUpdate()
        {
            if (_hitFreezeActive) return;

            // Clamp position within bounds
            Vector2 pos = _rb.position;
            if (pos.x < _dynamicMinX)
            {
                pos.x            = _dynamicMinX;
                _rb.position     = pos;
                _horizontalSpeed = Mathf.Abs(_horizontalSpeed); // force rightward
            }
            else if (pos.x > _dynamicMaxX)
            {
                pos.x            = _dynamicMaxX;
                _rb.position     = pos;
                _horizontalSpeed = -Mathf.Abs(_horizontalSpeed); // force leftward
            }

            // Always enforce _horizontalSpeed as x velocity — gravity only affects y
            _rb.linearVelocity = new Vector2(_horizontalSpeed, _rb.linearVelocity.y);
        }

        // ─────────────────────────────────────────────
        // Collision
        // ─────────────────────────────────────────────
        private void OnCollisionEnter2D(Collision2D collision)
        {
            switch (collision.transform.tag)
            {
                case "Ground":
                    SpawnSmoke(collision.contacts[0].point);
                    BounceOffGround();
                    break;

                case "Wall":
                    if (_wallBounceCooldown) return;
                    BounceOffWall();
                    StartCoroutine(WallBounceCooldown());
                    break;
            }
        }

        // Shoot straight up, keep current horizontal direction
        private void BounceOffGround()
        {
            if (_hitFreezeActive) return;
            _rb.linearVelocity = new Vector2(_horizontalSpeed, _jumpImpulse);
        }

        // Flip horizontal direction only
        private void BounceOffWall()
        {
            if (_hitFreezeActive) return;
            _horizontalSpeed = -_horizontalSpeed;
            // FixedUpdate will sync velocity.x next frame
        }

        private IEnumerator WallBounceCooldown()
        {
            _wallBounceCooldown = true;
            yield return new WaitForSeconds(_wallBounceCooldownTime);
            _wallBounceCooldown = false;
        }

        // ─────────────────────────────────────────────
        // Hit Freeze
        // ─────────────────────────────────────────────
        public void ApplyHitFreeze(float freezeDuration, float horizontalImpulse, Vector2 impulseDir)
        {
            if (_hitFreezeActive) StopCoroutine(nameof(HitFreezeRoutine));
            StartCoroutine(HitFreezeRoutine(freezeDuration, horizontalImpulse, impulseDir));
        }

        private IEnumerator HitFreezeRoutine(float duration, float horizontalImpulse, Vector2 impulseDir)
        {
            _hitFreezeActive = true;

            float originalGravity  = _rb.gravityScale;
            _rb.linearVelocity     = Vector2.zero;
            _rb.gravityScale       = 0f;

            // Small horizontal nudge in bullet direction
            if (Mathf.Abs(impulseDir.x) > 0.01f)
                _rb.AddForce(new Vector2(impulseDir.x, 0f).normalized * horizontalImpulse, ForceMode2D.Impulse);

            // Hold freeze — suppress vertical drift
            float elapsed = 0f;
            while (elapsed < duration)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore gravity smoothly
            float restoreTime = 0.15f;
            float t           = 0f;
            while (t < restoreTime)
            {
                _rb.gravityScale = Mathf.Lerp(0f, originalGravity, t / restoreTime);
                t               += Time.deltaTime;
                yield return null;
            }
            _rb.gravityScale = originalGravity;

            // Resume with original horizontal path
            _rb.linearVelocity = new Vector2(_horizontalSpeed, 0f);

            _hitFreezeActive = false;
        }

        // ─────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────
        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            Destroy(Instantiate(smokeParticle, position, Quaternion.identity), 3f);
        }
    }
}
