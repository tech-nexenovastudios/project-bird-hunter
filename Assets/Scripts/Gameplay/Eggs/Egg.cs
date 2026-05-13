using System;
using System.Collections;
using DG.Tweening;
using Gameplay.Interfaces;
using Gameplay.Pooling;
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
        [SerializeField] private float deathScaleTarget = 0.1f;
        [SerializeField] private float deathScaleDuration = 0.25f;
        [SerializeField] private float spawnSlowMovementDuration = 1f;
        [SerializeField] private float spawnMaxSlowVelocity = 1f;
        [SerializeField] private float wallBounceDamping = 0.65f;
        [SerializeField] private float minHorizontalVelocity = 0.3f;
        [SerializeField] private float bounceDirectionRandomness = 0.15f;

   

        public Action<Egg> OnDestroyed;
        public Action<Egg> OnTrySplit;

        private const float BounceCooldown = 0.08f;
        private const float ApexClampSlack = 1.0f;
        private const float MinBounceHeight = 0.5f;
        private const float BobAmplitude = 0.1f;
        private const float BobFrequency = 0.8f;

        private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");
        private static Transform s_cannonTransform;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private SortingGroup _sortingGroup;
        private Material _mat;
        private Collider2D _collider;
        private Renderer[] _allRenderers;
        private Canvas[] _allCanvases;
        private ParticleSystem _minorHitVFXInstance;
        private Health.EggHealth _eggHealth;

        private Vector3 _prefabScale;
        private Vector3 _originalScale;
        private bool _isDying;
        private bool _isInSpawnPhase;
        private float _spawnPhaseTimer;
        private Vector3 _lastBouncePosition;
        private float _maxHeightReachedSinceLastBounce;

        // Cached per-spawn launch / bounds / layer values.
        private float _baseBounceVelocity;
        private float _maxHorizontalLaunch;
        private float _desiredBounceHeight;
        private float _leftBound, _rightBound, _topBound, _bottomBound;
        private int _groundLayerMask;
        private bool _groundLayerCached;
        private float _lastBounceTime;
        private float _currentLeanZ;

        // Bullet impulse pressure — drains over time, fires evasive launch at threshold.
        private float _bulletImpulseAccumulator;

        // Self-clear freeze state.
        private bool _isFrozen;
        private bool _pendingFreezeAtApex;
        private float _frozenAnchorY;
        private float _bobPhase;

        // Apex-hang: pause vertical motion at the top of each bounce arc for config.apexHangDuration.
        private bool _wasAscending;
        private bool _isHangingAtApex;
        private float _hangTimer;

        // Personality pulse (HP-modulated breathing).
        private float _personalityFreq;
        private float _personalityAmp;
        private float _personalityPhase;
        private bool _hitTweenActive;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sortingGroup = GetComponent<SortingGroup>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _eggHealth = GetComponent<Health.EggHealth>();

            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _allCanvases = GetComponentsInChildren<Canvas>(true);

            if (_spriteRenderer != null) _mat = _spriteRenderer.material;
            if (mainCam == null) mainCam = Camera.main;

            _prefabScale = transform.localScale;
            _originalScale = _prefabScale;

            int layer = LayerMask.NameToLayer("Ground");
            if (layer >= 0)
            {
                _groundLayerMask = 1 << layer;
                _groundLayerCached = true;
            }
        }

        public void Init(EggTierConfig tierConfig, int hp, int sortingIndex)
        {
            if (tierConfig == null) return;

            config = tierConfig;
            if (_sortingGroup != null) _sortingGroup.sortingOrder = sortingIndex;

            _isDying = false;
            _isInSpawnPhase = true;
            _spawnPhaseTimer = 0f;
            _maxHeightReachedSinceLastBounce = transform.position.y;
            _lastBouncePosition = transform.position;
            _isFrozen = false;
            _pendingFreezeAtApex = false;
            _bobPhase = 0f;
            _hitTweenActive = false;
            _lastBounceTime = -1f;
            _bulletImpulseAccumulator = 0f;
            _currentLeanZ = 0f;
            _wasAscending = false;
            _isHangingAtApex = false;
            _hangTimer = 0f;

            ApplyPersonality(tierConfig, hp);

            transform.localScale = _originalScale;
            transform.rotation = Quaternion.identity;

            if (_mat != null) _mat.SetFloat(EdgeWidthID, 0f);
            SetVisualsActive(true);
            if (_collider != null) _collider.enabled = true;
            _eggHealth?.Init(tierConfig, hp);

            if (_rb != null)
            {
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.angularVelocity = 0f;
                _rb.gravityScale = tierConfig.gravityScale;
            }

            CacheScreenBounds();
            CacheBounceConstants(tierConfig);
        }

        private void ApplyPersonality(EggTierConfig tier, int hp)
        {
            float hpSpan = Mathf.Max(1, tier.baseHpMax - tier.baseHpMin);
            float t = Mathf.Clamp01((hp - tier.baseHpMin) / hpSpan);

            float sizeMul = Mathf.Lerp(tier.sizeRange.x, tier.sizeRange.y, t);
            _originalScale = _prefabScale * sizeMul;

            _personalityFreq = Mathf.Lerp(tier.bounceFrequencyRange.y, tier.bounceFrequencyRange.x, t);
            _personalityAmp = Mathf.Lerp(tier.bounceAmplitudeRange.x, tier.bounceAmplitudeRange.y, t);
            _personalityPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void CacheScreenBounds()
        {
            if (mainCam == null || _collider == null) return;

            Vector3 bottomLeft = mainCam.ViewportToWorldPoint(Vector3.zero);
            Vector3 topRight = mainCam.ViewportToWorldPoint(Vector3.one);
            float bw = _collider.bounds.extents.x;
            float bh = _collider.bounds.extents.y;

            _leftBound = bottomLeft.x + bw;
            _rightBound = topRight.x - bw;
            _bottomBound = bottomLeft.y + bh;
            _topBound = topRight.y - bh;
        }

        private void CacheBounceConstants(EggTierConfig tier)
        {
            float worldScreenHeight = mainCam != null
                ? (mainCam.ViewportToWorldPoint(Vector3.one).y - mainCam.ViewportToWorldPoint(Vector3.zero).y)
                : 12f;

            _desiredBounceHeight = Mathf.Max(MinBounceHeight, worldScreenHeight * tier.bounceHeightPercent);

            float gScale = _rb != null ? _rb.gravityScale : tier.gravityScale;
            float gravity = Mathf.Abs(Physics2D.gravity.y * gScale);
            _baseBounceVelocity = Mathf.Sqrt(2f * gravity * _desiredBounceHeight);
            _maxHorizontalLaunch = tier.maxSpeed * tier.horizontalIncrease * 0.5f;
        }

        // ──────────────── Update Loops ────────────────

        private void LateUpdate()
        {
            TickPersonalityPulse();
            TickFlightLean();
        }

        private void TickPersonalityPulse()
        {
            if (_isDying || _isFrozen || _hitTweenActive) return;
            if (_personalityAmp <= 0f || _personalityFreq <= 0f) return;

            _personalityPhase += Time.deltaTime * _personalityFreq * Mathf.PI * 2f;
            float pulse = Mathf.Sin(_personalityPhase) * _personalityAmp;

            transform.localScale = new Vector3(
                _originalScale.x * (1f - pulse * 0.4f),
                _originalScale.y * (1f + pulse),
                _originalScale.z);
        }

        private void TickFlightLean()
        {
            if (_isDying || _isFrozen || _isInSpawnPhase) return;
            if (_rb == null || config == null) return;
            if (config.maxLeanDegrees <= 0f) return;

            float vx = _rb.linearVelocity.x;
            float maxSpeedRef = Mathf.Max(0.1f, config.maxSpeed);
            float leanFactor = Mathf.Clamp(vx / maxSpeedRef, -1f, 1f);
            float targetTilt = -leanFactor * config.maxLeanDegrees;

            _currentLeanZ = Mathf.Lerp(_currentLeanZ, targetTilt, Time.deltaTime * config.leanSmoothing);
            transform.rotation = Quaternion.Euler(0f, 0f, _currentLeanZ);
        }

        private void FixedUpdate()
        {
            if (_isDying) return;

            if (_isFrozen)
            {
                TickFrozenBob();
                return;
            }

            if (_isHangingAtApex)
            {
                TickApexHang();
                return;
            }

            if (_isInSpawnPhase) HandleSpawnPhase();

            if (transform.position.y > _maxHeightReachedSinceLastBounce)
                _maxHeightReachedSinceLastBounce = transform.position.y;

            TryGroundBounce();
            TickBulletImpulseDecay();

            if (_pendingFreezeAtApex && _rb.linearVelocity.y <= 0.1f)
            {
                FreezeNow();
                return;
            }

            // Apex hang: when the ascent flips to descent, pause for the configured duration.
            if (_wasAscending && _rb.linearVelocity.y <= 0f && config != null && config.apexHangDuration > 0f)
            {
                BeginApexHang();
                return;
            }

            Vector2 v = _rb.linearVelocity;
            v.x = Mathf.Clamp(v.x, -config.maxSpeed, config.maxSpeed);
            float maxUp = _baseBounceVelocity * ApexClampSlack;
            if (v.y > maxUp) v.y = maxUp;
            _rb.linearVelocity = v;

            HandleScreenEdges();
        }

        // ──────────────── Self-Clear Freeze ────────────────

        public void FreezeAtNextApex()
        {
            if (_isFrozen || _isDying) return;
            _pendingFreezeAtApex = true;
        }

        public void FreezeNow()
        {
            if (_isFrozen || _isDying) return;
            _isFrozen = true;
            _pendingFreezeAtApex = false;
            _frozenAnchorY = transform.position.y;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            _currentLeanZ = 0f;
            _bulletImpulseAccumulator = 0f;
            transform.localScale = _originalScale;
            transform.rotation = Quaternion.identity;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.gravityScale = 0f;
            }
        }

        private void TickFrozenBob()
        {
            _bobPhase += Time.fixedDeltaTime * BobFrequency * Mathf.PI * 2f;
            Vector3 pos = transform.position;
            pos.y = _frozenAnchorY + Mathf.Sin(_bobPhase) * BobAmplitude;
            transform.position = pos;
        }

        // ──────────────── Spawn / Bounds ────────────────

        private void HandleSpawnPhase()
        {
            _spawnPhaseTimer += Time.fixedDeltaTime;

            if (_rb.linearVelocity.magnitude > spawnMaxSlowVelocity)
                _rb.linearVelocity = _rb.linearVelocity.normalized * spawnMaxSlowVelocity;

            if (_spawnPhaseTimer < spawnSlowMovementDuration) return;

            _isInSpawnPhase = false;
            _spawnPhaseTimer = 0f;
            _rb.linearVelocity = Vector2.zero;
        }

        private void HandleScreenEdges()
        {
            Vector3 position = transform.position;
            Vector2 velocity = _rb.linearVelocity;

            if (position.x < _leftBound)
            {
                position.x = _leftBound;
                velocity.x = Mathf.Abs(velocity.x) * wallBounceDamping;
            }
            else if (position.x > _rightBound)
            {
                position.x = _rightBound;
                velocity.x = -Mathf.Abs(velocity.x) * wallBounceDamping;
            }

            if (Mathf.Abs(velocity.x) < minHorizontalVelocity) velocity.x = 0f;

            if (position.y < _bottomBound)
            {
                position.y = _bottomBound;
                velocity.y = Mathf.Max(velocity.y, config.maxSpeed * 0.3f);
            }
            else if (position.y > _topBound)
            {
                position.y = _topBound;
                velocity.y = Mathf.Min(velocity.y, -config.maxSpeed * 0.2f);
            }

            transform.position = position;
            _rb.linearVelocity = velocity;
        }

        // ──────────────── Ground Bounce ────────────────

        // Primary trigger: raycast in FixedUpdate. Cooldown gates re-entry from the collision fallback.
        private void TryGroundBounce()
        {
            if (_isInSpawnPhase || !_groundLayerCached) return;
            if (_rb == null || _collider == null) return;
            if (_rb.linearVelocity.y > 0.1f) return;
            if (Time.time - _lastBounceTime < BounceCooldown) return;

            float extentY = _collider.bounds.extents.y;
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, Vector2.down, extentY + 0.1f, _groundLayerMask);
            if (hit.collider == null) return;

            _rb.position = new Vector2(_rb.position.x, hit.point.y + extentY);
            ExecuteBounceLaunch(hit.point);
        }

        // Safety net for high-vy frames that skip past the raycast distance.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDying || !collision.gameObject.CompareTag("Ground")) return;
            if (Time.time - _lastBounceTime < BounceCooldown) return;

            Vector2 contactPoint = collision.contactCount > 0
                ? collision.contacts[0].point
                : _rb.position;
            ExecuteBounceLaunch(contactPoint);
        }

        private void ExecuteBounceLaunch(Vector2 contactPoint)
        {
            _lastBounceTime = Time.time;
            SpawnSmoke(contactPoint);
            Gameplay.Events.GameEvents.FireEggBounced(contactPoint);
            ApplyBounceBehavior();
            _lastBouncePosition = transform.position;
            _maxHeightReachedSinceLastBounce = transform.position.y;
            // Arm the apex-hang detector — fires once the upward velocity flips to descent.
            _wasAscending = true;
            PlayGroundSquash();
        }

        private void BeginApexHang()
        {
            _isHangingAtApex = true;
            _wasAscending = false;
            _hangTimer = config.apexHangDuration;
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;
        }

        private void TickApexHang()
        {
            _hangTimer -= Time.fixedDeltaTime;
            if (_hangTimer > 0f) return;

            _isHangingAtApex = false;
            _rb.gravityScale = config != null ? config.gravityScale : 1f;
            // vy left at 0 — gravity now pulls the egg down naturally.
        }

        private void ApplyBounceBehavior()
        {
            float bounceVelocity = CalculateBounceVelocity();
            Vector2 randomDirection = GetRandomBounceDirection();
            _rb.linearVelocity = new Vector2(randomDirection.x, bounceVelocity);
        }

        // Self-correcting: peak of last arc damps overshoots, pumps undershoots back to target.
        private float CalculateBounceVelocity()
        {
            float lastArcHeight = Mathf.Max(0f, _maxHeightReachedSinceLastBounce - _lastBouncePosition.y);
            float heightRatio = Mathf.Clamp01(lastArcHeight / _desiredBounceHeight);
            float heightInfluence = 1f - heightRatio * config.bounceHeightDecay;
            return _baseBounceVelocity * heightInfluence;
        }

        private Vector2 GetRandomBounceDirection()
        {
            float randomAngle = Random.Range(-bounceDirectionRandomness, bounceDirectionRandomness);
            float horizontalVelocity = Random.Range(-_maxHorizontalLaunch, _maxHorizontalLaunch);
            return new Vector2(horizontalVelocity * (1f + randomAngle), 0f);
        }

        // ──────────────── Player Collision ────────────────

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isDying) return;
            if (!collision.CompareTag("Player")) return;
            if (!collision.TryGetComponent(out IDamageable damageable)) return;

            damageable.TakeDamage(config.cannonDamage);
            _eggHealth?.MarkDeadSilent();
            PlayDeathSequence();
        }

        // ──────────────── Bullet Impulse Accumulator ────────────────

        public void ApplyBulletHitForce()
        {
            if (_isDying || _isFrozen || _isHangingAtApex || config == null) return;

            if (!config.enableBulletUpwardPush)
            {
                // Hit-stop: halt vertical motion completely so the egg appears to absorb the
                // bullet in place. Gravity resumes between hits; rapid fire pins the egg.
                // Horizontal momentum is preserved.
                Vector2 vClamp = _rb.linearVelocity;
                vClamp.y = 0f;
                _rb.linearVelocity = vClamp;
                return;
            }

            float proximity = ComputeCannonProximityBoost();
            float deposit = config.bulletImpulsePerHit * (1f + proximity * config.cannonProximityMultiplier);
            _bulletImpulseAccumulator += deposit;

            Vector2 v = _rb.linearVelocity;
            float shake = deposit * 0.3f;
            if (v.y < shake) v.y = shake;
            _rb.linearVelocity = v;

            if (_bulletImpulseAccumulator >= config.evasiveLaunchThreshold)
            {
                TriggerEvasiveLaunch();
                _bulletImpulseAccumulator = 0f;
            }
        }

        // Horizontal-only proximity — an egg high above the cannon isn't in immediate danger.
        private float ComputeCannonProximityBoost()
        {
            EnsureCannonReference();
            if (s_cannonTransform == null || config.cannonDangerRadius <= 0f) return 0f;

            float dx = Mathf.Abs(_rb.position.x - s_cannonTransform.position.x);
            return Mathf.Clamp01(1f - dx / config.cannonDangerRadius);
        }

        // Launches with exactly the velocity needed to reach desiredBounceHeight from current altitude.
        private void TriggerEvasiveLaunch()
        {
            Vector2 v = _rb.linearVelocity;

            float currentAlt = GetHeightAboveGround();
            float remainingHeight = Mathf.Max(0f, _desiredBounceHeight - currentAlt);
            float gravity = Mathf.Abs(Physics2D.gravity.y * _rb.gravityScale);
            float vyNeeded = remainingHeight > 0f ? Mathf.Sqrt(2f * gravity * remainingHeight) : 0f;
            if (v.y < vyNeeded) v.y = vyNeeded;

            float awayDir;
            EnsureCannonReference();
            if (s_cannonTransform != null)
            {
                float dx = _rb.position.x - s_cannonTransform.position.x;
                awayDir = dx >= 0f ? 1f : -1f;
            }
            else
            {
                awayDir = Random.value > 0.5f ? 1f : -1f;
            }
            v.x = awayDir * _maxHorizontalLaunch;

            _rb.linearVelocity = v;
        }

        private float GetHeightAboveGround()
        {
            if (!_groundLayerCached || _collider == null) return 0f;
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, Vector2.down, Mathf.Infinity, _groundLayerMask);
            if (hit.collider == null) return 0f;
            return Mathf.Max(0f, hit.distance - _collider.bounds.extents.y);
        }

        private void TickBulletImpulseDecay()
        {
            if (_bulletImpulseAccumulator <= 0f || config == null) return;
            _bulletImpulseAccumulator = Mathf.Max(0f,
                _bulletImpulseAccumulator - config.bulletImpulseDecay * Time.fixedDeltaTime);
        }

        private static void EnsureCannonReference()
        {
            if (s_cannonTransform != null) return;
            var cannon = GameObject.FindGameObjectWithTag("Player");
            if (cannon != null) s_cannonTransform = cannon.transform;
        }

        // ──────────────── Hit Reaction / Death ────────────────

        public void PlayHitReaction(float healthPercent)
        {
            if (_isDying) return;

            UpdateEdgeWidth(healthPercent);
            PlayMinorHitVFX();

            float squashX = config != null ? config.squashOnHit : 1.15f;
            float stretchY = config != null ? config.stretchOnHit : 0.85f;

            Vector3 squashed = new Vector3(
                _originalScale.x * squashX,
                _originalScale.y * stretchY,
                _originalScale.z);

            _hitTweenActive = true;
            transform.DOKill();
            transform.DOScale(squashed, 0.06f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(_originalScale, 0.22f)
                        .SetEase(Ease.OutElastic, 1f, 0.4f)
                        .OnComplete(() => _hitTweenActive = false);
                });
        }

        private void PlayMinorHitVFX()
        {
            if (minorHitVFXPrefab == null || hitVFXPoint == null) return;

            if (_minorHitVFXInstance == null)
            {
                GameObject go = Instantiate(minorHitVFXPrefab, hitVFXPoint.position, Quaternion.identity, transform);
                _minorHitVFXInstance = go.GetComponent<ParticleSystem>();
                if (_minorHitVFXInstance == null) { Destroy(go); return; }
            }

            _minorHitVFXInstance.transform.position = hitVFXPoint.position;
            _minorHitVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _minorHitVFXInstance.Play(true);
        }

        private void UpdateEdgeWidth(float health)
        {
            if (_mat == null || _eggHealth == null || _eggHealth.MaxHp <= 0) return;
            float healthPercent = health / _eggHealth.MaxHp;
            _mat.SetFloat(EdgeWidthID, 1f - Mathf.Clamp01(healthPercent));
        }

        private void PlayGroundSquash()
        {
            if (_isDying || _isFrozen || config == null) return;

            float dur = Mathf.Max(0.05f, config.groundSquashDuration);
            Vector3 squashed = new Vector3(
                _originalScale.x * config.groundSquashX,
                _originalScale.y * config.groundSquashY,
                _originalScale.z);

            _hitTweenActive = true;
            transform.DOKill();
            transform.localScale = squashed;
            transform.DOScale(_originalScale, dur)
                .SetEase(Ease.OutElastic, 1f, 0.5f)
                .OnComplete(() => _hitTweenActive = false);
        }

        public void PlayDeathSequence()
        {
            if (_isDying) return;

            _isDying = true;
            _hitTweenActive = false;
            transform.DOKill();

            if (_minorHitVFXInstance != null)
            {
                _minorHitVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(_minorHitVFXInstance.gameObject);
                _minorHitVFXInstance = null;
            }

            if (_mat != null) _mat.SetFloat(EdgeWidthID, 1f);

            OnTrySplit?.Invoke(this);
            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            if (_collider != null) _collider.enabled = false;

            Vector3 deathScale = _originalScale * deathScaleTarget;
            yield return AnimateScale(transform.localScale, deathScale, deathScaleDuration);

            SetVisualsActive(false);

            if (blastVFXPrefab != null)
            {
                Vector3 blastPos = blastVFXPoint != null ? blastVFXPoint.position : transform.position;
                GameObject blastInstance = ParticlePoolManager.Spawn(blastVFXPrefab, blastPos);

                float blastTotalTime = 0f;
                if (blastInstance != null && blastInstance.TryGetComponent(out ParticleSystem blastPS))
                {
                    ParticleSystem.MainModule main = blastPS.main;
                    blastTotalTime = main.duration + main.startLifetime.constantMax;
                }

                ParticlePoolManager.Despawn(blastVFXPrefab, blastInstance, blastTotalTime);
                if (blastTotalTime > 0f) yield return new WaitForSeconds(blastTotalTime);
            }

            OnDestroyed?.Invoke(this);
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f) { transform.localScale = to; yield break; }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = InBack(t);
                transform.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }
            transform.localScale = to;
        }

        private static float InBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        private void SetVisualsActive(bool active)
        {
            for (int i = 0; i < _allRenderers.Length; i++)
            {
                Renderer r = _allRenderers[i];
                if (r == null || r is ParticleSystemRenderer) continue;
                r.enabled = active;
            }

            for (int i = 0; i < _allCanvases.Length; i++)
            {
                Canvas c = _allCanvases[i];
                if (c != null) c.enabled = active;
            }
        }

        private void OnDestroy()
        {
            if (_minorHitVFXInstance != null)
            {
                Destroy(_minorHitVFXInstance.gameObject);
                _minorHitVFXInstance = null;
            }
        }

        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            var instance = ParticlePoolManager.Spawn(smokeParticle, position);
            ParticlePoolManager.Despawn(smokeParticle, instance, 3f);
        }
    }
}
