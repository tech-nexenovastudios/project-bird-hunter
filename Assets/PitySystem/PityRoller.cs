using System;
using System.Collections.Generic;
using UnityEngine;

namespace PitySystem
{
    /// <summary>Result of a single pull.</summary>
    public struct PullResult
    {
        public LootItem item;
        public bool hitPity;        // true if this pull produced the pity rarity
        public bool wasHardPity;    // true if guaranteed by hard pity
        public int pullsSinceLast;  // counter value BEFORE this pull
        public float rateUsed;      // effective pity rate on this pull
    }

    /// <summary>
    /// Core pity logic. Pure C# - no Unity lifecycle, fully unit-testable.
    /// Tracks how many pulls since the last pity-rarity drop and rolls
    /// against an escalating rate (soft pity) with a guaranteed cap (hard pity).
    /// </summary>
    public class PityRoller
    {
        readonly PityConfig _config;
        readonly System.Random _rng;

        /// <summary>Pulls accumulated since the last pity-rarity hit.</summary>
        public int PullsSinceLast { get; private set; }

        /// <summary>Total pulls ever performed by this roller.</summary>
        public int TotalPulls { get; private set; }

        public PityRoller(PityConfig config, int? seed = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _config = config;
            _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>Perform one pull and return what dropped.</summary>
        public PullResult Pull()
        {
            float rate = _config.GetRate(PullsSinceLast);
            bool hardPity = (PullsSinceLast + 1) >= _config.hardPity;
            bool hit = _rng.NextDouble() < rate;

            var res = new PullResult
            {
                hitPity = hit,
                wasHardPity = hit && hardPity,
                pullsSinceLast = PullsSinceLast,
                rateUsed = rate
            };

            if (hit)
            {
                res.item = PickItem(_config.pityRarity);
                PullsSinceLast = 0;
            }
            else
            {
                res.item = PickNonPityItem();
                PullsSinceLast++;
            }

            TotalPulls++;
            return res;
        }

        /// <summary>Weighted pick among all items of a given rarity.</summary>
        LootItem PickItem(Rarity rarity)
        {
            float total = 0f;
            foreach (var it in _config.items)
                if (it.rarity == rarity) total += Mathf.Max(0f, it.weight);

            if (total <= 0f) return null;
            float roll = (float)_rng.NextDouble() * total;
            foreach (var it in _config.items)
            {
                if (it.rarity != rarity) continue;
                roll -= Mathf.Max(0f, it.weight);
                if (roll <= 0f) return it;
            }
            return null;
        }

        /// <summary>Weighted pick among everything that is NOT the pity rarity.</summary>
        LootItem PickNonPityItem()
        {
            float total = 0f;
            foreach (var it in _config.items)
                if (it.rarity != _config.pityRarity) total += Mathf.Max(0f, it.weight);

            if (total <= 0f) return null;
            float roll = (float)_rng.NextDouble() * total;
            foreach (var it in _config.items)
            {
                if (it.rarity == _config.pityRarity) continue;
                roll -= Mathf.Max(0f, it.weight);
                if (roll <= 0f) return it;
            }
            return null;
        }

        /// <summary>Reset the pity counter (e.g. on a new banner).</summary>
        public void ResetPity() => PullsSinceLast = 0;

        /// <summary>Restore a saved counter value (load from save file).</summary>
        public void LoadState(int pullsSinceLast, int totalPulls = 0)
        {
            PullsSinceLast = Mathf.Max(0, pullsSinceLast);
            TotalPulls = Mathf.Max(0, totalPulls);
        }
    }
}
