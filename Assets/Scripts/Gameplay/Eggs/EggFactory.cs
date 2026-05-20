using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Eggs
{
    public interface IEggFactory
    {
        Egg Acquire(EggTierConfig tier, Vector3 position, Quaternion rotation);
        void Release(Egg egg);
        void Prewarm(EggTierConfig tier, int count);
        void Clear();
    }

    public sealed class PooledEggFactory : IEggFactory
    {
        private readonly Dictionary<EggTierConfig, Stack<Egg>> _pools = new();
        private readonly Transform _container;

        public PooledEggFactory(Transform container = null)
        {
            _container = container;
        }

        public Egg Acquire(EggTierConfig tier, Vector3 position, Quaternion rotation)
        {
            if (tier == null || tier.eggPrefab == null) return null;

            if (_pools.TryGetValue(tier, out var stack))
            {
                while (stack.Count > 0)
                {
                    Egg pooled = stack.Pop();
                    if (pooled == null) continue;
                    pooled.transform.SetPositionAndRotation(position, rotation);
                    pooled.OnPoolAcquire();
                    return pooled;
                }
            }

            return InstantiateNew(tier, position, rotation);
        }

        public void Release(Egg egg)
        {
            if (egg == null) return;
            EggTierConfig key = egg.config;
            if (key == null)
            {
                Object.Destroy(egg.gameObject);
                return;
            }

            egg.OnPoolRelease();

            if (!_pools.TryGetValue(key, out var stack))
            {
                stack = new Stack<Egg>(32);
                _pools[key] = stack;
            }
            stack.Push(egg);
        }

        public void Prewarm(EggTierConfig tier, int count)
        {
            if (tier == null || tier.eggPrefab == null || count <= 0) return;
            if (!_pools.TryGetValue(tier, out var stack))
            {
                stack = new Stack<Egg>(count);
                _pools[tier] = stack;
            }
            for (int i = 0; i < count; i++)
            {
                Egg egg = InstantiateNew(tier, Vector3.zero, Quaternion.identity);
                if (egg == null) continue;
                egg.gameObject.SetActive(false);
                stack.Push(egg);
            }
        }

        public void Clear()
        {
            foreach (var stack in _pools.Values)
            {
                while (stack.Count > 0)
                {
                    Egg egg = stack.Pop();
                    if (egg != null) Object.Destroy(egg.gameObject);
                }
            }
            _pools.Clear();
        }

        private Egg InstantiateNew(EggTierConfig tier, Vector3 position, Quaternion rotation)
        {
            var go = Object.Instantiate(tier.eggPrefab, position, rotation, _container);
            return go.GetComponent<Egg>();
        }
    }
}
