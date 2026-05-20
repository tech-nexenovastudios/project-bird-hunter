using UnityEngine;
using UnityEngine.Events;

namespace PitySystem
{
    /// <summary>
    /// Scene-friendly wrapper around PityRoller. Drop this on a GameObject,
    /// assign a PityConfig, and call Pull() from a button or game code.
    /// </summary>
    public class LootBox : MonoBehaviour
    {
        [SerializeField] PityConfig config;
        [SerializeField, Tooltip("Use a fixed seed for reproducible runs. -1 = random.")]
        int seed = -1;

        [System.Serializable] public class PullEvent : UnityEvent<PullResult> {}
        public PullEvent onPull;

        PityRoller _roller;

        public int PullsSinceLast => _roller != null ? _roller.PullsSinceLast : 0;
        public int TotalPulls => _roller != null ? _roller.TotalPulls : 0;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[LootBox] No PityConfig assigned.", this);
                return;
            }
            _roller = new PityRoller(config, seed >= 0 ? seed : (int?)null);
        }

        /// <summary>Perform one pull, fire the event, and return the result.</summary>
        public PullResult Pull()
        {
            var res = _roller.Pull();
            onPull?.Invoke(res);
            return res;
        }

        /// <summary>Pull n times, returning every result.</summary>
        public PullResult[] PullMany(int n)
        {
            var results = new PullResult[Mathf.Max(0, n)];
            for (int i = 0; i < results.Length; i++) results[i] = Pull();
            return results;
        }
    }
}
