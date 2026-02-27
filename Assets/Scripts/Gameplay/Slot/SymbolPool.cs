using UnityEngine;
using UnityEngine.Pool;

namespace Gameplay.Slot
{
    public class SymbolPool : MonoBehaviour
    {
        [SerializeField] private SymbolView symbolPrefab;
        [SerializeField] private int defaultCapacity = 60;  // 3 reels × ~20 symbols
        [SerializeField] private int maxSize = 120;

        public static SymbolPool Instance { get; private set; }

        private IObjectPool<SymbolView> pool;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            pool = new ObjectPool<SymbolView>(
                () =>
                {
                    var inst = Instantiate(symbolPrefab, transform);
                    inst.gameObject.SetActive(false);
                    return inst;
                },
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject),
                true, defaultCapacity, maxSize
            );
        }

        public SymbolView Get() => pool.Get();
        public void Release(SymbolView view) => pool.Release(view);
    }
}