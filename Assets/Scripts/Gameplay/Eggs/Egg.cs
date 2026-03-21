using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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

        [Header("VFX Prefabs (from Project folder)")]
        [Tooltip("Prefab for small hit burst (sparks, dust, shell bits)")]
        [SerializeField] private GameObject minorHitVFXPrefab;

        [Tooltip("Prefab for death explosion")]
        [SerializeField] private GameObject blastVFXPrefab;

        [Header("VFX Spawn Points (empty child Transforms)")]
        [Tooltip("Where the minor hit effect plays")]
        [SerializeField] private Transform hitVFXPoint;

        [Tooltip("Where the blast explosion plays")]
        [SerializeField] private Transform blastVFXPoint;

        [Header("Other VFX")]
        [SerializeField] private GameObject smokeParticle;

        [Header("Hit Reaction")]
        [Tooltip("Scale multiplier on each hit (1.1 = 110%)")]
        [SerializeField] private float hitScaleMultiplier = 1.1f;

        [Tooltip("How far the egg gets pushed upward on hit")]
        [SerializeField] private float hitPushUpForce = 2f;

        [Tooltip("Time to scale up on hit")]
        [SerializeField] private float hitScaleUpDuration = 0.08f;

        [Tooltip("Time to return to original scale after hit")]
        [SerializeField] private float hitScaleDownDuration = 0.12f;

        [Header("Death Sequence")]
        [Tooltip("Final scale before destruction (0.1 = 10% of original)")]
        [SerializeField] private float deathScaleTarget = 0.1f;

        [Tooltip("Time to shrink to death scale")]
        [SerializeField] private float deathScaleDuration = 0.25f;

        // ── Cached Components ──
        private Rigidbody2D    _rb;
        private SpriteRenderer _spriteRenderer;
        private SortingGroup   _sortingGroup;
        private Material       _mat;
        private Collider2D     _collider;

        // ── Cached Visuals (for hide/show) ──
        private Renderer[] _allRenderers;
        private Canvas[]   _allCanvases;

        // ── Cached VFX Instance ──
        // Minor hit is instantiated ONCE on first hit, then reused.
        // This avoids Instantiate/Destroy every hit on a rapid-fire cannon.
        private ParticleSystem _minorHitVFXInstance;

        // ── State ──
        private Vector3   _originalScale;
        private bool      _isDying;
        private Coroutine _hitScaleCoroutine;

        // ── Shader Property IDs ──
        private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");

        // ════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════════════════════

        private void Awake()
        {
            _rb             = GetComponent<Rigidbody2D>();
            _sortingGroup   = GetComponent<SortingGroup>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider       = GetComponent<Collider2D>();

            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _allCanvases  = GetComponentsInChildren<Canvas>(true);

            if (_spriteRenderer != null)
                _mat = _spriteRenderer.material;

            if (mainCam == null)
                mainCam = Camera.main;

            halfWidth = _collider != null ? _collider.bounds.extents.x : 0.5f;
            _originalScale = transform.localScale;
        }

        public void Init(EggTierConfig tierConfig, int hp, int sortingIndex)
        {
            config = tierConfig;
            _sortingGroup.sortingOrder = sortingIndex;
            _isDying = false;

            transform.localScale = _originalScale;

            if (_mat != null)
                _mat.SetFloat(EdgeWidthID, 0f);

            SetVisualsActive(true);

            if (_collider != null)
                _collider.enabled = true;

            _rb.bodyType = RigidbodyType2D.Dynamic;

            var eggHealth = GetComponent<Health.EggHealth>();
            eggHealth.Init(tierConfig, hp);

            if (mainCam == null)
                mainCam = Camera.main;

            ApplyPhysicsSettings();
        }

        private void ApplyPhysicsSettings()
        {
            _rb.gravityScale  = config.gravityScale;
            _rb.mass          = config.mass;
            _rb.linearDamping = config.linearDamping;
        }

        // ════════════════════════════════════════════════════════
        //  PHYSICS & MOVEMENT
        // ════════════════════════════════════════════════════════

        private void FixedUpdate()
        {
            if (_isDying) return;

            _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, config.maxSpeed);
            HandleScreenEdges();
        }

        private void HandleScreenEdges()
        {
            Vector3 pos = transform.position;

            float leftBound  = mainCam.ViewportToWorldPoint(Vector3.zero).x + halfWidth;
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

        // ════════════════════════════════════════════════════════
        //  COLLISIONS
        // ════════════════════════════════════════════════════════

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDying) return;

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
            if (_isDying) return;
            if (hitDirection.sqrMagnitude <= 0.0001f) return;

            _rb.AddForce(hitDirection.normalized * hitForce, ForceMode2D.Impulse);
        }

        // ════════════════════════════════════════════════════════
        //  HIT REACTION
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Called by EggHealth.TakeDamage() when the egg survives a hit.
        /// Plays minor hit VFX, scales up temporarily, pushes upward,
        /// and updates edge cracks based on remaining health.
        /// </summary>
        public void PlayHitReaction(float healthPercent)
        {
            if (_isDying) return;

            // 1. Update crack shader
            UpdateEdgeWidth(healthPercent);

            // 2. Play minor hit VFX at hitVFXPoint
            PlayMinorHitVFX();

            // 3. Push upward
            _rb.AddForce(Vector2.up * hitPushUpForce, ForceMode2D.Impulse);

            // 4. Scale punch
            if (_hitScaleCoroutine != null)
            {
                StopCoroutine(_hitScaleCoroutine);
                transform.localScale = _originalScale;
            }

            _hitScaleCoroutine = StartCoroutine(HitScaleRoutine());
        }

        /// <summary>
        /// Instantiates the minor hit VFX prefab on first hit, then reuses
        /// the same instance on subsequent hits. Repositions to hitVFXPoint
        /// each time so it always plays at the correct spot.
        /// </summary>
        private void PlayMinorHitVFX()
        {
            if (minorHitVFXPrefab == null || hitVFXPoint == null) return;

            // Lazy instantiate: create once, reuse forever
            if (_minorHitVFXInstance == null)
            {
                GameObject go = Instantiate(minorHitVFXPrefab, hitVFXPoint.position, Quaternion.identity);

                // NOT parented to egg — lives in world space
                // so particles don't move with the egg after spawning
                _minorHitVFXInstance = go.GetComponent<ParticleSystem>();

                if (_minorHitVFXInstance == null)
                {
                    // Prefab didn't have a ParticleSystem — clean up
                    Destroy(go);
                    return;
                }

                // Ensure it doesn't auto-play or loop
                var main = _minorHitVFXInstance.main;
                // (These should already be set on the prefab, but safety first)
            }

            // Reposition to current hit point and replay
            _minorHitVFXInstance.transform.position = hitVFXPoint.position;
            _minorHitVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _minorHitVFXInstance.Play(true);
        }

        private IEnumerator HitScaleRoutine()
        {
            Vector3 targetScale = _originalScale * hitScaleMultiplier;

            yield return AnimateScale(_originalScale, targetScale, hitScaleUpDuration, EaseType.OutBack);
            yield return AnimateScale(targetScale, _originalScale, hitScaleDownDuration, EaseType.InBack);

            _hitScaleCoroutine = null;
        }

        private void UpdateEdgeWidth(float healthPercent)
        {
            if (_mat == null) return;
            float edgeWidth = 1f - Mathf.Clamp01(healthPercent);
            _mat.SetFloat(EdgeWidthID, edgeWidth);
        }

        // ════════════════════════════════════════════════════════
        //  DEATH SEQUENCE
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Called by EggHealth.Die() after HP reaches 0.
        /// Scale down → blast → destroy.
        /// </summary>
        public void PlayDeathSequence()
        {
            if (_isDying) return;
            _isDying = true;

            // Kill hit reaction if running
            if (_hitScaleCoroutine != null)
            {
                StopCoroutine(_hitScaleCoroutine);
                _hitScaleCoroutine = null;
            }

            // Destroy the cached minor hit VFX instance — no longer needed
            if (_minorHitVFXInstance != null)
            {
                _minorHitVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(_minorHitVFXInstance.gameObject);
                _minorHitVFXInstance = null;
            }

            // Max out cracks
            if (_mat != null)
                _mat.SetFloat(EdgeWidthID, 1f);

            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            // ── Phase 0: Freeze physics ──
            _rb.linearVelocity  = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.bodyType        = RigidbodyType2D.Kinematic;

            if (_collider != null)
                _collider.enabled = false;

            // ── Phase 1: Scale DOWN ──
            Vector3 deathScale = _originalScale * deathScaleTarget;
            yield return AnimateScale(transform.localScale, deathScale, deathScaleDuration, EaseType.InBack);

            // ── Phase 2: Hide visuals + spawn blast ──
            SetVisualsActive(false);

            if (blastVFXPrefab != null)
            {
                // Spawn position from the empty child transform
                Vector3 blastPos = blastVFXPoint != null
                    ? blastVFXPoint.position
                    : transform.position;

                // Instantiate in world space — NOT parented to egg
                // so it survives after egg is destroyed/pooled
                GameObject blastGO = Instantiate(blastVFXPrefab, blastPos, Quaternion.identity);

                var blastPS = blastGO.GetComponent<ParticleSystem>();
                if (blastPS != null)
                {
                    blastPS.Play(true);

                    // Calculate total visible time then auto-destroy
                    var main = blastPS.main;
                    float blastTotalTime = main.duration + main.startLifetime.constantMax;
                    Destroy(blastGO, blastTotalTime);

                    yield return new WaitForSeconds(blastTotalTime);
                }
                else
                {
                    // Prefab had no ParticleSystem — destroy immediately
                    Destroy(blastGO);
                }
            }

            // ── Phase 3: Notify SpawnController ──
            OnDestroyed?.Invoke(this);
        }

        // ════════════════════════════════════════════════════════
        //  ANIMATION HELPERS
        // ════════════════════════════════════════════════════════

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, EaseType ease)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = ApplyEase(t, ease);

                transform.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            transform.localScale = to;
        }

        // ════════════════════════════════════════════════════════
        //  EASING
        // ════════════════════════════════════════════════════════

        private enum EaseType
        {
            Linear,
            OutBack,
            InBack
        }

        private static float ApplyEase(float t, EaseType ease)
        {
            switch (ease)
            {
                case EaseType.OutBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float tm1 = t - 1f;
                    return 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1;

                case EaseType.InBack:
                    const float c1b = 1.70158f;
                    const float c3b = c1b + 1f;
                    return c3b * t * t * t - c1b * t * t;

                case EaseType.Linear:
                default:
                    return t;
            }
        }

        // ════════════════════════════════════════════════════════
        //  VISUAL TOGGLING
        // ════════════════════════════════════════════════════════

        private void SetVisualsActive(bool active)
        {
            foreach (var r in _allRenderers)
            {
                if (r == null) continue;
                if (r is ParticleSystemRenderer) continue;
                r.enabled = active;
            }

            foreach (var c in _allCanvases)
            {
                if (c != null)
                    c.enabled = active;
            }
        }

        // ════════════════════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════════════════════

        private void OnDestroy()
        {
            // If egg is destroyed unexpectedly (scene unload, etc.)
            // clean up the cached minor hit VFX instance
            if (_minorHitVFXInstance != null)
            {
                Destroy(_minorHitVFXInstance.gameObject);
                _minorHitVFXInstance = null;
            }
        }

        // ════════════════════════════════════════════════════════
        //  MISC
        // ════════════════════════════════════════════════════════

        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            Destroy(Instantiate(smokeParticle, position, Quaternion.identity), 3f);
        }
    }
}