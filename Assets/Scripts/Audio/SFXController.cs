using UnityEngine;
using Gameplay.Events;
using Gameplay.Interfaces;

namespace Audio
{
    public class SFXController : MonoBehaviour
    {
        [Header("Sounds (optional)")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip eggDestroySound;
        [SerializeField] private AudioClip birdDestroySound;
        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit += OnEggHit;
            GameEvents.OnEggDestroyed += OnEggDestroyed;
            GameEvents.OnBirdHit += OnBirdHit;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnEggHit;
            GameEvents.OnEggDestroyed -= OnEggDestroyed;
            GameEvents.OnBirdHit -= OnBirdHit;
            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
        }

        private void OnEggHit(IDamageable egg, int damage, Vector3 hitPoint)
        {
            PlaySound(hitSound);
        }

        private void OnEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position)
        {
            PlaySound(eggDestroySound);
        }

        private void OnBirdHit(IDamageable bird, int damage, Vector3 hitPoint)
        {
            PlaySound(hitSound);
        }

        private void OnBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position)
        {
            PlaySound(birdDestroySound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
