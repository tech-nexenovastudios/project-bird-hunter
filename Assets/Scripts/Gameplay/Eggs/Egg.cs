using System;
using System.Collections;
using DG.Tweening;
using Gameplay.Interfaces;
using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Gameplay.Eggs
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Egg : MonoBehaviour
    {
        [SerializeField] public EggTierConfig config;
        [SerializeField] private Camera mainCam;
        [SerializeField] private GameObject minorHitVFXPrefab;
        [SerializeField] private GameObject blastVFXPrefab;
        [SerializeField] private Transform hitVFXPoint;
        [SerializeField] private Transform blastVFXPoint;
        [SerializeField] private GameObject smokeParticle;
        [SerializeField] private float hitScaleMultiplier = 1.1f;
        [SerializeField] private float hitScaleUpDuration = 0.08f;
        [SerializeField] private float hitScaleDownDuration = 0.12f;
        [SerializeField] private float deathScaleTarget = 0.1f;
        [SerializeField] private float deathScaleDuration = 0.25f;
        [SerializeField] private float spawnSlowMovementDuration = 1f;
        [SerializeField] private float spawnMaxSlowVelocity = 1f;
        [SerializeField] private float wallBounceDamping = 0.65f;
        [SerializeField] private float minHorizontalVelocity = 0.3f;
        [SerializeField] private float bounceDirectionRandomness = 0.3f;

        public Action<Egg> OnDestroyed;
        public Action<Egg> OnTrySplit;

        private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private SortingGroup _sortingGroup;
        private Material _mat;
        private Collider2D _collider;
        private Renderer[] _allRenderers;
        private Canvas[] _allCanvases;
        private ParticleSystem _minorHitVFXInstance;
        private Vector3 _originalScale;
        private bool _isDying;
        private bool _isInSpawnPhase;
        private float _spawnPhaseTimer;
        private Coroutine _hitScaleCoroutine;
        private Vector3 _lastBouncePosition;
        private float _maxHeightReachedSinceLastBounce;
        private Health.EggHealth _eggHealth;
        private Transform _cannonTransform;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sortingGroup = GetComponent<SortingGroup>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _eggHealth = GetComponent<Health.EggHealth>();

            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _allCanvases = GetComponentsInChildren<Canvas>(true);

            if (_spriteRenderer != null)
            {
                _mat = _spriteRenderer.material;
            }

            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

            _originalScale = transform.localScale;
        }

        public void Init(EggTierConfig tierConfig, int hp, int sortingIndex)
        {
            config = tierConfig;
            _sortingGroup.sortingOrder = sortingIndex;

            _isDying = false;
            _isInSpawnPhase = true;
            _spawnPhaseTimer = 0f;
            _maxHeightReachedSinceLastBounce = transform.position.y;
            _lastBouncePosition = transform.position;

            transform.localScale = _originalScale;
            transform.rotation = Quaternion.identity;

            _cannonTransform = GameManager.Instance.cannonSpawner.transform;

            if (_mat != null)
            {
                _mat.SetFloat(EdgeWidthID, 0f);
            }

            SetVisualsActive(true);

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            _eggHealth?.Init(tierConfig, hp);

            if (mainCam == null)
            {
                mainCam = Camera.main;
            }

        }

        public void ApplyPhysics()
        {
            _rb.gravityScale = config.gravityScale;
            _rb.mass = config.mass;
            _rb.linearDamping = config.linearDamping;
            _rb.angularVelocity = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.bodyType = RigidbodyType2D.Dynamic;
        }

        private void FixedUpdate()
        {
            if (_isDying)
            {
                return;
            }

            if (_isInSpawnPhase)
            {
                HandleSpawnPhase();
            }

            if (transform.position.y > _maxHeightReachedSinceLastBounce)
            {
                _maxHeightReachedSinceLastBounce = transform.position.y;
            }

            _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, config.maxSpeed);
            HandleScreenEdges();
        }

        private void HandleSpawnPhase()
        {
            _spawnPhaseTimer += Time.fixedDeltaTime;

            if (_rb.linearVelocity.magnitude > spawnMaxSlowVelocity)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * spawnMaxSlowVelocity;
            }

            if (_spawnPhaseTimer < spawnSlowMovementDuration)
            {
                return;
            }

            _isInSpawnPhase = false;
            _spawnPhaseTimer = 0f;
            _rb.linearVelocity = Vector2.zero;
        }

        private void HandleScreenEdges()
        {
            if (mainCam == null || _collider == null)
            {
                return;
            }

            Vector3 position = transform.position;
            Vector3 bottomLeft = mainCam.ViewportToWorldPoint(Vector3.zero);
            Vector3 topRight = mainCam.ViewportToWorldPoint(Vector3.one);

            float boundsWidth = _collider.bounds.extents.x;
            float boundsHeight = _collider.bounds.extents.y;

            float leftBound = bottomLeft.x + boundsWidth;
            float rightBound = topRight.x - boundsWidth;
            float bottomBound = bottomLeft.y + boundsHeight;
            float topBound = topRight.y - boundsHeight;

            Vector2 velocity = _rb.linearVelocity;

            if (position.x < leftBound)
            {
                position.x = leftBound;
                velocity.x = Mathf.Abs(velocity.x) * wallBounceDamping;
            }
            else if (position.x > rightBound)
            {
                position.x = rightBound;
                velocity.x = -Mathf.Abs(velocity.x) * wallBounceDamping;
            }

            if (Mathf.Abs(velocity.x) < minHorizontalVelocity)
            {
                velocity.x = 0f;
            }

            if (position.y < bottomBound)
            {
                position.y = bottomBound;
                velocity.y = Mathf.Max(velocity.y, config.maxSpeed * 0.3f);
            }
            else if (position.y > topBound)
            {
                position.y = topBound;
                velocity.y = Mathf.Min(velocity.y, -config.maxSpeed * 0.2f);
            }

            transform.position = position;
            _rb.linearVelocity = velocity;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDying || !collision.gameObject.CompareTag("Ground"))
            {
                return;
            }

            if (collision.contactCount > 0)
            {
                SpawnSmoke(collision.contacts[0].point);
            }

            ApplyBounceBehavior();
            _lastBouncePosition = transform.position;
            _maxHeightReachedSinceLastBounce = transform.position.y;
        }

        private void ApplyBounceBehavior()
        {
            float bounceVelocity = CalculateBounceVelocity();
            Vector2 randomDirection = GetRandomBounceDirection();
            _rb.linearVelocity = new Vector2(randomDirection.x, bounceVelocity);
        }

        private float CalculateBounceVelocity()
        {
            float gravity = Physics2D.gravity.y * _rb.gravityScale;
            float desiredBounceHeight = config.desiredBounceHeight;
            float requiredVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * desiredBounceHeight);

            float currentHeight = Mathf.Max(0f, transform.position.y - _lastBouncePosition.y);
            float heightRatio = Mathf.Clamp01(currentHeight / desiredBounceHeight);
            float heightInfluence = 1f - heightRatio * 0.6f;

            return requiredVelocity * heightInfluence;
        }

        private Vector2 GetRandomBounceDirection()
        {
            float randomAngle = Random.Range(-bounceDirectionRandomness, bounceDirectionRandomness);
            float maxHorizontalVelocity = config.maxSpeed * config.horizontalIncrease * 0.5f;
            float horizontalVelocity = Random.Range(-maxHorizontalVelocity, maxHorizontalVelocity);

            return new Vector2(horizontalVelocity * (1f + randomAngle), 0f);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player"))
            {
                return;
            }

            if (collision.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(config.cannonDamage);
                _eggHealth?.TakeDamage(Mathf.CeilToInt(config.cannonDamage * 0.5f));
                Invoke(nameof(ExecuteBounceEffect), 0.05f);
            }
        }
        
        public void ApplyBulletHitForce()
        {
            if (_isDying || _rb.linearVelocity.sqrMagnitude < 0.0001f) return;
            
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                
            float reducedSpeed = _rb.linearVelocity.magnitude * 0.5f;
            _rb.linearVelocity = _rb.linearVelocity.normalized * reducedSpeed;
                
            ExecuteBounceEffect();
        }

        private void ExecuteBounceEffect()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, 1 << LayerMask.NameToLayer("Ground"));

            if (hit.collider != null && hit.collider.gameObject.CompareTag("Ground") && hit.distance < config.desiredBounceHeight * 0.25f)
            {
                float distanceMultiplier = 1f - (hit.distance / config.desiredBounceHeight * 0.5f);
                distanceMultiplier = Mathf.Max(0, distanceMultiplier);

                float finalImpulse = 5 * distanceMultiplier;
                
                _rb.AddForce(Vector2.up * finalImpulse, ForceMode2D.Impulse);
            }
        }

        public void PlayHitReaction(float healthPercent)
        {
            if (_isDying)
            {
                return;
            }

            UpdateEdgeWidth(healthPercent);
            PlayMinorHitVFX();
            
            //reduce scale multiplier every time egg is hit
             var reducedScaleMultiplier = hitScaleMultiplier * (1f - healthPercent);
            
            //transform.DOScale(transform.localScale * hitScaleMultiplier, 0.05f).SetEase(Ease.OutBack);
            transform.DOScale(transform.localScale * hitScaleMultiplier, 0.07f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                transform.DOScale(_originalScale, 0.05f).SetEase(Ease.InBack).OnComplete(()=>DOTween.Kill(this));
            });
            transform.DOShakeScale(0.05f, 0.05f, 2, 0, false, ShakeRandomnessMode.Harmonic).SetEase(Ease.OutBack);
        }

        private float CalculateHeightBasedHitImpulse()
        {
            float gravity = Physics2D.gravity.y * _rb.gravityScale;
            float desiredBounceHeight = config.desiredBounceHeight;
            float neededVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * desiredBounceHeight);
            float velocityWithDamping = neededVelocity * 0.8f;
            float currentHeight = Mathf.Max(0f, transform.position.y - _lastBouncePosition.y);
            float heightRatio = Mathf.Clamp01(currentHeight / desiredBounceHeight);
            float heightModifier = 1f - heightRatio * 0.2f;
            float finalVelocity = velocityWithDamping * heightModifier;

            float impulse = _rb.mass * finalVelocity;
            return impulse;
        }

        private void PlayMinorHitVFX()
        {
            if (minorHitVFXPrefab == null || hitVFXPoint == null)
            {
                return;
            }

            if (_minorHitVFXInstance == null)
            {
                GameObject go = Instantiate(minorHitVFXPrefab, hitVFXPoint.position, Quaternion.identity, transform);
                _minorHitVFXInstance = go.GetComponent<ParticleSystem>();

                if (_minorHitVFXInstance == null)
                {
                    Destroy(go);
                    return;
                }
            }

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

        private void UpdateEdgeWidth(float health)
        {
            if (_mat != null)
            {
                var healthPercent = health / _eggHealth.MaxHp;
                _mat.SetFloat(EdgeWidthID, 1f - Mathf.Clamp01(healthPercent));
            }
        }

        public void PlayDeathSequence()
        {
            if (_isDying)
            {
                return;
            }

            _isDying = true;

            if (_hitScaleCoroutine != null)
            {
                StopCoroutine(_hitScaleCoroutine);
                _hitScaleCoroutine = null;
            }

            if (_minorHitVFXInstance != null)
            {
                _minorHitVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(_minorHitVFXInstance.gameObject);
                _minorHitVFXInstance = null;
            }

            if (_mat != null)
            {
                _mat.SetFloat(EdgeWidthID, 1f);
            }
            
            //invoke egg splitting action
            OnTrySplit?.Invoke(this);

            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            Vector3 deathScale = _originalScale * deathScaleTarget;
            yield return AnimateScale(transform.localScale, deathScale, deathScaleDuration, EaseType.InBack);

            SetVisualsActive(false);

            if (blastVFXPrefab != null)
            {
                Vector3 blastPos = blastVFXPoint != null ? blastVFXPoint.position : transform.position;
                GameObject blastGO = Instantiate(blastVFXPrefab, blastPos, Quaternion.identity);

                if (blastGO.TryGetComponent(out ParticleSystem blastPS))
                {
                    blastPS.Play(true);
                    ParticleSystem.MainModule main = blastPS.main;
                    float blastTotalTime = main.duration + main.startLifetime.constantMax;
                    Destroy(blastGO, blastTotalTime);
                    yield return new WaitForSeconds(blastTotalTime);
                }
                else
                {
                    Destroy(blastGO);
                }
            }

            OnDestroyed?.Invoke(this);
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, EaseType ease)
        {
            if (duration <= 0f)
            {
                transform.localScale = to;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(from, to, ApplyEase(t, ease));
                yield return null;
            }

            transform.localScale = to;
        }

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

                default:
                    return t;
            }
        }

        private void SetVisualsActive(bool active)
        {
            foreach (Renderer renderer in _allRenderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                renderer.enabled = active;
            }

            foreach (Canvas canvas in _allCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = active;
                }
            }
        }

        private void OnDestroy()
        {
            Debug.Log("Egg destroyed");
            if (_minorHitVFXInstance != null)
            {
                Destroy(_minorHitVFXInstance.gameObject);
                _minorHitVFXInstance = null;
            }
        }

        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null)
            {
                return;
            }

            Destroy(Instantiate(smokeParticle, position, Quaternion.identity), 3f);
        }
    }
}