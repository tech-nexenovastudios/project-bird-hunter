using System;
using System.Collections.Generic;
using BirdHunter.Inventory.Stats;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace BirdHunter.Inventory.Data
{
    [Serializable]
    public class CannonBaseStatsDto
    {
        public string name;
        public string description;
        public int unlockAtChapter;
        public int maxUpgradeLevel = 10;

        public float baseDamage;
        public float baseHealth;
        public float baseFireRate;
        public float baseBulletSpeed;
        public float baseCannonSpeed;

        public int baseCoinsRequired;
        public int baseGemsRequired;

        public StatSheet BuildStatSheet()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Damage, baseDamage);
            sheet.SetBase(StatType.Health, baseHealth);
            sheet.SetBase(StatType.FireRate, baseFireRate);
            sheet.SetBase(StatType.BulletSpeed, baseBulletSpeed);
            sheet.SetBase(StatType.MoveSpeed, baseCannonSpeed);
            sheet.SetBase(StatType.MinBulletsPerShot, 1);
            sheet.SetBase(StatType.MaxBulletsPerShot, 1);
            sheet.SetBase(StatType.BulletBounce, 0);
            return sheet;
        }
    }

    /// <summary>
    /// Loads cannon base-stats from Cloud Save Game Data under the custom
    /// id `cannon_stats`. Each sub-key holds one DTO. Caches DTOs by name.
    /// </summary>
    public sealed class CannonStatsRepository
    {
        private const string CANNON_STATS_KEY = "cannon_stats";

        private static CannonStatsRepository instance;
        public static CannonStatsRepository Instance => instance ??= new CannonStatsRepository();

        private readonly Dictionary<string, CannonBaseStatsDto> _byKey = new();
        private CannonBaseStatsDto[] _ordered = Array.Empty<CannonBaseStatsDto>();

        public bool IsLoaded { get; private set; }
        public int Count => _ordered.Length;

        public CannonBaseStatsDto GetByKey(string key)
            => _byKey.GetValueOrDefault(key);

        public IReadOnlyList<CannonBaseStatsDto> All => _ordered;

        public async UniTask LoadAsync()
        {
            if (IsLoaded) return;

            var loaded = await CloudSaveManager.Instance.LoadCustomAllAsync(CANNON_STATS_KEY);

            if (loaded == null || loaded.Count == 0)
            {
                Debug.LogError(
                    $"[CannonStatsRepository] No entries found under custom id '{CANNON_STATS_KEY}'. " +
                    "Check Cloud Save → Game Data in the dashboard.");
                return;
            }

            var parsed = new List<CannonBaseStatsDto>();
            foreach (var kvp in loaded)
            {
                var dto = ParseOne(kvp.Value);
                if (dto == null)
                {
                    Debug.LogWarning($"[CannonStatsRepository] Skipped unparseable entry '{kvp.Key}'.");
                    continue;
                }

                if (string.IsNullOrEmpty(dto.name)) dto.name = kvp.Key;

                parsed.Add(dto);
            }

            if (parsed.Count == 0)
            {
                Debug.LogError("[CannonStatsRepository] Parsed zero cannon entries.");
                return;
            }

            parsed.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            _ordered = parsed.ToArray();
            _byKey.Clear();
            foreach (var dto in _ordered)
                _byKey[dto.name] = dto;

            IsLoaded = true;
            Debug.Log($"[CannonStatsRepository] Loaded {_ordered.Length} cannon(s) from cloud.");
        }

        private static CannonBaseStatsDto ParseOne(Item item)
        {
            try
            {
                string raw = item.Value.GetAsString();
                if (!string.IsNullOrEmpty(raw))
                    return JsonConvert.DeserializeObject<CannonBaseStatsDto>(raw);
            }
            catch { /* fall through */ }

            try { return item.Value.GetAs<CannonBaseStatsDto>(); }
            catch { /* fall through */ }

            return null;
        }
    }
}
