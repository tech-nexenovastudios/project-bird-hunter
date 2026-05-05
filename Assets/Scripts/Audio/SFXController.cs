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
        [SerializeField] private SfxClip birdDestroySound = SfxClip.Default;
        [SerializeField] private SfxClip cannonHitSound = SfxClip.Default;
        [SerializeField] private SfxClip cannonShootSound = SfxClip.Default;
        [SerializeField] private SfxClip cannonDestroyedSound = SfxClip.Default;
        [SerializeField] private SfxClip slotSpinStarted = SfxClip.Default;
        [SerializeField] private SfxClip slotSpinCompleted = SfxClip.Default;
        [SerializeField] private SfxClip slotPowerSelected = SfxClip.Default;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource spinLoopSource;

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
            GameEvents.OnBirdHit += OnBirdHit;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
            GameEvents.OnCannonHit += OnCannonHit;
            GameEvents.OnCannonShoot += OnCannonShoot;
            GameEvents.OnPlayerDeath += OnCannonDestroyed;
            GameEvents.OnSpinStarted += OnSpinStarted;
            GameEvents.OnSpinAnimationCompleted += OnSpinAnimationCompleted;
            GameEvents.OnPowerupCommitted += OnSpinCompleted;
            GameEvents.OnPowerupSelected += OnPowerupSelected;

        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnEggHit;
            GameEvents.OnEggDestroyed -= OnEggDestroyed;
            GameEvents.OnBirdHit -= OnBirdHit;
            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
            GameEvents.OnCannonHit -= OnCannonHit;
            GameEvents.OnCannonShoot -= OnCannonShoot;
            GameEvents.OnPlayerDeath -= OnCannonDestroyed;
            GameEvents.OnSpinStarted -= OnSpinStarted;
            GameEvents.OnSpinAnimationCompleted -= OnSpinAnimationCompleted;
            GameEvents.OnPowerupCommitted -= OnSpinCompleted;
            GameEvents.OnPowerupSelected -= OnPowerupSelected;

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
