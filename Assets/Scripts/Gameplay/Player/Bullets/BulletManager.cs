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
        [SerializeField] private SpreadBullet    spreadBulletPrefab;
        [SerializeField] private ExplosiveBullet explosiveBulletPrefab;
        [SerializeField] private HomingBullet    homingBulletPrefab;
        [SerializeField] private SplitBullet     splitBulletPrefab;
        [SerializeField] private PierceBullet    pierceBulletPrefab;
        [SerializeField] private BounceBullet    bounceBulletPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize = 30;

        // Per-type pool storage
        private readonly Dictionary<Type, object> _pools = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            PrewarmPools();
        }

        private void PrewarmPools()
        {
            GetOrCreatePool<StraightBullet>  (straightBulletPrefab);
            GetOrCreatePool<SpreadBullet>    (spreadBulletPrefab);
            GetOrCreatePool<ExplosiveBullet> (explosiveBulletPrefab);
            GetOrCreatePool<HomingBullet>    (homingBulletPrefab);
            GetOrCreatePool<SplitBullet>     (splitBulletPrefab);
            GetOrCreatePool<PierceBullet>    (pierceBulletPrefab);
            GetOrCreatePool<BounceBullet>    (bounceBulletPrefab);
        }

        public TBullet SpawnBullet<TBullet>(
            BulletConfig config,
            Vector2 position,
            Vector2 direction)
            where TBullet : MonoBehaviour, IBullet
        {
            var pool = GetOrCreatePool<TBullet>(GetPrefab<TBullet>());
            var bullet = pool.Get();

            bullet.transform.position = (Vector3)position;
            bullet.transform.up       = (Vector3)direction;
            bullet.SetReleaseAction(b => pool.Release((TBullet)b));

            if (bullet is IInitializable init)
                init.Initialize(config, direction);

            return bullet;
        }

        // ─────────────────────────────────────────
        // Pool getter / creator
        // ─────────────────────────────────────────
        private ObjectPool<TBullet> GetOrCreatePool<TBullet>(MonoBehaviour prefab = null)
            where TBullet : MonoBehaviour, IBullet
        {
            var type = typeof(TBullet);
            if (_pools.TryGetValue(type, out var existing))
                return (ObjectPool<TBullet>)existing;

            var pool = new ObjectPool<TBullet>(
                createFunc: () =>
                {
                    var go = Instantiate(prefab.gameObject);
                    go.SetActive(false);
                    return go.GetComponent<TBullet>();
                },
                actionOnGet:     b => b.gameObject.SetActive(true),
                actionOnRelease: b =>
                {
                    b.gameObject.SetActive(false);
                    if (b.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        rb.linearVelocity  = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                },
                actionOnDestroy: b => Destroy(b.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _pools[type] = pool;
            return pool;
        }

        // ─────────────────────────────────────────
        // Prefab lookup by type
        // ─────────────────────────────────────────
        private MonoBehaviour GetPrefab<TBullet>() where TBullet : MonoBehaviour
        {
            return typeof(TBullet) switch
            {
                var t when t == typeof(StraightBullet)  => straightBulletPrefab,
                var t when t == typeof(SpreadBullet)    => spreadBulletPrefab,
                var t when t == typeof(ExplosiveBullet) => explosiveBulletPrefab,
                var t when t == typeof(HomingBullet)    => homingBulletPrefab,
                var t when t == typeof(SplitBullet)     => splitBulletPrefab,
                var t when t == typeof(PierceBullet)    => pierceBulletPrefab,
                var t when t == typeof(BounceBullet)    => bounceBulletPrefab,
                _ => throw new Exception($"[BulletManager] No prefab for {typeof(TBullet).Name}")
            };
        }

        private void OnDestroy()
        {
            foreach (var pool in _pools.Values)
                if (pool is IDisposable d) d.Dispose();
            _pools.Clear();
        }
    }
}
