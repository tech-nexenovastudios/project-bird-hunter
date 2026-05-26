using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Gameplay.Pooling
{
    /// <summary>
    /// Static prefab-keyed pool for one-shot ParticleSystem GameObjects.
    /// Replaces the per-bounce / per-death Instantiate+Destroy churn (heavy GC on egg-rich
    /// scenes) with reuse. Pools are created lazily the first time a prefab is spawned.
    /// </summary>
    public static class ParticlePoolManager
    {
        private const int DefaultCapacity = 8;
        private const int MaxCapacity = 64;

        private static readonly Dictionary<GameObject, ObjectPool<GameObject>> Pools = new();
        private static CoroutineHost _host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pools.Clear();
            _host = null;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene) => Clear();

        public static void Clear()
        {
            foreach (var pool in Pools.Values)
                pool.Clear();
            Pools.Clear();
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Transform parent = null)
        {
            if (prefab == null) return null;

            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();
            if (instance == null) return null;

            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;

            // Restart any ParticleSystem(s) on the instance so a reused object plays from frame 0.
            if (instance.TryGetComponent(out ParticleSystem ps))
            {
                ps.Clear(true);
                ps.Play(true);
            }
            return instance;
        }

        /// <summary>Return to pool after `delay` seconds. Safe to call with delay=0 for immediate return.</summary>
        public static void Despawn(GameObject prefab, GameObject instance, float delay = 0f)
        {
            if (prefab == null || instance == null) return;
            if (!Pools.TryGetValue(prefab, out var pool))
            {
                // No pool tracked — fall back to destroy to avoid leaks.
                Object.Destroy(instance);
                return;
            }

            if (delay <= 0f)
            {
                pool.Release(instance);
                return;
            }

            EnsureHost();
            _host.StartCoroutine(DespawnAfter(pool, instance, delay));
        }

        private static ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (Pools.TryGetValue(prefab, out var existing)) return existing;

            var pool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(prefab),
                actionOnGet: go => { if (go != null) go.SetActive(true); },
                actionOnRelease: go => { if (go != null) go.SetActive(false); },
                actionOnDestroy: go => { if (go != null) Object.Destroy(go); },
                collectionCheck: false,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxCapacity);

            Pools[prefab] = pool;
            return pool;
        }

        private static IEnumerator DespawnAfter(ObjectPool<GameObject> pool, GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (instance == null) yield break;
            pool.Release(instance);
        }

        private static void EnsureHost()
        {
            if (_host != null) return;
            var go = new GameObject("[ParticlePoolHost]");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<CoroutineHost>();
        }

        private class CoroutineHost : MonoBehaviour { }
    }
}
