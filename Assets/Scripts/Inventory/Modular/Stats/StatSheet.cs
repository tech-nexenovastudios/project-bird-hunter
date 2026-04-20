using System.Collections.Generic;

namespace BirdHunter.Inventory.Stats
{
    public enum StatType
    {
        Damage,
        Health,
        FireRate,
        BulletSpeed,
        MoveSpeed,
        MinBulletsPerShot,
        MaxBulletsPerShot,
        BulletBounce
    }

    public enum StatModOp
    {
        Flat,       // base + sum(flat)
        PctAdd,     // (base + flat) * (1 + sum(pctAdd))
        PctMult     // prior * product(1 + pctMult)
    }

    public readonly struct StatModifier
    {
        public readonly StatType Type;
        public readonly float Value;
        public readonly StatModOp Op;
        public readonly object Source;

        public StatModifier(StatType type, float value, StatModOp op, object source)
        {
            Type = type;
            Value = value;
            Op = op;
            Source = source;
        }
    }

    /// <summary>
    /// Holds per-stat bases plus a flat list of modifiers. Values are recomputed on demand
    /// so previews (add mod, read, remove) are free. Never mutate bases for progression —
    /// add a modifier instead so it can be peeled off later.
    /// </summary>
    public sealed class StatSheet
    {
        private readonly Dictionary<StatType, float> _bases = new();
        private readonly List<StatModifier> _mods = new();

        public void SetBase(StatType type, float value) => _bases[type] = value;

        public float GetBase(StatType type)
            => _bases.TryGetValue(type, out var v) ? v : 0f;

        public void Add(StatModifier mod) => _mods.Add(mod);

        public int RemoveBySource(object source)
        {
            if (source == null) return 0;
            int removed = 0;
            for (int i = _mods.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_mods[i].Source, source))
                {
                    _mods.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        public void Clear() => _mods.Clear();

        public float Get(StatType type)
        {
            float flat = 0f;
            float pctAdd = 0f;
            float pctMult = 1f;

            for (int i = 0; i < _mods.Count; i++)
            {
                var m = _mods[i];
                if (m.Type != type) continue;
                switch (m.Op)
                {
                    case StatModOp.Flat:    flat += m.Value; break;
                    case StatModOp.PctAdd:  pctAdd += m.Value; break;
                    case StatModOp.PctMult: pctMult *= 1f + m.Value; break;
                }
            }

            float result = (GetBase(type) + flat) * (1f + pctAdd) * pctMult;
            return result;
        }

        public int GetInt(StatType type) => UnityEngine.Mathf.RoundToInt(Get(type));
    }
}
