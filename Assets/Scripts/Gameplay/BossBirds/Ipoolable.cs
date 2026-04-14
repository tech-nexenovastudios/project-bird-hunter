// IPoolable.cs
// Optional interface for components that need reset logic when reused from a pool.

public interface IPoolable
{
    /// <summary>
    /// Called when the object is taken from the pool and activated.
    /// Use this to reset state instead of Awake/Start (which only fire once).
    /// </summary>
    void OnPoolSpawned();

    /// <summary>
    /// Called just before the object is deactivated and returned to the pool.
    /// Use this to stop particles, cancel coroutines, reset timers.
    /// </summary>
    void OnPoolDespawned();
}