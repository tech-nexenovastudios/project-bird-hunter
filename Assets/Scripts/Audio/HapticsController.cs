using System.Runtime.InteropServices;
using Gameplay.Birds;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using UnityEngine;

namespace Audio
{
    public class HapticsController : MonoBehaviour
    {
        public enum Intensity { Light = 0, Medium = 1, Heavy = 2 }

        [Header("Combat")]
        [SerializeField] private Intensity eggHitIntensity        = Intensity.Light;
        [SerializeField] private Intensity eggDestroyIntensity    = Intensity.Medium;
        [SerializeField] private Intensity birdDestroyIntensity   = Intensity.Light;
        [SerializeField] private Intensity cannonHitIntensity     = Intensity.Heavy;
        [SerializeField] private Intensity playerDeathIntensity   = Intensity.Heavy;
        [SerializeField] private Intensity bossPhase2Intensity    = Intensity.Heavy;
        [SerializeField] private Intensity bossDefeatedIntensity  = Intensity.Heavy;

        [Header("Level Flow")]
        [SerializeField] private Intensity levelCompletedIntensity   = Intensity.Medium;
        [SerializeField] private Intensity chapterCompletedIntensity = Intensity.Heavy;
        [SerializeField] private Intensity graceStartedIntensity     = Intensity.Light;
        [SerializeField] private Intensity graceEndedIntensity       = Intensity.Medium;

        [Header("Slot Machine")]
        [Tooltip("Fires when player taps a reel to pick a powerup.")]
        [SerializeField] private Intensity reelTapIntensity        = Intensity.Light;
        [Tooltip("Fires when player taps Confirm to lock in the powerup.")]
        [SerializeField] private Intensity powerupConfirmIntensity = Intensity.Medium;
        [Tooltip("Fires when all reels finish their visual spin.")]
        [SerializeField] private Intensity reelsStoppedIntensity   = Intensity.Light;

        [Header("Android pulse (ms / 1-255 amplitude)")]
        [SerializeField] private int lightDurationMs = 10;
        [SerializeField] private int lightAmplitude = 80;
        [SerializeField] private int mediumDurationMs = 25;
        [SerializeField] private int mediumAmplitude = 150;
        [SerializeField] private int heavyDurationMs = 40;
        [SerializeField] private int heavyAmplitude = 255;

        private const string PrefKey = "VibrationEnabled";
        private static bool _enabled = true;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _HapticImpact(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdkInt;
        private static bool _androidReady;
#endif

        public static HapticsController Instance { get; private set; }

        public static bool IsEnabled => _enabled;

        public static void SetEnabled(bool value)
        {
            _enabled = value;
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Static convenience for fire-and-forget callers (UI buttons, etc.).</summary>
        public static void Tap(Intensity intensity) => Instance?.Play(intensity);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _enabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
            InitAndroid();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit                 += OnEggHit;
            GameEvents.OnEggDestroyed           += OnEggDestroyed;
            GameEvents.OnBirdDestroyed          += OnBirdDestroyed;
            GameEvents.OnCannonHit              += OnCannonHit;
            GameEvents.OnPlayerDeath            += OnPlayerDeath;
            GameEvents.OnBossPhase2             += OnBossPhase2;
            GameEvents.OnBossDefeated           += OnBossDefeated;

            GameEvents.OnLevelCompleted         += OnLevelCompleted;
            GameEvents.OnChapterCompleted       += OnChapterCompleted;
            GameEvents.OnSelfClearStarted       += OnSelfClearStarted;
            GameEvents.OnSelfClearEnded         += OnSelfClearEnded;

            GameEvents.OnPowerupCommitted       += OnReelTap;
            GameEvents.OnPowerupSelected        += OnPowerupConfirm;
            GameEvents.OnSpinAnimationCompleted += OnReelsStopped;
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit                 -= OnEggHit;
            GameEvents.OnEggDestroyed           -= OnEggDestroyed;
            GameEvents.OnBirdDestroyed          -= OnBirdDestroyed;
            GameEvents.OnCannonHit              -= OnCannonHit;
            GameEvents.OnPlayerDeath            -= OnPlayerDeath;
            GameEvents.OnBossPhase2             -= OnBossPhase2;
            GameEvents.OnBossDefeated           -= OnBossDefeated;

            GameEvents.OnLevelCompleted         -= OnLevelCompleted;
            GameEvents.OnChapterCompleted       -= OnChapterCompleted;
            GameEvents.OnSelfClearStarted       -= OnSelfClearStarted;
            GameEvents.OnSelfClearEnded         -= OnSelfClearEnded;

            GameEvents.OnPowerupCommitted       -= OnReelTap;
            GameEvents.OnPowerupSelected        -= OnPowerupConfirm;
            GameEvents.OnSpinAnimationCompleted -= OnReelsStopped;
        }

        // Combat
        private void OnEggHit(IDamageable _, int __, Vector3 ___)        => Play(eggHitIntensity);
        private void OnEggDestroyed(IDamageable _, int __, Vector3 ___)  => Play(eggDestroyIntensity);
        private void OnBirdDestroyed(IDamageable _, int __, Vector3 ___) => Play(birdDestroyIntensity);
        private void OnCannonHit(int _)                                  => Play(cannonHitIntensity);
        private void OnPlayerDeath()                                     => Play(playerDeathIntensity);
        private void OnBossPhase2(BossBird _)                            => Play(bossPhase2Intensity);
        private void OnBossDefeated(BossBird _)                          => Play(bossDefeatedIntensity);

        // Level flow
        private void OnLevelCompleted(int _)   => Play(levelCompletedIntensity);
        private void OnChapterCompleted(int _) => Play(chapterCompletedIntensity);
        private void OnSelfClearStarted()      => Play(graceStartedIntensity);
        private void OnSelfClearEnded()        => Play(graceEndedIntensity);

        // Slot machine
        private void OnReelTap(PowerupConfig _)         => Play(reelTapIntensity);
        private void OnPowerupConfirm(PowerupConfig _)  => Play(powerupConfirmIntensity);
        private void OnReelsStopped()                   => Play(reelsStoppedIntensity);

        public void Play(Intensity intensity)
        {
            if (!_enabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            _HapticImpact((int)intensity);
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidPulse(intensity);
#endif
        }

        [ContextMenu("Test Light")]  private void TestLight()  => Play(Intensity.Light);
        [ContextMenu("Test Medium")] private void TestMedium() => Play(Intensity.Medium);
        [ContextMenu("Test Heavy")]  private void TestHeavy()  => Play(Intensity.Heavy);

        // ─── Android ───

        private static void InitAndroid()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_androidReady) return;
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    _sdkInt = version.GetStatic<int>("SDK_INT");

                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                _androidReady = _vibrator != null;
            }
            catch
            {
                _androidReady = false;
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void AndroidPulse(Intensity intensity)
        {
            if (!_androidReady) return;

            int duration, amplitude;
            switch (intensity)
            {
                case Intensity.Heavy:  duration = heavyDurationMs;  amplitude = heavyAmplitude;  break;
                case Intensity.Medium: duration = mediumDurationMs; amplitude = mediumAmplitude; break;
                default:               duration = lightDurationMs;  amplitude = lightAmplitude;  break;
            }

            try
            {
                if (_sdkInt >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)duration, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", (long)duration);
                }
            }
            catch { /* device denied or no vibrator */ }
        }
#endif
    }
}
