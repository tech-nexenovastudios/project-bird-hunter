using UnityEngine;
using Gameplay.Events;
using Gameplay.Interfaces;

namespace FX
{
    public class HitFXController : MonoBehaviour
    {
        [Header("Particles (optional)")]
        [SerializeField] private GameObject hitParticlePrefab;
        [SerializeField] private GameObject eggDestroyParticlePrefab;
        [SerializeField] private GameObject birdDestroyParticlePrefab;

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
            // Stub: Spawn hit particle at hitPoint when prefab assigned
            if (hitParticlePrefab != null)
            {
                var go = Instantiate(hitParticlePrefab, hitPoint, Quaternion.identity);
                Destroy(go, 3f);
            }
        }

        private void OnEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position)
        {
            if (eggDestroyParticlePrefab != null)
            {
                var go = Instantiate(eggDestroyParticlePrefab, position, Quaternion.identity);
                Destroy(go, 3f);
            }
        }

        private void OnBirdHit(IDamageable bird, int damage, Vector3 hitPoint)
        {
            if (hitParticlePrefab != null)
            {
                var go = Instantiate(hitParticlePrefab, hitPoint, Quaternion.identity);
                Destroy(go, 3f);
            }
        }

        private void OnBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position)
        {
            if (birdDestroyParticlePrefab != null)
            {
                var go = Instantiate(birdDestroyParticlePrefab, position, Quaternion.identity);
                Destroy(go, 3f);
            }
        }
    }
}
