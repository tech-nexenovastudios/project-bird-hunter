using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.VFX
{
    /// <summary>
    /// Centralized VFX pool. Any script can request a VFX to play at a position.
    /// Pre-creates instances per prefab type, recycles them automatically.
    /// 
    /// Usage:
    ///   VFXPoolManager.Instance.Play(blastPrefab, transform. position);
    ///   
    /// That's it. No Instantiate, no Destroy, no caching, no cleanup.
    /// Place one GameObject in your scene with this component.
    /// </summary>
    public class VFXPoolManager : MonoBehaviour
    {
        public static VFXPoolManager Instance { get; private set; }

        [Tooltip("How many instances to pre-create per prefab type on first request")]
        [SerializeField] private int defaultPoolSize = 5;

        // Each prefab gets its own pool of pre-created instances
        private readonly Dictionary<GameObject, Queue<ParticleSystem>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Play a VFX prefab at a world position. Returns the ParticleSystem
        /// in case the caller needs to stop it early (optional).
        /// The instance auto-returns to the pool after particles finish.
        /// </summary>
        public ParticleSystem Play(GameObject prefab, Vector3 position, Quaternion? rotation = null)
        {
            if (prefab == null) return null;

            ParticleSystem ps = GetFromPool(prefab);
            if (ps == null) return null;

            Transform t = ps.transform;
            t.SetParent(null);
            t.position = position;
            t.rotation = rotation ?? Quaternion.identity;

            ps.gameObject.SetActive(true);
            ps.Play(true);

            float totalTime = GetTotalDuration(ps);
            StartCoroutine(ReturnAfterDelay(prefab, ps, totalTime));

            return ps;
        }

        /// <summary>
        /// Play a VFX and parent it to a transform so it moves with it.
        /// Used for muzzle flash, auras, or anything that tracks a moving object.
        /// Returns the ParticleSystem in case the caller needs to stop it early.
        /// </summary>
        public ParticleSystem PlayAttached(GameObject prefab, Transform parent)
        {
            if (prefab == null || parent == null) return null;

            ParticleSystem ps = GetFromPool(prefab);
            if (ps == null) return null;

            Transform t = ps.transform;
            t.SetParent(parent);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            ps.gameObject.SetActive(true);
            ps.Play(true);

            float totalTime = GetTotalDuration(ps);
            StartCoroutine(ReturnAfterDelay(prefab, ps, totalTime));

            return ps;
        }

        /// <summary>
        /// Manually stop and return a VFX early. Not required — VFX auto-returns
        /// after its duration. Use this when you need to cut an effect short
        /// (e.g., stopping muzzle flash during recoil recovery).
        /// </summary>
        public void StopAndReturn(GameObject prefab, ParticleSystem instance)
        {
            if (instance == null || prefab == null) return;

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ReturnToPool(prefab, instance);
        }

        // ════════════════════════════════════════════════════════
        //  POOL INTERNALS
        // ════════════════════════════════════════════════════════

        private ParticleSystem GetFromPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                _pools[prefab] = pool;
                PreWarm(prefab, pool);
            }

            // Try to get an inactive instance
            while (pool.Count > 0)
            {
                ParticleSystem ps = pool.Dequeue();
                if (ps != null) return ps;
            }

            // Pool empty — all instances in use, create one more
            return CreateInstance(prefab);
        }

        private void ReturnToPool(GameObject prefab, ParticleSystem ps)
        {
            if (ps == null) return;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.SetParent(transform);
            ps.gameObject.SetActive(false);

            if (_pools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
            {
                pool.Enqueue(ps);
            }
        }

        private IEnumerator ReturnAfterDelay(GameObject prefab, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Only return if still active (might have been manually returned via StopAndReturn)
            if (ps != null && ps.gameObject.activeSelf)
            {
                ReturnToPool(prefab, ps);
            }
        }

        private void PreWarm(GameObject prefab, Queue<ParticleSystem> pool)
        {
            for (int i = 0; i < defaultPoolSize; i++)
            {
                ParticleSystem ps = CreateInstance(prefab);
                if (ps != null)
                {
                    ps.gameObject.SetActive(false);
                    pool.Enqueue(ps);
                }
            }
        }

        private ParticleSystem CreateInstance(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, transform);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            if (ps == null)
            {
                Destroy(go);
                return null;
            }

            go.SetActive(false);
            return ps;
        }

        private static float GetTotalDuration(ParticleSystem ps)
        {
            var main = ps.main;
            return main.duration + main.startLifetime.constantMax;
        }
    }
}