using System;
using System.Collections.Generic;
using Gameplay.Birds;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using UnityEngine;

namespace Audio
{
    public class SFXController : MonoBehaviour
    {
        [Serializable]
        private struct SfxClip
        {
            public AudioClip clip;
            [Range(-3f, 3f)] public float pitch;

            public static SfxClip Default => new SfxClip { pitch = 1f };
        }

        [Header("Sounds (optional)")]
        [SerializeField] private SfxClip eggHitSound = SfxClip.Default;
        [SerializeField] private SfxClip eggDestroySound = SfxClip.Default;
        [SerializeField] private SfxClip eggBounceSound = SfxClip.Default;
        [SerializeField] private SfxClip birdDestroySound = SfxClip.Default;
        [SerializeField] private SfxClip cannonHitSound = SfxClip.Default;
        [SerializeField] private SfxClip cannonShootSound = SfxClip.Default;
        [SerializeField] private SfxClip cannonDestroyedSound = SfxClip.Default;
        [SerializeField] private SfxClip gameOverSound = SfxClip.Default;
        [SerializeField] private SfxClip slotSpinStarted = SfxClip.Default;
        [SerializeField] private SfxClip slotSpinCompleted = SfxClip.Default;
        [SerializeField] private SfxClip slotPowerSelected = SfxClip.Default;
        [SerializeField] private SfxClip allEggsClearedSound = SfxClip.Default;
        [SerializeField] private SfxClip selfClearStartedSound = SfxClip.Default;
        [SerializeField] private SfxClip finishNowSound = SfxClip.Default;
        [SerializeField] private SfxClip rewardToastSound = SfxClip.Default;

        [Header("Bird Looping SFX")]
        [Tooltip("Loops on each normal bird while its fly_N Spine animation is playing.")]
        [SerializeField] private SfxClip birdFlapSound = SfxClip.Default;

        [Header("Level Countdown SFX")]
        [Tooltip("Plays at each tick of the countdown (3, 2, 1). Use a short beep / tick clip.")]
        [SerializeField] private SfxClip levelCountdownSound = SfxClip.Default;
        [Tooltip("Plays once on the final 'Go!' beat. Use a brighter / higher-pitched cue to signal the start.")]
        [SerializeField] private SfxClip levelCountdownGoSound = SfxClip.Default;

        [Header("Bounce SFX Throttle")]
        [Tooltip("Minimum time between bounce sounds across all eggs. Prevents audio spam when many eggs land in the same frame.")]
        [SerializeField] private float bounceSoundCooldown = 0.05f;
        private float _lastBounceSoundTime = -10f;

        [Header("Boss Music")]
        [Tooltip("Plays while a boss is on screen; previous music resumes when the boss leaves.")]
        [SerializeField] private AudioClip bossMusic;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource spinLoopSource;
        [SerializeField] private AudioSource flapLoopSource;

        private bool _bossMusicActive;

        // One shared flap loop for every flying bird. We track which birds are currently
        // flapping so duplicate Start/Stop events for the same bird don't desync the count;
        // the loop runs whenever the set is non-empty.
        private readonly HashSet<NormalBird> _flappingBirds = new();

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            if (spinLoopSource == null)
                spinLoopSource = gameObject.AddComponent<AudioSource>();
            spinLoopSource.loop = true;
            spinLoopSource.playOnAwake = false;

            if (flapLoopSource == null)
                flapLoopSource = gameObject.AddComponent<AudioSource>();
            flapLoopSource.loop = true;
            flapLoopSource.playOnAwake = false;

            // SFXController owns its own AudioSources (separate from AudioManager.sfxSource),
            // so the pause-panel slider would have no effect here unless we sync explicitly.
            ApplySfxVolume(AudioManager.Instance != null
                ? AudioManager.Instance.GetSFXVolume()
                : PlayerPrefs.GetFloat("SFXVol", 1f));
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit += OnEggHit;
            GameEvents.OnEggDestroyed += OnEggDestroyed;
            GameEvents.OnEggBounced += OnEggBounced;
            GameEvents.OnBirdHit += OnBirdHit;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
            GameEvents.OnCannonHit += OnCannonHit;
            GameEvents.OnCannonShoot += OnCannonShoot;
            GameEvents.OnCannonDestroyStarted += OnCannonDestroyed;
            GameEvents.OnPlayerDeath += OnGameOver;
            GameEvents.OnSpinStarted += OnSpinStarted;
            GameEvents.OnSpinAnimationCompleted += OnSpinAnimationCompleted;
            GameEvents.OnPowerupCommitted += OnSpinCompleted;
            GameEvents.OnPowerupSelected += OnPowerupSelected;
            GameEvents.OnAllEggsCleared += OnAllEggsCleared;
            GameEvents.OnSelfClearStarted += OnSelfClearStarted;
            GameEvents.OnPlayerFinishedLevel += OnFinishNow;
            GameEvents.OnRewardNotification += OnRewardToast;
            GameEvents.OnLevelCountdownTick += OnLevelCountdownTick;
            GameEvents.OnLevelCountdownGo += OnLevelCountdownGo;
            GameEvents.OnBirdFlapStart += OnBirdFlapStart;
            GameEvents.OnBirdFlapStop += OnBirdFlapStop;

            AudioManager.OnSFXVolumeChanged += ApplySfxVolume;

            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossSpawned += OnBossSpawned;
                BossEventBus.Instance.OnBossDefeated += OnBossDefeated;
                BossEventBus.Instance.OnBossRetreated += OnBossRetreated;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnEggHit;
            GameEvents.OnEggDestroyed -= OnEggDestroyed;
            GameEvents.OnEggBounced -= OnEggBounced;
            GameEvents.OnBirdHit -= OnBirdHit;
            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
            GameEvents.OnCannonHit -= OnCannonHit;
            GameEvents.OnCannonShoot -= OnCannonShoot;
            GameEvents.OnCannonDestroyStarted -= OnCannonDestroyed;
            GameEvents.OnPlayerDeath -= OnGameOver;
            GameEvents.OnSpinStarted -= OnSpinStarted;
            GameEvents.OnSpinAnimationCompleted -= OnSpinAnimationCompleted;
            GameEvents.OnPowerupCommitted -= OnSpinCompleted;
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
            GameEvents.OnAllEggsCleared -= OnAllEggsCleared;
            GameEvents.OnSelfClearStarted -= OnSelfClearStarted;
            GameEvents.OnPlayerFinishedLevel -= OnFinishNow;
            GameEvents.OnRewardNotification -= OnRewardToast;
            GameEvents.OnLevelCountdownTick -= OnLevelCountdownTick;
            GameEvents.OnLevelCountdownGo -= OnLevelCountdownGo;
            GameEvents.OnBirdFlapStart -= OnBirdFlapStart;
            GameEvents.OnBirdFlapStop -= OnBirdFlapStop;

            AudioManager.OnSFXVolumeChanged -= ApplySfxVolume;

            if (BossEventBus.Instance != null)
            {
                BossEventBus.Instance.OnBossSpawned -= OnBossSpawned;
                BossEventBus.Instance.OnBossDefeated -= OnBossDefeated;
                BossEventBus.Instance.OnBossRetreated -= OnBossRetreated;
            }

            // Make sure we leave gameplay music in the right state if we're disabled
            // mid-boss (scene unload, etc.).
            if (_bossMusicActive)
            {
                AudioManager.Instance?.PopMusic();
                _bossMusicActive = false;
            }

            StopSpinLoop();
            StopAllFlapLoops();
        }

        private void OnEggHit(IDamageable e, int d, Vector3 p) => PlaySound(eggHitSound);
        private void OnEggDestroyed(IDamageable e, int s, Vector3 p) => PlaySound(eggDestroySound);
        private void OnBirdHit(IDamageable b, int d, Vector3 p) => PlaySound(eggHitSound);
        private void OnBirdDestroyed(IDamageable b, int s, Vector3 p) => PlaySound(birdDestroySound);
        private void OnCannonHit(int damage) => PlaySound(cannonHitSound);
        private void OnCannonShoot() => PlaySound(cannonShootSound);
        private void OnCannonDestroyed() => PlaySound(cannonDestroyedSound);
        private void OnGameOver() => PlaySound(gameOverSound);
        private void OnSpinStarted(List<PowerupConfig> options) => StartSpinLoop();
        private void OnSpinAnimationCompleted() => StopSpinLoop();
        private void OnSpinCompleted(PowerupConfig config) => PlaySound(slotSpinCompleted);
        private void OnPowerupSelected(PowerupConfig config) => PlaySound(slotPowerSelected);

        private void OnEggBounced(Vector3 _)
        {
            // Global throttle — many eggs landing in the same physics step would otherwise
            // stack into a single jarring click. One bounce sound per cooldown window.
            if (Time.unscaledTime - _lastBounceSoundTime < bounceSoundCooldown) return;
            _lastBounceSoundTime = Time.unscaledTime;
            PlaySound(eggBounceSound);
        }

        private void OnAllEggsCleared()
        {
            PlaySound(allEggsClearedSound);
            AudioManager.Instance?.DuckMusic(1.8f, 0.35f);
        }

        private void OnSelfClearStarted() => PlaySound(selfClearStartedSound);

        private void OnFinishNow()
        {
            PlaySound(finishNowSound);
            AudioManager.Instance?.DuckMusic(2.0f, 0.3f);
        }

        private void OnRewardToast(RewardNotification _) => PlaySound(rewardToastSound);

        private void OnLevelCountdownTick() => PlaySound(levelCountdownSound);
        private void OnLevelCountdownGo() => PlaySound(levelCountdownGoSound);

        // ── Boss music ──────────────────────────────────────────────
        private void OnBossSpawned(string bossName)
        {
            if (bossMusic == null || _bossMusicActive) return;
            AudioManager.Instance?.PushMusic(bossMusic);
            _bossMusicActive = true;
        }

        private void OnBossDefeated(string bossName, int score) => RestoreMusicAfterBoss();
        private void OnBossRetreated(string bossName, float hpNorm) => RestoreMusicAfterBoss();

        private void RestoreMusicAfterBoss()
        {
            if (!_bossMusicActive) return;
            AudioManager.Instance?.PopMusic();
            _bossMusicActive = false;
        }

        private void ApplySfxVolume(float vol)
        {
            float v = Mathf.Clamp01(vol);
            if (audioSource != null) audioSource.volume = v;
            if (spinLoopSource != null) spinLoopSource.volume = v;
            if (flapLoopSource != null) flapLoopSource.volume = v;
        }

        private void OnBirdFlapStart(NormalBird bird)
        {
            if (bird == null || birdFlapSound.clip == null || flapLoopSource == null) return;

            // First flapping bird turns the shared loop on; subsequent ones just join the set.
            bool wasEmpty = _flappingBirds.Count == 0;
            if (!_flappingBirds.Add(bird)) return;

            if (wasEmpty && !flapLoopSource.isPlaying)
            {
                flapLoopSource.clip = birdFlapSound.clip;
                flapLoopSource.pitch = birdFlapSound.pitch == 0f ? 1f : birdFlapSound.pitch;
                flapLoopSource.loop = true;
                flapLoopSource.Play();
            }
        }

        private void OnBirdFlapStop(NormalBird bird)
        {
            if (bird == null) return;
            if (!_flappingBirds.Remove(bird)) return;
            if (_flappingBirds.Count == 0) StopFlapLoop();
        }

        private void StopFlapLoop()
        {
            if (flapLoopSource != null && flapLoopSource.isPlaying)
                flapLoopSource.Stop();
        }

        private void StopAllFlapLoops()
        {
            _flappingBirds.Clear();
            StopFlapLoop();
        }

        private void PlaySound(SfxClip sfx)
        {
            if (sfx.clip == null || audioSource == null)
                return;

            audioSource.pitch = sfx.pitch == 0f ? 1f : sfx.pitch;
            audioSource.PlayOneShot(sfx.clip);
        }

        private void StartSpinLoop()
        {
            if (slotSpinStarted.clip == null || spinLoopSource == null)
                return;

            spinLoopSource.clip = slotSpinStarted.clip;
            spinLoopSource.pitch = slotSpinStarted.pitch == 0f ? 1f : slotSpinStarted.pitch;
            spinLoopSource.loop = true;
            spinLoopSource.Play();
        }

        private void StopSpinLoop()
        {
            if (spinLoopSource != null && spinLoopSource.isPlaying)
                spinLoopSource.Stop();
        }
    }
}
