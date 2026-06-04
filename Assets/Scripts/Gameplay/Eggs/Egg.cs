using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        [Header("Self-Clear Drift")]
        [SerializeField] private float driftMinSpeed = 0.4f;
        [SerializeField] private float driftMaxSpeed = 1.0f;
        [SerializeField] private float driftRetargetMin = 1.5f;
        [SerializeField] private float driftRetargetMax = 3.5f;
        [Tooltip("Y floor for drift mode as a fraction of screen height (0 = bottom, 1 = top).")]
        [SerializeField, Range(0f, 1f)] private float driftMinHeightPercent = 0.5f;

        public Action<Egg> OnDestroyed;        // fires at logical death (HP=0) — synchronous with kill
        public Action<Egg> OnReleaseReady;     // fires after death animation — safe to return to pool
        public Action<Egg> OnTrySplit;

        public int UnscoredHpOnDeath { get; private set; }

        private const float BounceCooldown = 0.08f;
        private const float ApexClampSlack = 1.0f;
        private const float MinBounceHeight = 0.5f;
        private const float LeanEpsilon = 0.01f;
        private const float EvasiveRaycastDistance = 50f;

        private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");
        private static Transform _cannonTransform;
        private static MaterialPropertyBlock _mpb;
        private static Camera _cachedScreenCamera;
        private static int _cachedScreenWidth;
        private static int _cachedScreenHeight;
        private static Vector2 _cachedScreenWorldBottomLeft;
        private static Vector2 _cachedScreenWorldTopRight;
        private static float _cachedScreenWorldHeight;

        private Rigidbody2D _rb;
        private Transform _t;
        private SpriteRenderer _spriteRenderer;
        private SortingGroup _sortingGroup;
        private Collider2D _collider;
        private Renderer[] _allRenderers;
        private Canvas[] _allCanvases;
        private Health.EggHealth _eggHealth;

        private Vector3 _prefabScale;
        private Vector3 _originalScale;
        private bool _isDying;
        private bool _isInSpawnPhase;
        private float _spawnPhaseTimer;
        private Vector3 _lastBouncePosition;
        private float _maxHeightReachedSinceLastBounce;

        private float _baseBounceVelocity;
        private float _maxHorizontalLaunch;
        private float _desiredBounceHeight;
        private float _leftBound, _rightBound, _topBound, _bottomBound;
        private int _groundLayerMask;
        private bool _groundLayerCached;
        private float _lastBounceTime;
        private float _currentLeanZ;

        private float _bulletImpulseAccumulator;

        private bool _isFrozen;
        private bool _pendingFreezeAtApex;
        private Vector2 _driftDirection;
        private float _driftSpeed;
        private float _driftRetargetTimer;
        private float _driftMinY;

        private bool _wasAscending;
        private bool _isHangingAtApex;
        private float _hangTimer;

        private float _personalityFreq;
        private float _personalityAmp;
        private float _personalityPhase;
        private bool _hitTweenActive;

        private IEggState _currentState;
        private CancellationTokenSource _cts;
        private TweenCallback _onHitTweenComplete;

        // ─── Unity lifecycle ───

        private void Awake()
        {
            _t = transform;
            _rb = GetComponent<Rigidbody2D>();
            _sortingGroup = GetComponent<SortingGroup>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _eggHealth = GetComponent<Health.EggHealth>();

            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _allCanvases = GetComponentsInChildren<Canvas>(true);

            if (mainCam == null) mainCam = Camera.main;

            _prefabScale = _t.localScale;
            _originalScale = _prefabScale;

            int layer = LayerMask.NameToLayer("Ground");
            if (layer >= 0)
            {
                _groundLayerMask = 1 << layer;
                _groundLayerCached = true;
            }

            _onHitTweenComplete = () => _hitTweenActive = false;
        }

        private void OnDestroy()
        {
            CancelAsync();
        }

        public void Init(EggTierConfig tierConfig, int hp, int sortingIndex)
        {
            if (tierConfig == null) return;
            config = tierConfig;
            if (_sortingGroup != null) _sortingGroup.sortingOrder = sortingIndex;

            _isDying = false;
            UnscoredHpOnDeath = 0;
            _maxHeightReachedSinceLastBounce = _t.position.y;
            _lastBouncePosition = _t.position;
            _isFrozen = false;
            _pendingFreezeAtApex = false;
            _hitTweenActive = false;
            _lastBounceTime = -1f;
            _bulletImpulseAccumulator = 0f;
            _currentLeanZ = 0f;
            _wasAscending = false;
            _isHangingAtApex = false;
            _hangTimer = 0f;
            _spawnPhaseTimer = 0f;

            ApplyPersonality(tierConfig, hp);

            _t.localScale = _originalScale;
            _t.rotation = Quaternion.identity;

            SetEdgeWidth(0f);
            SetVisualsActive(true);
            if (_collider != null) _collider.enabled = true;
            _eggHealth?.Init(tierConfig, hp);

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.gravityScale = tierConfig.gravityScale;
            }

            CacheScreenBounds();
            CacheBounceConstants(tierConfig);

            CancelAsync();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            EnterState(EggStates.Spawn);
        }

        public void OnPoolAcquire()
        {
            gameObject.SetActive(true);
        }

        public void OnPoolRelease()
        {
            CancelAsync();
            _t.DOKill();
            _hitTweenActive = false;

            _currentState?.OnExit(this);
            _currentState = null;

            OnDestroyed = null;
            OnReleaseReady = null;
            OnTrySplit = null;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }

            gameObject.SetActive(false);
        }

        private void CancelAsync()
        {
            if (_cts == null) return;
            try { _cts.Cancel(); } catch { /* already disposed */ }
            _cts.Dispose();
            _cts = null;
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

            if (_cachedScreenCamera != mainCam ||
                _cachedScreenWidth != Screen.width ||
                _cachedScreenHeight != Screen.height)
            {
                _cachedScreenCamera = mainCam;
                _cachedScreenWidth = Screen.width;
                _cachedScreenHeight = Screen.height;
                Vector3 bottomLeft = mainCam.ViewportToWorldPoint(Vector3.zero);
                Vector3 topRight = mainCam.ViewportToWorldPoint(Vector3.one);
                _cachedScreenWorldBottomLeft = bottomLeft;
                _cachedScreenWorldTopRight = topRight;
                _cachedScreenWorldHeight = topRight.y - bottomLeft.y;
            }

            float bw = _collider.bounds.extents.x;
            float bh = _collider.bounds.extents.y;
            _leftBound = _cachedScreenWorldBottomLeft.x + bw;
            _rightBound = _cachedScreenWorldTopRight.x - bw;
            _bottomBound = _cachedScreenWorldBottomLeft.y + bh;
            _topBound = _cachedScreenWorldTopRight.y - bh;
        }

        private void CacheBounceConstants(EggTierConfig tier)
        {
            float worldScreenHeight = _cachedScreenWorldHeight > 0f ? _cachedScreenWorldHeight : 12f;
            _desiredBounceHeight = Mathf.Max(MinBounceHeight, worldScreenHeight * tier.bounceHeightPercent);

            float gScale = _rb != null ? _rb.gravityScale : tier.gravityScale;
            float gravity = Mathf.Abs(Physics2D.gravity.y * gScale);
            _baseBounceVelocity = Mathf.Sqrt(2f * gravity * _desiredBounceHeight);
            _maxHorizontalLaunch = tier.maxSpeed * tier.horizontalIncrease * 0.5f;
        }

        // ─── Update loops ───

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

            _t.localScale = new Vector3(
                _originalScale.x * (1f - pulse * 0.4f),
                _originalScale.y * (1f + pulse),
                _originalScale.z);
        }

        private void TickFlightLean()
        {
            if (_isDying || _isFrozen || _isInSpawnPhase) return;
            if (_rb == null || config == null || config.maxLeanDegrees <= 0f) return;

            float vx = _rb.linearVelocity.x;
            float maxSpeedRef = Mathf.Max(0.1f, config.maxSpeed);
            float leanFactor = Mathf.Clamp(vx / maxSpeedRef, -1f, 1f);
            float targetTilt = -leanFactor * config.maxLeanDegrees;

            float newLean = Mathf.Lerp(_currentLeanZ, targetTilt, Time.deltaTime * config.leanSmoothing);
            if (Mathf.Abs(newLean - _currentLeanZ) > LeanEpsilon)
            {
                _currentLeanZ = newLean;
                _t.rotation = Quaternion.Euler(0f, 0f, _currentLeanZ);
            }
        }

        private void FixedUpdate()
        {
            if (_isDying) return;
            _currentState?.FixedTick(this);
        }

        // ─── Strategy pattern: egg states ───

        private interface IEggState
        {
            void OnEnter(Egg e);
            void OnExit(Egg e);
            void FixedTick(Egg e);
        }

        private static class EggStates
        {
            public static readonly IEggState Spawn = new SpawnEggState();
            public static readonly IEggState Bouncing = new BouncingEggState();
            public static readonly IEggState ApexHang = new ApexHangEggState();
            public static readonly IEggState Drift = new DriftEggState();
        }

        private void EnterState(IEggState newState)
        {
            _currentState?.OnExit(this);
            _currentState = newState;
            newState?.OnEnter(this);
        }

        private sealed class SpawnEggState : IEggState
        {
            public void OnEnter(Egg e) { e._isInSpawnPhase = true; e._spawnPhaseTimer = 0f; }
            public void OnExit(Egg e)  { e._isInSpawnPhase = false; }
            public void FixedTick(Egg e) { e.TickSpawnPhase(); }
        }

        private sealed class BouncingEggState : IEggState
        {
            public void OnEnter(Egg e) { }
            public void OnExit(Egg e) { }
            public void FixedTick(Egg e)
            {
                if (e._t.position.y > e._maxHeightReachedSinceLastBounce)
                    e._maxHeightReachedSinceLastBounce = e._t.position.y;

                e.TryGroundBounce();
                e.TickBulletImpulseDecay();

                if (e._pendingFreezeAtApex && e._rb.linearVelocity.y <= 0.1f)
                {
                    e.FreezeNow();
                    return;
                }

                if (e._wasAscending && e._rb.linearVelocity.y <= 0f && e.config != null && e.config.apexHangDuration > 0f)
                {
                    e.EnterState(EggStates.ApexHang);
                    return;
                }

                Vector2 v = e._rb.linearVelocity;
                v.x = Mathf.Clamp(v.x, -e.config.maxSpeed, e.config.maxSpeed);
                float maxUp = e._baseBounceVelocity * ApexClampSlack;
                if (v.y > maxUp) v.y = maxUp;
                e._rb.linearVelocity = v;

                e.HandleScreenEdges();
            }
        }

        private sealed class ApexHangEggState : IEggState
        {
            public void OnEnter(Egg e)
            {
                e._isHangingAtApex = true;
                e._wasAscending = false;
                e._hangTimer = e.config.apexHangDuration;
                e._rb.linearVelocity = Vector2.zero;
                e._rb.gravityScale = 0f;
            }
            public void OnExit(Egg e)
            {
                e._isHangingAtApex = false;
                e._rb.gravityScale = e.config != null ? e.config.gravityScale : 1f;
            }
            public void FixedTick(Egg e)
            {
                e._hangTimer -= Time.fixedDeltaTime;
                if (e._hangTimer <= 0f) e.EnterState(EggStates.Bouncing);
            }
        }

        private sealed class DriftEggState : IEggState
        {
            public void OnEnter(Egg e) { e.PickDriftTarget(); }
            public void OnExit(Egg e) { }
            public void FixedTick(Egg e) { e.TickDrift(); }
        }

        // ─── Spawn phase ───

        private void TickSpawnPhase()
        {
            _spawnPhaseTimer += Time.fixedDeltaTime;
            if (_rb.linearVelocity.magnitude > spawnMaxSlowVelocity)
                _rb.linearVelocity = _rb.linearVelocity.normalized * spawnMaxSlowVelocity;
            if (_spawnPhaseTimer < spawnSlowMovementDuration) return;

            _rb.linearVelocity = Vector2.zero;
            EnterState(EggStates.Bouncing);
        }

        // ─── Self-clear drift ───

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
            _currentLeanZ = 0f;
            _bulletImpulseAccumulator = 0f;
            _t.localScale = _originalScale;
            _t.rotation = Quaternion.identity;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.gravityScale = 0f;
            }

            ComputeDriftFloor();
            EnterState(EggStates.Drift);
        }

        private void ComputeDriftFloor()
        {
            _driftMinY = _cachedScreenWorldBottomLeft.y + _cachedScreenWorldHeight * driftMinHeightPercent;
        }

        private void PickDriftTarget()
        {
            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
            else dir.Normalize();
            _driftDirection = dir;
            _driftSpeed = Random.Range(driftMinSpeed, driftMaxSpeed);
            _driftRetargetTimer = Random.Range(driftRetargetMin, driftRetargetMax);
        }

        private void TickDrift()
        {
            _driftRetargetTimer -= Time.fixedDeltaTime;
            if (_driftRetargetTimer <= 0f) PickDriftTarget();

            Vector2 pos = _rb.position;
            pos += _driftDirection * (_driftSpeed * Time.fixedDeltaTime);

            if (pos.x < _leftBound)       { pos.x = _leftBound;  _driftDirection.x =  Mathf.Abs(_driftDirection.x); }
            else if (pos.x > _rightBound) { pos.x = _rightBound; _driftDirection.x = -Mathf.Abs(_driftDirection.x); }

            if (pos.y < _driftMinY)       { pos.y = _driftMinY;  _driftDirection.y =  Mathf.Abs(_driftDirection.y); }
            else if (pos.y > _topBound)   { pos.y = _topBound;   _driftDirection.y = -Mathf.Abs(_driftDirection.y); }

            _rb.MovePosition(pos);
        }

        // ─── Bounds + bounce ───

        private void HandleScreenEdges()
        {
            Vector3 position = _t.position;
            Vector2 velocity = _rb.linearVelocity;

            if (position.x < _leftBound) { position.x = _leftBound; velocity.x = Mathf.Abs(velocity.x) * wallBounceDamping; }
            else if (position.x > _rightBound) { position.x = _rightBound; velocity.x = -Mathf.Abs(velocity.x) * wallBounceDamping; }

            if (Mathf.Abs(velocity.x) < minHorizontalVelocity) velocity.x = 0f;

            if (position.y < _bottomBound) { position.y = _bottomBound; velocity.y = Mathf.Max(velocity.y, config.maxSpeed * 0.3f); }
            else if (position.y > _topBound) { position.y = _topBound; velocity.y = Mathf.Min(velocity.y, -config.maxSpeed * 0.2f); }

            _t.position = position;
            _rb.linearVelocity = velocity;
        }

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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDying || !collision.gameObject.CompareTag("Ground")) return;
            if (Time.time - _lastBounceTime < BounceCooldown) return;

            Vector2 contactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : _rb.position;
            ExecuteBounceLaunch(contactPoint);
        }

        private void ExecuteBounceLaunch(Vector2 contactPoint)
        {
            _lastBounceTime = Time.time;
            SpawnSmoke(contactPoint);
            Gameplay.Events.GameEvents.FireEggBounced(contactPoint);
            ApplyBounceBehavior();
            _lastBouncePosition = _t.position;
            _maxHeightReachedSinceLastBounce = _t.position.y;
            _wasAscending = true;
            PlayGroundSquash();
        }

        private void ApplyBounceBehavior()
        {
            float bounceVelocity = CalculateBounceVelocity();
            Vector2 randomDirection = GetRandomBounceDirection();
            _rb.linearVelocity = new Vector2(randomDirection.x, bounceVelocity);
        }

        private float CalculateBounceVelocity()
        {
            float lastArcHeight = Mathf.Max(0f, _maxHeightReachedSinceLastBounce - _lastBouncePosition.y);
            float heightRatio = Mathf.Clamp01(lastArcHeight / _desiredBounceHeight);
            float heightInfluence = 1f - heightRatio * config.bounceHeightDecay;
            return _baseBounceVelocity * heightInfluence;
        }

        private Vector2 GetRandomBounceDirection()
        {
            float currentVx = _rb.linearVelocity.x;
            float sign = Mathf.Abs(currentVx) > 0.05f
                ? -Mathf.Sign(currentVx)
                : (Random.value < 0.5f ? -1f : 1f);

            float jitter = 2f + Random.Range(-bounceDirectionRandomness, bounceDirectionRandomness);
            return new Vector2(sign * _maxHorizontalLaunch * jitter, 0f);
        }

        // ─── Player collision ───

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isDying) return;
            if (!collision.CompareTag("Player")) return;
            if (!collision.TryGetComponent(out IDamageable damageable)) return;

            UnscoredHpOnDeath = _eggHealth != null ? _eggHealth.CurrentHp : 0;

            damageable.TakeDamage(config.cannonDamage);
            _eggHealth?.MarkDeadSilent();
            PlayDeathSequence();
        }

        // ─── Bullet impulse ───

        public void ApplyBulletHitForce()
        {
            if (_isDying || _isFrozen || _isHangingAtApex || config == null) return;

            if (!config.enableBulletUpwardPush)
            {
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

        private float ComputeCannonProximityBoost()
        {
            EnsureCannonReference();
            if (_cannonTransform == null || config.cannonDangerRadius <= 0f) return 0f;
            float dx = Mathf.Abs(_rb.position.x - _cannonTransform.position.x);
            return Mathf.Clamp01(1f - dx / config.cannonDangerRadius);
        }

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
            if (_cannonTransform != null)
            {
                float dx = _rb.position.x - _cannonTransform.position.x;
                awayDir = dx >= 0f ? 1f : -1f;
            }
            else awayDir = Random.value > 0.5f ? 1f : -1f;
            v.x = awayDir * _maxHorizontalLaunch;
            _rb.linearVelocity = v;
        }

        private float GetHeightAboveGround()
        {
            if (!_groundLayerCached || _collider == null) return 0f;
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, Vector2.down, EvasiveRaycastDistance, _groundLayerMask);
            if (hit.collider == null) return 0f;
            return Mathf.Max(0f, hit.distance - _collider.bounds.extents.y);
        }

        private void TickBulletImpulseDecay()
        {
            if (_bulletImpulseAccumulator <= 0f || config == null) return;
            _bulletImpulseAccumulator = Mathf.Max(0f, _bulletImpulseAccumulator - config.bulletImpulseDecay * Time.fixedDeltaTime);
        }

        public static void InvalidateCannonReference() => _cannonTransform = null;

        private static void EnsureCannonReference()
        {
            if (_cannonTransform != null) return;
            var cannon = GameObject.FindGameObjectWithTag("Player");
            if (cannon != null) _cannonTransform = cannon.transform;
        }

        // ─── Hit / death ───

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
            _t.DOKill();

            DOTween.Sequence()
                .Append(_t.DOScale(squashed, 0.06f).SetEase(Ease.OutQuad))
                .Append(_t.DOScale(_originalScale, 0.22f).SetEase(Ease.OutElastic, 1f, 0.4f))
                .OnComplete(_onHitTweenComplete);
        }

        private void PlayMinorHitVFX()
        {
            if (minorHitVFXPrefab == null || hitVFXPoint == null) return;

            GameObject instance = ParticlePoolManager.Spawn(minorHitVFXPrefab, hitVFXPoint.position);
            if (instance == null) return;

            float lifetime = ConfigureOneShotAndGetLifetime(instance);
            ParticlePoolManager.Despawn(minorHitVFXPrefab, instance, lifetime);
        }

        // Walks every ParticleSystem (root + children), disables looping, and returns the worst-case
        // single-cycle visible time — emission duration plus longest particle lifetime — so the pool
        // can despawn as soon as the burst finishes naturally.
        private static float ConfigureOneShotAndGetLifetime(GameObject instance)
        {
            if (instance == null) return 1f;
            var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            float worst = 0f;
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null) continue;
                var main = ps.main;
                if (main.loop) main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;

                float lifetime = main.startLifetime.mode == ParticleSystemCurveMode.Constant
                    ? main.startLifetime.constant
                    : main.startLifetime.constantMax;
                float total = main.duration + lifetime;
                if (total > worst) worst = total;
            }
            return worst > 0f ? worst : 1f;
        }

        private void UpdateEdgeWidth(float health)
        {
            if (_eggHealth == null || _eggHealth.MaxHp <= 0) return;
            SetEdgeWidth(1f - Mathf.Clamp01(health / _eggHealth.MaxHp));
        }

        private void SetEdgeWidth(float value)
        {
            if (_spriteRenderer == null) return;
            _mpb ??= new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(EdgeWidthID, value);
            _spriteRenderer.SetPropertyBlock(_mpb);
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
            _t.DOKill();
            _t.localScale = squashed;
            _t.DOScale(_originalScale, dur)
                .SetEase(Ease.OutElastic, 1f, 0.5f)
                .OnComplete(_onHitTweenComplete);
        }

        public void MarkScoreSilent() => _eggHealth?.MarkDeadSilent();

        public void PlayDeathSequence()
        {
            if (_isDying) return;
            _isDying = true;
            _hitTweenActive = false;
            _t.DOKill();

            SetEdgeWidth(1f);
            OnTrySplit?.Invoke(this);
            OnDestroyed?.Invoke(this);   // fires NOW so listeners (level-complete, scoring) react instantly

            DeathSequenceAsync(_cts != null ? _cts.Token : default).Forget();
        }

        private async UniTaskVoid DeathSequenceAsync(CancellationToken ct)
        {
            try
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = RigidbodyType2D.Kinematic;
                if (_collider != null) _collider.enabled = false;

                Vector3 deathScale = _originalScale * deathScaleTarget;
                await AnimateScaleAsync(_t.localScale, deathScale, deathScaleDuration, ct);

                SetVisualsActive(false);

                if (blastVFXPrefab != null)
                {
                    Vector3 blastPos = blastVFXPoint != null ? blastVFXPoint.position : _t.position;
                    GameObject blastInstance = ParticlePoolManager.Spawn(blastVFXPrefab, blastPos);
                    float blastTotalTime = ConfigureOneShotAndGetLifetime(blastInstance);

                    ParticlePoolManager.Despawn(blastVFXPrefab, blastInstance, blastTotalTime);
                    if (blastTotalTime > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(blastTotalTime), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
                }

                OnReleaseReady?.Invoke(this);
            }
            catch (OperationCanceledException) { /* released to pool mid-sequence */ }
        }

        private async UniTask AnimateScaleAsync(Vector3 from, Vector3 to, float duration, CancellationToken ct)
        {
            if (duration <= 0f) { _t.localScale = to; return; }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float tNorm = Mathf.Clamp01(elapsed / duration);
                _t.localScale = Vector3.LerpUnclamped(from, to, InBack(tNorm));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            _t.localScale = to;
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

        private void SpawnSmoke(Vector2 position)
        {
            if (smokeParticle == null) return;
            var instance = ParticlePoolManager.Spawn(smokeParticle, position);
            ParticlePoolManager.Despawn(smokeParticle, instance, 3f);
        }
    }
}
