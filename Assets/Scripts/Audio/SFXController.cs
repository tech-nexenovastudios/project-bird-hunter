using System;
using System.Collections.Generic;
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
        [SerializeField] private SfxClip slotSpinStarted = SfxClip.Default;
        [SerializeField] private SfxClip slotSpinCompleted = SfxClip.Default;
        [SerializeField] private SfxClip slotPowerSelected = SfxClip.Default;
        [SerializeField] private SfxClip allEggsClearedSound = SfxClip.Default;
        [SerializeField] private SfxClip selfClearStartedSound = SfxClip.Default;
        [SerializeField] private SfxClip finishNowSound = SfxClip.Default;
        [SerializeField] private SfxClip rewardToastSound = SfxClip.Default;

        [Header("Bounce SFX Throttle")]
        [Tooltip("Minimum time between bounce sounds across all eggs. Prevents audio spam when many eggs land in the same frame.")]
        [SerializeField] private float bounceSoundCooldown = 0.05f;
        private float _lastBounceSoundTime = -10f;

        [Header("Boss Music")]
        [Tooltip("Plays while a boss is on screen; previous music resumes when the boss leaves.")]
        [SerializeField] private AudioClip bossMusic;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource spinLoopSource;

        private bool _bossMusicActive;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            if (spinLoopSource == null)
                spinLoopSource = gameObject.AddComponent<AudioSource>();
            spinLoopSource.loop = true;
            spinLoopSource.playOnAwake = false;
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
            GameEvents.OnPlayerDeath += OnCannonDestroyed;
            GameEvents.OnSpinStarted += OnSpinStarted;
            GameEvents.OnSpinAnimationCompleted += OnSpinAnimationCompleted;
            GameEvents.OnPowerupCommitted += OnSpinCompleted;
            GameEvents.OnPowerupSelected += OnPowerupSelected;
            GameEvents.OnAllEggsCleared += OnAllEggsCleared;
            GameEvents.OnSelfClearStarted += OnSelfClearStarted;
            GameEvents.OnPlayerFinishedLevel += OnFinishNow;
            GameEvents.OnRewardNotification += OnRewardToast;

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
            GameEvents.OnPlayerDeath -= OnCannonDestroyed;
            GameEvents.OnSpinStarted -= OnSpinStarted;
            GameEvents.OnSpinAnimationCompleted -= OnSpinAnimationCompleted;
            GameEvents.OnPowerupCommitted -= OnSpinCompleted;
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
            GameEvents.OnAllEggsCleared -= OnAllEggsCleared;
            GameEvents.OnSelfClearStarted -= OnSelfClearStarted;
            GameEvents.OnPlayerFinishedLevel -= OnFinishNow;
            GameEvents.OnRewardNotification -= OnRewardToast;

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
        }

        private void OnEggHit(IDamageable e, int d, Vector3 p) => PlaySound(eggHitSound);
        private void OnEggDestroyed(IDamageable e, int s, Vector3 p) => PlaySound(eggDestroySound);
        private void OnBirdHit(IDamageable b, int d, Vector3 p) => PlaySound(eggHitSound);
        private void OnBirdDestroyed(IDamageable b, int s, Vector3 p) => PlaySound(birdDestroySound);
        private void OnCannonHit(int damage) => PlaySound(cannonHitSound);
        private void OnCannonShoot() => PlaySound(cannonShootSound);
        private void OnCannonDestroyed() => PlaySound(cannonDestroyedSound);
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
