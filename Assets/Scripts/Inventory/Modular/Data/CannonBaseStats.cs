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
        public int id;
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

        public string CannonIdKey => $"CANNON_{id:00}";

        /// <summary>
        /// Build a fresh StatSheet with only the base values populated.
        /// Upgrade/equip layers add modifiers on top.
        /// </summary>
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
    /// Loads the cannon base-stats from Cloud Save Game Data under the custom
    /// id `cannon_stats`. Each sub-key holds one DTO. Caches DTOs by id.
    /// </summary>
    public sealed class CannonStatsRepository
    {
        private const string CANNON_STATS_KEY = "cannon_stats";

        private static CannonStatsRepository instance;
        public static CannonStatsRepository Instance => instance ??= new CannonStatsRepository();

        private readonly Dictionary<int, CannonBaseStatsDto> _byId = new();
        private CannonBaseStatsDto[] _ordered = Array.Empty<CannonBaseStatsDto>();

        public bool IsLoaded { get; private set; }
        public int Count => _ordered.Length;

        public CannonBaseStatsDto GetById(int id)
            => _byId.GetValueOrDefault(id);

        public IReadOnlyList<CannonBaseStatsDto> All => _ordered;

        public async UniTask LoadAsync()
        {
            if (IsLoaded) return;

            // Game Data → Custom ID "cannon_stats" — one key per cannon, each
            // holding a single CannonBaseStatsDto as JSON.
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

                // Fill blanks if the cloud payload left them out
                if (string.IsNullOrEmpty(dto.name)) dto.name = kvp.Key;

                parsed.Add(dto);
            }

            if (parsed.Count == 0)
            {
                Debug.LogError("[CannonStatsRepository] Parsed zero cannon entries.");
                return;
            }

            // Sort by id for deterministic ordering; assign sequential ids if all came back zero.
            bool allZeroIds = parsed.TrueForAll(d => d.id == 0);
            if (allZeroIds)
            {
                parsed.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                for (int i = 0; i < parsed.Count; i++) parsed[i].id = i;
            }
            else
            {
                parsed.Sort((a, b) => a.id.CompareTo(b.id));
            }

            _ordered = parsed.ToArray();
            _byId.Clear();
            foreach (var dto in _ordered)
                _byId[dto.id] = dto;

            IsLoaded = true;
            Debug.Log($"[CannonStatsRepository] Loaded {_ordered.Length} cannon(s) from cloud.");
        }

        private static CannonBaseStatsDto ParseOne(Item item)
        {
            // Each Game Data value is a single DTO (not an array/wrapper).
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
