using System;
using System.Collections.Generic;
using System.Threading;
using BirdHunter.Inventory.Data;
using BirdHunter.Inventory.Stats;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BirdHunter.Inventory.Services
{
    /// <summary>
    /// Owns cannon inventory state: per-cannon level, unlock flag, equipped id.
    /// Costs come from EconomyFormulaConfig; base stats from CannonStatsRepository.
    /// UI never touches CloudSave / CurrencyManager directly — it goes through this.
    /// </summary>
    public sealed class CannonInventoryService : MonoBehaviour
    {
        public static CannonInventoryService Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private EconomyFormulaConfig economy;

        [Header("Progression Curve")]
        [Tooltip("Percent per upgrade level, applied as PctAdd. Level N adds (N-1) * this.")]
        [SerializeField] private float damagePerLevelPct = 0.10f;
        [SerializeField] private float healthPerLevelPct = 0.10f;
        [SerializeField] private float fireRatePerLevelPct = 0.05f;
        [SerializeField] private float moveSpeedPerLevelPct = 0.02f;

        [Header("Defaults")]
        [SerializeField] private int[] defaultUnlockedIds = { 0 };
        [SerializeField] private bool devUnlockAll = false;

        // ── Persistence keys ────────────────────────────────────────────────
        private const string LEVEL_KEY_PREFIX = "cannon_level_";
        private const string UNLOCK_KEY_PREFIX = "cannon_unlocked_";
        private const string EQUIPPED_KEY = "cannon_equipped_id";

        // ── State, keyed by cannon id ───────────────────────────────────────
        private readonly Dictionary<int, int> _levels = new();
        private readonly Dictionary<int, bool> _unlocked = new();
        private readonly Dictionary<int, StatSheet> _sheets = new();
        private int _equippedId = -1;

        private CancellationToken _destroyCT;

        // ── Events ──────────────────────────────────────────────────────────
        public event Action OnReady;
        public event Action<int> OnUnlocked;
        public event Action<int> OnUpgraded;
        public event Action<int> OnEquipped;

        public bool IsReady { get; private set; }
        public int Count => CannonStatsRepository.Instance.Count;
        public int EquippedId => _equippedId;
        public EconomyFormulaConfig Economy => economy;

        // ════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _destroyCT = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnReady = null;
                OnUnlocked = null;
                OnUpgraded = null;
                OnEquipped = null;
                Instance = null;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Initialization
        // ════════════════════════════════════════════════════════════════════

        private async UniTaskVoid InitializeAsync()
        {
            await CannonStatsRepository.Instance.LoadAsync();

            var repo = CannonStatsRepository.Instance;
            if (!repo.IsLoaded || repo.Count == 0)
            {
                Debug.LogError("[CannonInventoryService] Base stats failed to load. Service inert.");
                return;
            }

            // Build per-cannon state shells
            foreach (var dto in repo.All)
            {
                _levels[dto.id] = 1;
                _unlocked[dto.id] = false;
                _sheets[dto.id] = dto.BuildStatSheet();
            }

            // Apply local defaults
            if (!devUnlockAll && defaultUnlockedIds != null)
                foreach (int id in defaultUnlockedIds)
                    if (_unlocked.ContainsKey(id))
                        _unlocked[id] = true;
            if (devUnlockAll)
                foreach (var dto in repo.All)
                    _unlocked[dto.id] = true;

            // Overlay cloud state
            await LoadCloudStateAsync();

            // Apply upgrade modifiers so sheets are current on ready
            foreach (var dto in repo.All)
                ApplyUpgradeModifier(dto.id);

            IsReady = true;
            OnReady?.Invoke();
            OnEquipped?.Invoke(_equippedId);
        }

        private async UniTask LoadCloudStateAsync()
        {
            var keys = new HashSet<string> { EQUIPPED_KEY };
            foreach (var dto in CannonStatsRepository.Instance.All)
            {
                keys.Add(LEVEL_KEY_PREFIX + dto.id);
                keys.Add(UNLOCK_KEY_PREFIX + dto.id);
            }

            try
            {
                var data = await CloudSaveManager.Instance.LoadAsync(keys);

                foreach (var dto in CannonStatsRepository.Instance.All)
                {
                    string lk = LEVEL_KEY_PREFIX + dto.id;
                    if (data.TryGetValue(lk, out var lItem))
                        _levels[dto.id] = Mathf.Clamp(lItem.Value.GetAs<int>(), 1, dto.maxUpgradeLevel);

                    if (!devUnlockAll)
                    {
                        string uk = UNLOCK_KEY_PREFIX + dto.id;
                        if (data.TryGetValue(uk, out var uItem))
                            _unlocked[dto.id] = uItem.Value.GetAs<bool>();
                    }
                }

                if (data.TryGetValue(EQUIPPED_KEY, out var eItem))
                    _equippedId = eItem.Value.GetAs<int>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CannonInventoryService] Cloud load failed: {ex.Message}");
            }

            // Fallback: equip first unlocked cannon
            if (!_unlocked.ContainsKey(_equippedId) || !_unlocked[_equippedId])
            {
                _equippedId = FirstUnlockedId();
            }
        }

        private int FirstUnlockedId()
        {
            foreach (var dto in CannonStatsRepository.Instance.All)
                if (_unlocked.TryGetValue(dto.id, out var u) && u)
                    return dto.id;
            return CannonStatsRepository.Instance.All.Count > 0
                ? CannonStatsRepository.Instance.All[0].id
                : -1;
        }

        // ════════════════════════════════════════════════════════════════════
        // Queries
        // ════════════════════════════════════════════════════════════════════

        public bool IsUnlocked(int id) => _unlocked.TryGetValue(id, out var u) && u;
        public int GetLevel(int id) => _levels.GetValueOrDefault(id, 0);
        public bool IsEquipped(int id) => _equippedId == id;

        public CannonBaseStatsDto GetBaseData(int id)
            => CannonStatsRepository.Instance.GetById(id);

        public StatSheet GetStatSheet(int id)
            => _sheets.GetValueOrDefault(id);

        public int GetMaxLevel(int id)
        {
            var dto = GetBaseData(id);
            return dto?.maxUpgradeLevel ?? 1;
        }

        public bool IsMaxLevel(int id) => GetLevel(id) >= GetMaxLevel(id);

        // ── Costs (via EconomyFormulaConfig with DTO fallback) ──────────────

        public int GetUnlockCoinCost(int id)
        {
            var dto = GetBaseData(id);
            if (dto == null) return 0;
            if (economy != null)
                return economy.GetCannonUnlockCoins(dto.CannonIdKey, dto.unlockAtChapter);
            return dto.baseCoinsRequired;
        }

        public int GetUnlockGemCost(int id)
        {
            var dto = GetBaseData(id);
            if (dto == null) return 0;
            if (economy != null)
                return economy.GetCannonUnlockGems(GetUnlockCoinCost(id));
            return dto.baseGemsRequired;
        }

        public int GetUpgradeCoinCost(int id)
        {
            var dto = GetBaseData(id);
            if (dto == null) return 0;
            int toLevel = GetLevel(id) + 1;
            if (toLevel > dto.maxUpgradeLevel) return 0;
            if (economy != null)
                return economy.GetUpgradeCostCoins(dto.CannonIdKey, toLevel);
            return 0;
        }

        public int GetUpgradeMaterialCost(int id)
        {
            var dto = GetBaseData(id);
            if (dto == null || economy == null) return 0;
            int toLevel = GetLevel(id) + 1;
            if (toLevel > dto.maxUpgradeLevel) return 0;
            return economy.GetUpgradeMaterialsRequired(dto.CannonIdKey, toLevel);
        }

        // ════════════════════════════════════════════════════════════════════
        // Mutations
        // ════════════════════════════════════════════════════════════════════

        public async UniTask<bool> TryUnlock(int id)
        {
            if (!IsReady) return false;
            if (IsUnlocked(id)) return false;

            int coinCost = GetUnlockCoinCost(id);
            int gemCost = GetUnlockGemCost(id);

            if (coinCost > 0 || gemCost > 0)
            {
                bool spent = await CurrencyManager.Instance.SpendMultiple(
                    (CurrencyType.Gold, coinCost),
                    (CurrencyType.Gems, gemCost));
                if (!spent) return false;
            }

            _unlocked[id] = true;
            SaveUnlockAsync(id).Forget();
            OnUnlocked?.Invoke(id);
            return true;
        }

        public async UniTask<bool> TryUpgrade(int id)
        {
            if (!IsReady || !IsUnlocked(id) || IsMaxLevel(id)) return false;

            int cost = GetUpgradeCoinCost(id);
            if (cost > 0)
            {
                bool spent = await CurrencyManager.Instance.SpendGold(cost);
                if (!spent) return false;
            }

            _levels[id] = GetLevel(id) + 1;
            ApplyUpgradeModifier(id);
            SaveLevelAsync(id).Forget();

            OnUpgraded?.Invoke(id);
            return true;
        }

        public async UniTask<bool> Equip(int id)
        {
            if (!IsReady || !IsUnlocked(id)) return false;
            if (_equippedId == id) return true;

            _equippedId = id;
            SaveEquippedAsync().Forget();
            OnEquipped?.Invoke(id);
            return true;
        }

        public void ForceUnlock(int id)
        {
            if (!_unlocked.ContainsKey(id) || _unlocked[id]) return;
            _unlocked[id] = true;
            SaveUnlockAsync(id).Forget();
            OnUnlocked?.Invoke(id);
        }

        // ════════════════════════════════════════════════════════════════════
        // Upgrade modifier — removed + reapplied on each level change
        // ════════════════════════════════════════════════════════════════════

        private static readonly object UpgradeSource = new { tag = "Upgrade" };

        private void ApplyUpgradeModifier(int id)
        {
            if (!_sheets.TryGetValue(id, out var sheet)) return;

            sheet.RemoveBySource(UpgradeSource);

            int level = GetLevel(id);
            if (level <= 1) return;

            int steps = level - 1;
            sheet.Add(new StatModifier(StatType.Damage,    steps * damagePerLevelPct,    StatModOp.PctAdd, UpgradeSource));
            sheet.Add(new StatModifier(StatType.Health,    steps * healthPerLevelPct,    StatModOp.PctAdd, UpgradeSource));
            sheet.Add(new StatModifier(StatType.FireRate,  steps * fireRatePerLevelPct,  StatModOp.PctAdd, UpgradeSource));
            sheet.Add(new StatModifier(StatType.MoveSpeed, steps * moveSpeedPerLevelPct, StatModOp.PctAdd, UpgradeSource));
        }

        // ════════════════════════════════════════════════════════════════════
        // Persistence
        // ════════════════════════════════════════════════════════════════════

        private async UniTaskVoid SaveLevelAsync(int id)
        {
            try { await CloudSaveManager.Instance.SaveValueAsync(LEVEL_KEY_PREFIX + id, _levels[id]); }
            catch (Exception ex) { Debug.LogError($"[CannonInventoryService] SaveLevel failed: {ex.Message}"); }
        }

        private async UniTaskVoid SaveUnlockAsync(int id)
        {
            try { await CloudSaveManager.Instance.SaveValueAsync(UNLOCK_KEY_PREFIX + id, true); }
            catch (Exception ex) { Debug.LogError($"[CannonInventoryService] SaveUnlock failed: {ex.Message}"); }
        }

        private async UniTaskVoid SaveEquippedAsync()
        {
            try { await CloudSaveManager.Instance.SaveValueAsync(EQUIPPED_KEY, _equippedId); }
            catch (Exception ex) { Debug.LogError($"[CannonInventoryService] SaveEquipped failed: {ex.Message}"); }
        }

        /// <summary>Fetch the equipped cannon id from cloud (use from gameplay scene).</summary>
        public static UniTask<int> GetEquippedCannonIdFromCloud()
            => CloudSaveManager.Instance.LoadValueAsync(EQUIPPED_KEY, 0);
    }
}
