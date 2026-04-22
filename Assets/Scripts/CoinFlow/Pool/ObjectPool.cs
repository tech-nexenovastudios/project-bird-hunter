using System;
using System.Collections.Generic;
using UnityEngine;

// ───────────────────────────────────────────────────────────
// PURPOSE: Reusable object pool. Zero garbage collection.
//          Works with ANY Component type (coins, bullets, FX).
//
// USAGE:
//     // Create pool
//     pool = new ObjectPool<CoinEntity>(
//         prefab:       coinPrefab,
//         parent:       transform,
//         initialSize:  20,
//         onGet:        coin => coin.gameObject.SetActive(true),
//         onRelease:    coin => coin.gameObject.SetActive(false)
//     );
//
//     // Borrow an object
//     CoinEntity coin = pool.Get();
//
//     // Return it when done
//     pool.Release(coin);
// ───────────────────────────────────────────────────────────

public class ObjectPoo<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly Action<T> onGet;
    private readonly Action<T> onRelease;
    private readonly Queue<T> available = new Queue<T>();

    /// <summary>How many objects exist in total (active + inactive).</summary>
    public int CountAll { get; private set; }

    /// <summary>How many are currently sitting idle in the pool.</summary>
    public int CountInactive => available.Count;


    public ObjectPoo(
        T prefab,
        Transform parent,
        int initialSize,
        Action<T> onGet = null,
        Action<T> onRelease = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.onGet = onGet;
        this.onRelease = onRelease;

        // Pre-warm: create all objects now so first burst has zero lag.
        for (int i = 0; i < initialSize; i++)
        {
            T instance = CreateNewInstance();
            onRelease?.Invoke(instance);        // start hidden
            available.Enqueue(instance);
        }
    }


    /// <summary>
    /// Borrow an object from the pool. If empty, a new one is created.
    /// </summary>
    public T Get()
    {
        T instance;

        if (available.Count > 0)
        {
            instance = available.Dequeue();
        }
        else
        {
            // Pool exhausted — grow dynamically. This is safe but
            // indicates poolInitialSize in Config could be raised.
            instance = CreateNewInstance();
            Debug.LogWarning(
                $"[ObjectPool<{typeof(T).Name}>] Pool exhausted — " +
                $"grew to {CountAll}. Consider raising initial size.");
        }

        onGet?.Invoke(instance);
        return instance;
    }


    /// <summary>
    /// Return an object to the pool for reuse.
    /// </summary>
    public void Release(T instance)
    {
        onRelease?.Invoke(instance);
        available.Enqueue(instance);
    }


    private T CreateNewInstance()
    {
        // Instantiate without a parent first, then reparent with
        // worldPositionStays:true so the prefab's original scale is preserved.
        T instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.SetParent(parent, worldPositionStays: true);
        CountAll++;
        return instance;
    }
}