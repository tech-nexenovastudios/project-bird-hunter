using DG.Tweening;
using Gameplay.Birds;
using Gameplay.Events;
using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.VFX
{
    /// <summary>
    /// Event-driven screen shake. Mirrors SFXController / HapticsController: subscribes to
    /// GameEvents and shakes Camera.main. Other systems can also drive a one-off shake via the
    /// static <see cref="Shake"/> entry point (e.g. boss attacks).
    /// </summary>
    public class CameraShakeController : MonoBehaviour
    {
        [System.Serializable]
        private struct ShakePreset
        {
            public float duration;
            public float strength;
            public int vibrato;
        }

        [Header("Combat")]
        [SerializeField] private ShakePreset eggDestroy   = new ShakePreset { duration = 0.12f, strength = 0.06f, vibrato = 10 };
        [SerializeField] private ShakePreset birdDestroy  = new ShakePreset { duration = 0.12f, strength = 0.06f, vibrato = 10 };
        [SerializeField] private ShakePreset cannonHit    = new ShakePreset { duration = 0.30f, strength = 0.25f, vibrato = 18 };
        [SerializeField] private ShakePreset playerDeath  = new ShakePreset { duration = 0.50f, strength = 0.40f, vibrato = 20 };

        [Header("Boss")]
        [SerializeField] private ShakePreset bossPhase2   = new ShakePreset { duration = 0.50f, strength = 0.35f, vibrato = 18 };
        [SerializeField] private ShakePreset bossBurst    = new ShakePreset { duration = 0.25f, strength = 0.20f, vibrato = 14 };
        [SerializeField] private ShakePreset bossDefeated = new ShakePreset { duration = 0.60f, strength = 0.45f, vibrato = 20 };

        [Header("Global")]
        [Tooltip("Master multiplier on every shake's strength. 0 disables all shake.")]
        [SerializeField, Range(0f, 2f)] private float intensityScale = 0.5f;

        private const string PrefKey = "ScreenShakeEnabled";
        private static bool _enabled = true;

        private Transform _camTransform;
        private Vector3 _baseLocalPos;
        private Tween _currentShake;
        private bool _isPaused;

        public static CameraShakeController Instance { get; private set; }

        public static bool IsEnabled => _enabled;

        public static void SetEnabled(bool value)
        {
            _enabled = value;
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Static convenience for systems that want to drive a one-off shake directly.</summary>
        public static void Shake(float duration, float strength, int vibrato = 10)
            => Instance?.DoShake(duration, strength, vibrato);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _enabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnEggDestroyed    += OnEggDestroyed;
            GameEvents.OnBirdDestroyed   += OnBirdDestroyed;
            GameEvents.OnCannonHit       += OnCannonHit;
            GameEvents.OnPlayerDeath     += OnPlayerDeath;
            GameEvents.OnBossPhase2      += OnBossPhase2;
            GameEvents.OnBossBurstAttack += OnBossBurst;
            GameEvents.OnBossDefeated    += OnBossDefeated;
            GameEvents.OnPauseToggled    += OnPauseToggled;
        }

        private void OnDisable()
        {
            GameEvents.OnEggDestroyed    -= OnEggDestroyed;
            GameEvents.OnBirdDestroyed   -= OnBirdDestroyed;
            GameEvents.OnCannonHit       -= OnCannonHit;
            GameEvents.OnPlayerDeath     -= OnPlayerDeath;
            GameEvents.OnBossPhase2      -= OnBossPhase2;
            GameEvents.OnBossBurstAttack -= OnBossBurst;
            GameEvents.OnBossDefeated    -= OnBossDefeated;
            GameEvents.OnPauseToggled    -= OnPauseToggled;

            KillCurrent();
        }

        private void OnEggDestroyed(IDamageable _, int __, Vector3 ___)  => Play(eggDestroy);
        private void OnBirdDestroyed(IDamageable _, int __, Vector3 ___) => Play(birdDestroy);
        private void OnCannonHit(int _)                                  => Play(cannonHit);
        private void OnPlayerDeath()                                     => Play(playerDeath);
        private void OnBossPhase2(BossBird _)                            => Play(bossPhase2);
        private void OnBossBurst(BossBird _, int __)                     => Play(bossBurst);
        private void OnBossDefeated(BossBird _)                          => Play(bossDefeated);

        private void OnPauseToggled(bool isPaused)
        {
            _isPaused = isPaused;
            if (isPaused) KillCurrent();
        }

        private void Play(ShakePreset preset) => DoShake(preset.duration, preset.strength, preset.vibrato);

        private void DoShake(float duration, float strength, int vibrato)
        {
            if (!_enabled || _isPaused || intensityScale <= 0f) return;
            if (duration <= 0f || strength <= 0f) return;
            if (!ResolveCamera()) return;

            // Snap any in-progress shake back to base before starting a new one so overlapping
            // shakes don't accumulate a drift offset, then re-capture base in case the camera
            // legitimately moved while idle.
            if (_currentShake != null && _currentShake.IsActive())
            {
                _currentShake.Kill();
                _camTransform.localPosition = _baseLocalPos;
            }
            _baseLocalPos = _camTransform.localPosition;

            _currentShake = _camTransform
                .DOShakePosition(duration, strength * intensityScale, vibrato)
                .OnComplete(() => { if (_camTransform != null) _camTransform.localPosition = _baseLocalPos; });
        }

        private bool ResolveCamera()
        {
            if (_camTransform != null) return true;
            Camera cam = Camera.main;
            if (cam == null) return false;
            _camTransform = cam.transform;
            _baseLocalPos = _camTransform.localPosition;
            return true;
        }

        private void KillCurrent()
        {
            if (_currentShake != null && _currentShake.IsActive())
                _currentShake.Kill();
            _currentShake = null;
            if (_camTransform != null) _camTransform.localPosition = _baseLocalPos;
        }
    }
}
