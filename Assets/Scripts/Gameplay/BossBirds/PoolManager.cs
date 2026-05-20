// PoolManager.cs
// Singleton MonoBehaviour — lives on a GameObject in the scene
// Handles all object pooling game-wide

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    // ---- Singleton ----
    private static PoolManager instance;

    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Auto-create if none exists (e.g. first scene load)
                var go = new GameObject("[PoolManager]");
                instance = go.AddComponent<PoolManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            instance = null;
        }
    }

    // Pooled objects (boss attacks, VFX) live under this DontDestroyOnLoad GameObject,
    // so they survive scene unloads and would render on top of whatever loads next
    // (e.g. boss VFX visible in MainMenu after Game Over -> Home). Clear on unload.
    private void HandleSceneUnloaded(Scene scene)
    {
        ClearAllInternal();
    }

    // ---- Pool Storage ----
    // One pool per prefab, keyed by the prefab's InstanceID (not name — names can collide)
    private readonly Dictionary<int, Pool> pools = new();

    // ---- Static API (what your scripts call) ----

    /// <summary>
    /// Get an object from the pool. If none available, instantiates a new one.
    /// The returned object is active and positioned.
    /// </summary>
    public static GameObject Get(GameObject prefab, Vector3 position)
    {
        return Instance.GetInternal(prefab, position, Quaternion.identity);
    }

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return Instance.GetInternal(prefab, position, rotation);
    }

    /// <summary>
    /// Return an object to the pool. It will be deactivated and stored for reuse.
    /// Safe to call with null — does nothing.
    /// </summary>
    public static void Return(GameObject obj)
    {
        if (obj == null) return;
        Instance.ReturnInternal(obj);
    }

    /// <summary>
    /// Return an object to the pool after a delay. Useful for VFX that need to finish playing.
    /// </summary>
    public static void ReturnDelayed(GameObject obj, float delay)
    {
        if (obj == null) return;
        Instance.StartCoroutine(Instance.ReturnAfterDelay(obj, delay));
    }

    /// <summary>
    /// Pre-warm a pool with a specific number of instances.
    /// Call this during loading screens or boss spawn prep.
    /// </summary>
    public static void Prewarm(GameObject prefab, int count)
    {
        Instance.PrewarmInternal(prefab, count);
    }

    /// <summary>
    /// Destroy all pooled objects for a specific prefab. 
    /// Use when transitioning chapters or unloading content.
    /// </summary>
    public static void ClearPool(GameObject prefab)
    {
        Instance.ClearPoolInternal(prefab);
    }

    /// <summary>
    /// Destroy all pools. Use on major scene transitions.
    /// </summary>
    public static void ClearAll()
    {
        Instance.ClearAllInternal();
    }

    // ---- Internal Implementation ----

    private GameObject GetInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int key = prefab.GetInstanceID();

        if (!pools.TryGetValue(key, out var pool))
        {
            pool = new Pool(prefab, transform);
            pools[key] = pool;
        }

        return pool.Get(position, rotation);
    }

    private void ReturnInternal(GameObject obj)
    {
        // Find which pool this object belongs to via the tracker component
        if (obj.TryGetComponent<PoolTracker>(out var tracker))
        {
            int key = tracker.PrefabID;
            if (pools.TryGetValue(key, out var pool))
            {
                pool.Return(obj);
                return;
            }
        }

        // If not tracked (wasn't created by pool), just destroy it
        // This handles edge cases gracefully instead of crashing
        Debug.LogWarning($"[PoolManager] Returning untracked object '{obj.name}'. Destroying instead.");
        Destroy(obj);
    }

    private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnInternal(obj);
    }

    private void PrewarmInternal(GameObject prefab, int count)
    {
        int key = prefab.GetInstanceID();

        if (!pools.TryGetValue(key, out var pool))
        {
            pool = new Pool(prefab, transform);
            pools[key] = pool;
        }

        pool.Prewarm(count);
    }

    private void ClearPoolInternal(GameObject prefab)
    {
        int key = prefab.GetInstanceID();
        if (pools.TryGetValue(key, out var pool))
        {
            pool.DestroyAll();
            pools.Remove(key);
        }
    }

    private void ClearAllInternal()
    {
        foreach (var pool in pools.Values)
            pool.DestroyAll();
        pools.Clear();
    }

    // ---- Pool Class (one per prefab type) ----

    private class Pool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly int prefabID;
        private readonly Queue<GameObject> inactive = new();

        // Track active count for diagnostics (visible in custom editor if you build one)
        private int activeCount;
        public int ActiveCount => activeCount;
        public int InactiveCount => inactive.Count;
        public int TotalCount => activeCount + inactive.Count;

        public Pool(GameObject prefab, Transform managerTransform)
        {
            this.prefab = prefab;
            this.prefabID = prefab.GetInstanceID();

            // Create a container child under PoolManager for organization
            var container = new GameObject($"Pool [{prefab.name}]");
            container.transform.SetParent(managerTransform);
            this.parent = container.transform;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            if (inactive.Count > 0)
            {
                obj = inactive.Dequeue();

                // Handle edge case: pooled object was destroyed externally
                if (obj == null)
                    return CreateNew(position, rotation);

                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }
            else
            {
                obj = CreateNew(position, rotation);
            }

            activeCount++;

            // Notify poolable components that they've been reused
            NotifySpawned(obj);

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            // Notify poolable components before deactivation
            NotifyDespawned(obj);

            obj.SetActive(false);
            obj.transform.SetParent(parent);
            inactive.Enqueue(obj);
            activeCount = Mathf.Max(0, activeCount - 1);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (inactive.Count >= count) break;

                var obj = CreateNew(Vector3.zero, Quaternion.identity);
                obj.SetActive(false);
                inactive.Enqueue(obj);
            }
        }

        public void DestroyAll()
        {
            while (inactive.Count > 0)
            {
                var obj = inactive.Dequeue();
                if (obj != null)
                    Object.Destroy(obj);
            }

            if (parent != null)
                Object.Destroy(parent.gameObject);

            activeCount = 0;
        }

        private GameObject CreateNew(Vector3 position, Quaternion rotation)
        {
            var obj = Object.Instantiate(prefab, position, rotation, parent);
            obj.name = prefab.name; // Remove "(Clone)" suffix for clean hierarchy

            // Attach tracker so Return() can find the right pool
            var tracker = obj.AddComponent<PoolTracker>();
            tracker.PrefabID = prefabID;

            return obj;
        }

        private static void NotifySpawned(GameObject obj)
        {
            // Cache-friendly: GetComponents with NonAlloc isn't worth it here
            // because spawns are infrequent (not per-frame)
            var poolables = obj.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnPoolSpawned();
        }

        private static void NotifyDespawned(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnPoolDespawned();
        }
    }
}