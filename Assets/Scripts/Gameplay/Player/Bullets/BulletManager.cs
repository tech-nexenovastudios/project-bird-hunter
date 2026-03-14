using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Gameplay.Player;

namespace Gameplay.Managers
{
    public class BulletManager : MonoBehaviour
    {
        public static BulletManager Instance { get; private set; }

        [Header("Bullet Prefabs")]
        [SerializeField] private StraightBullet  straightBulletPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize         = 30;

        private readonly Dictionary<Type, object>      _pools    = new();
        private readonly Dictionary<Type, MonoBehaviour> _prefabs = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            // Register prefabs — adding a new bullet type = one new line here only
            Register(straightBulletPrefab);
        }

        private void Register<TBullet>(TBullet prefab)
            where TBullet : MonoBehaviour, IBullet
        {
            if (prefab == null) return;
            _prefabs[typeof(TBullet)] = prefab;
            CreatePool<TBullet>(prefab);
        }

        // ── Public API ───────────────────────────────────────────────────────
        public TBullet SpawnBullet<TBullet>(BulletConfig config, Vector2 position, Vector2 direction)
            where TBullet : MonoBehaviour, IBullet
        {
            var pool   = GetPool<TBullet>();
            var bullet = pool.Get();                          // SetActive(true) via actionOnGet

            bullet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.forward, direction));
            bullet.Initialize(config, direction, b => pool.Release((TBullet)b));  // ← single source of truth

            return bullet;
        }

        // ── Pool internals ───────────────────────────────────────────────────
        private ObjectPool<TBullet> GetPool<TBullet>()
            where TBullet : MonoBehaviour, IBullet
        {
            if (_pools.TryGetValue(typeof(TBullet), out var existing))
                return (ObjectPool<TBullet>)existing;

            if (!_prefabs.TryGetValue(typeof(TBullet), out var prefab))
                throw new Exception($"[BulletManager] No prefab registered for {typeof(TBullet).Name}");

            return CreatePool<TBullet>((TBullet)prefab);
        }

        private ObjectPool<TBullet> CreatePool<TBullet>(TBullet prefab)
            where TBullet : MonoBehaviour, IBullet
        {
            var pool = new ObjectPool<TBullet>(
                createFunc:      ()  => { var go = Instantiate(prefab.gameObject); go.SetActive(false); return go.GetComponent<TBullet>(); },
                actionOnGet:     b   => b.gameObject.SetActive(true),
                actionOnRelease: b   => b.gameObject.SetActive(false),   // physics cleared inside Deactivate already
                actionOnDestroy: b   => Destroy(b.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize:         maxSize
            );

            _pools[typeof(TBullet)] = pool;
            return pool;
        }

        private void OnDestroy()
        {
            foreach (var pool in _pools.Values)
                if (pool is IDisposable d) d.Dispose();
            _pools.Clear();
        }
    }
}
