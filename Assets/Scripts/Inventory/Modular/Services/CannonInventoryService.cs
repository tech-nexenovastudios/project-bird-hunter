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
    /// Owns cannon inventory state: per-cannon level, unlock flag, equipped key.
    /// Costs come from EconomyFormulaConfig; base stats from CannonStatsRepository.
    /// UI never touches CloudSave / CurrencyManager directly — it goes through this.
    /// </summary>
    public sealed class CannonInventoryService : MonoBehaviour

    {
        public static CannonInventoryService Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private EconomyFormulaConfig economy;

        [Header("Progression Curve — see docs/Cannon_DPS_and_Egg_Workload_Design.md §3")]
        [Tooltip("Damage growth per level (geometric). target = round(base * g^(L-1)), floored at +1/level so every upgrade is a distinct integer. 1.06 ≈ 320× at L100.")]
        [SerializeField] private float damageGrowthPerLevel = 1.06f;
        [Tooltip("Fire-rate ceiling as a fraction over base (0.5 = +50% max). Front-loaded & saturating, so bullet count can't blow up at high levels.")]
        [SerializeField] private float fireRateCap = 0.5f;
        [Tooltip("Fire-rate saturation decay; ~0.924 reaches ~90% of the cap by L30, then plateaus.")]
        [SerializeField] private float fireRateDecay = 0.924f;
        [Tooltip("Health % per upgrade level, linear PctAdd. Level N adds (N-1) * this.")]
        [SerializeField] private float healthPerLevelPct = 0.10f;
        [Tooltip("Move speed % per upgrade level, linear PctAdd.")]
        [SerializeField] private float moveSpeedPerLevelPct = 0.02f;

        [Header("Defaults")]
        [SerializeField] private string[] defaultUnlockedKeys = { "SingleShotCannon" };
        [SerializeField] private bool devUnlockAll = false;

        // ── Persistence keys ────────────────────────────────────────────────
        private const string LEVEL_KEY_PREFIX   = "cannon_level_";
        private const string UNLOCK_KEY_PREFIX  = "cannon_unlocked_";
        private const string EQUIPPED_KEY       = "cannon_equipped_id";

        // ── State, keyed by cannon name/slug ───────────────────────────────
        private readonly Dictionary<string, int>       _levels   = new();
        private readonly Dictionary<string, bool>      _unlocked = new();
        private readonly Dictionary<string, StatSheet> _sheets   = new();
        private string _equippedKey;

        private CancellationToken _destroyCT;

        // ── Events ──────────────────────────────────────────────────────────
        public event Action              OnReady;
        public event Action<string>      OnUnlocked;
        public event Action<string>      OnUpgraded;
        public event Action<string>      OnEquipped;

        public bool   IsReady    { get; private set; }
        public int    Count      => CannonStatsRepository.Instance.Count;
        public string EquippedKey => _equippedKey;
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
            // Survive MainMenu→GamePlayScene transitions so in-memory upgrade levels and
            // the equipped key stay live. Without this, the service is destroyed on scene
            // unload and the gameplay scene's duplicate (or a re-init from cloud) replaces
            // it — racing the fire-and-forget SaveLevelAsync writes from the upgrade UI.
            // GameObject must be at scene root for DontDestroyOnLoad to take effect.
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            else
                Debug.LogWarning(
                    "[CannonInventoryService] Cannot DontDestroyOnLoad — GameObject is " +
                    "parented. Move it to the scene root, or upgrades won't persist between " +
                    "MainMenu and gameplay scenes.");
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
                OnReady    = null;
                OnUnlocked = null;
                OnUpgraded = null;
                OnEquipped = null;
                Instance   = null;
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

            foreach (var dto in repo.All)
            {
                _levels[dto.name]   = 1;
                _unlocked[dto.name] = false;
                _sheets[dto.name]   = dto.BuildStatSheet();
            }

            if (!devUnlockAll && defaultUnlockedKeys != null)
                foreach (string key in defaultUnlockedKeys)
                    if (_unlocked.ContainsKey(key))
                        _unlocked[key] = true;

            if (devUnlockAll)
                foreach (var dto in repo.All)
                    _unlocked[dto.name] = true;

            await LoadCloudStateAsync();

            foreach (var dto in repo.All)
                ApplyUpgradeModifier(dto.name);

            IsReady = true;
            OnReady?.Invoke();
            OnEquipped?.Invoke(_equippedKey);
        }

        private async UniTask LoadCloudStateAsync()
        {
            var keys = new HashSet<string> { EQUIPPED_KEY };
            foreach (var dto in CannonStatsRepository.Instance.All)
            {
                keys.Add(LEVEL_KEY_PREFIX  + dto.name);
                keys.Add(UNLOCK_KEY_PREFIX + dto.name);
            }

            try
            {
                var data = await CloudSaveManager.Instance.LoadAsync(keys);

                foreach (var dto in CannonStatsRepository.Instance.All)
                {
                    string lk = LEVEL_KEY_PREFIX + dto.name;
                    if (data.TryGetValue(lk, out var lItem))
                        _levels[dto.name] = Mathf.Clamp(lItem.Value.GetAs<int>(), 1, dto.maxUpgradeLevel);

                    if (!devUnlockAll)
                    {
                        string uk = UNLOCK_KEY_PREFIX + dto.name;
                        if (data.TryGetValue(uk, out var uItem))
                        {
                            bool cloudUnlocked = uItem.Value.GetAs<bool>();
                            _unlocked[dto.name] = cloudUnlocked;
                            if (cloudUnlocked)
                                Debug.Log($"[CannonInventoryService] Cloud override: {dto.name} is UNLOCKED (from saved '{uk}').");
                        }
                    }
                }

                if (data.TryGetValue(EQUIPPED_KEY, out var eItem))
                    _equippedKey = eItem.Value.GetAs<string>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CannonInventoryService] Cloud load failed: {ex.Message}");
            }

            // Fallback: equip first unlocked cannon
            if (string.IsNullOrEmpty(_equippedKey)
                || !_unlocked.TryGetValue(_equippedKey, out var equippedUnlocked)
                || !equippedUnlocked)
            {
                _equippedKey = FirstUnlockedKey();
            }
        }

        private string FirstUnlockedKey()
        {
            foreach (var dto in CannonStatsRepository.Instance.All)
                if (_unlocked.TryGetValue(dto.name, out var u) && u)
                    return dto.name;
            return CannonStatsRepository.Instance.All.Count > 0
                ? CannonStatsRepository.Instance.All[0].name
                : null;
        }

        // ════════════════════════════════════════════════════════════════════
        // Queries
        // ════════════════════════════════════════════════════════════════════

        public bool IsUnlocked(string key)  => _unlocked.TryGetValue(key, out var u) && u;
        public int  GetLevel(string key)     => _levels.GetValueOrDefault(key, 0);
        public bool IsEquipped(string key)   => _equippedKey == key;

        public CannonBaseStatsDto GetBaseData(string key)
            => CannonStatsRepository.Instance.GetByKey(key);

        public StatSheet GetStatSheet(string key)
            => _sheets.GetValueOrDefault(key);

        public int GetMaxLevel(string key)
        {
            var dto = GetBaseData(key);
            return dto?.maxUpgradeLevel ?? 1;
        }

        public bool IsMaxLevel(string key) => GetLevel(key) >= GetMaxLevel(key);

        // ── DPS-preview helpers (inventory UI) ──────────────────────────────
        // Compute the upgraded stat a cannon *would* have at an arbitrary level,
        // using the same curves as ApplyUpgradeModifier. Lets the UI preview the
        // next level's DPS without mutating the live StatSheet.
        public int GetDamageAtLevel(string key, int level)
        {
            var dto = GetBaseData(key);
            return dto == null ? 0 : ComputeUpgradedDamage(dto.baseDamage, Mathf.Max(1, level));
        }

        public float GetFireRateAtLevel(string key, int level)
        {
            var dto = GetBaseData(key);
            if (dto == null) return 0f;
            float bonus = fireRateCap * (1f - Mathf.Pow(fireRateDecay, Mathf.Max(0, level - 1)));
            return dto.baseFireRate * (1f + bonus);
        }

        // ── Costs ───────────────────────────────────────────────────────────
        // Unlock costs come straight from cloud save (CannonBaseStatsDto). The
        // EconomyFormulaConfig formula is an editor-side calculator only — see
        // docs/cloud-save/cannon_stats.json for the values shipped to cloud.
        // Upgrade costs still use the formula because they share a curve shape.

        public int GetUnlockCoinCost(string key)
        {
            var dto = GetBaseData(key);
            return dto?.baseCoinsRequired ?? 0;
        }

        public int GetUnlockGemCost(string key)
        {
            var dto = GetBaseData(key);
            return dto?.baseGemsRequired ?? 0;
        }

        public int GetUpgradeCoinCost(string key)
        {
            var dto = GetBaseData(key);
            if (dto == null) return 0;
            int toLevel = GetLevel(key) + 1;
            if (toLevel > dto.maxUpgradeLevel) return 0;
            if (economy != null)
                return economy.GetUpgradeCostCoins(dto.cannonId, toLevel);
            return 0;
        }

        public int GetUpgradeMaterialCost(string key)
        {
            var dto = GetBaseData(key);
            if (dto == null || economy == null) return 0;
            int toLevel = GetLevel(key) + 1;
            if (toLevel > dto.maxUpgradeLevel) return 0;
            return economy.GetUpgradeMaterialsRequired(dto.cannonId, toLevel);
        }

        // ════════════════════════════════════════════════════════════════════
        // Mutations
        // ════════════════════════════════════════════════════════════════════

        public async UniTask<bool> TryUnlock(string key)
        {
            if (!IsReady) return false;
            if (IsUnlocked(key)) return false;

            int coinCost = GetUnlockCoinCost(key);
            int gemCost  = GetUnlockGemCost(key);

            if (coinCost > 0 || gemCost > 0)
            {
                bool spent = await CurrencyManager.Instance.SpendMultiple(
                    (CurrencyType.Gold, coinCost),
                    (CurrencyType.Gems, gemCost));
                if (!spent) return false;
            }

            _unlocked[key] = true;
            await SaveUnlockAsync(key);
            OnUnlocked?.Invoke(key);
            return true;
        }

        public async UniTask<bool> TryUpgrade(string key)
        {
            if (!IsReady || !IsUnlocked(key) || IsMaxLevel(key)) return false;

            int cost = GetUpgradeCoinCost(key);
            if (cost > 0)
            {
                bool spent = await CurrencyManager.Instance.SpendGold(cost);
                if (!spent) return false;
            }

            _levels[key] = GetLevel(key) + 1;
            ApplyUpgradeModifier(key);
            await SaveLevelAsync(key);

            OnUpgraded?.Invoke(key);
            return true;
        }

        public async UniTask<bool> Equip(string key)
        {
            if (!IsReady || !IsUnlocked(key)) return false;
            if (_equippedKey == key) return true;

            _equippedKey = key;
            await SaveEquippedAsync();
            OnEquipped?.Invoke(key);
            return true;
        }

        public void ForceUnlock(string key)
        {
            if (!_unlocked.ContainsKey(key) || _unlocked[key]) return;
            _unlocked[key] = true;
            SaveUnlockAsync(key).Forget();
            OnUnlocked?.Invoke(key);
        }

        // ════════════════════════════════════════════════════════════════════
        // Upgrade modifier — removed + reapplied on each level change
        // ════════════════════════════════════════════════════════════════════

        private static readonly object UpgradeSource = new { tag = "Upgrade" };

        private void ApplyUpgradeModifier(string key)
        {
            if (!_sheets.TryGetValue(key, out var sheet)) return;

            sheet.RemoveBySource(UpgradeSource);

            int level = GetLevel(key);
            if (level <= 1) return;

            int steps = level - 1;

            float baseDmg = sheet.GetBase(StatType.Damage);
            int targetDmg = ComputeUpgradedDamage(baseDmg, level);
            sheet.Add(new StatModifier(StatType.Damage, targetDmg - baseDmg, StatModOp.Flat, UpgradeSource));

            float frBonus = fireRateCap * (1f - Mathf.Pow(fireRateDecay, steps));
            sheet.Add(new StatModifier(StatType.FireRate, frBonus, StatModOp.PctAdd, UpgradeSource));

            sheet.Add(new StatModifier(StatType.Health,    steps * healthPerLevelPct,    StatModOp.PctAdd, UpgradeSource));
            sheet.Add(new StatModifier(StatType.MoveSpeed, steps * moveSpeedPerLevelPct, StatModOp.PctAdd, UpgradeSource));
        }

        private int ComputeUpgradedDamage(float baseDmg, int level)
        {
            int dmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg));
            for (int L = 2; L <= level; L++)
            {
                int geo = Mathf.RoundToInt(baseDmg * Mathf.Pow(damageGrowthPerLevel, L - 1));
                dmg = Mathf.Max(dmg + 1, geo);
            }
            return dmg;
        }

        // ════════════════════════════════════════════════════════════════════
        // Persistence
        // ════════════════════════════════════════════════════════════════════

        private async UniTask SaveLevelAsync(string key)
        {
            try { await CloudSaveManager.Instance.SaveValueAsync(LEVEL_KEY_PREFIX + key, _levels[key]); }
            catch (Exception ex) { Debug.LogError($"[CannonInventoryService] SaveLevel failed: {ex.Message}"); }
        }

        private async UniTask SaveUnlockAsync(string key)
        {
            try { await CloudSaveManager.Instance.SaveValueAsync(UNLOCK_KEY_PREFIX + key, true); }
            catch (Exception ex) { Debug.LogError($"[CannonInventoryService] SaveUnlock failed: {ex.Message}"); }
        }

        private async UniTask SaveEquippedAsync()
        {
            try { await CloudSaveManager.Instance.SaveValueAsync(EQUIPPED_KEY, _equippedKey); }
            catch (Exception ex) { Debug.LogError($"[CannonInventoryService] SaveEquipped failed: {ex.Message}"); }
        }

        /// <summary>Fetch the equipped cannon key from cloud (use from gameplay scene).</summary>
        public static UniTask<string> GetEquippedCannonKeyFromCloud()
            => CloudSaveManager.Instance.LoadValueAsync(EQUIPPED_KEY, string.Empty);

        // ════════════════════════════════════════════════════════════════════
        // Dev utilities — right-click the component header in the Inspector
        // ════════════════════════════════════════════════════════════════════

        [ContextMenu("Dev/Clear Cloud Unlock State (all cannons)")]
        private void DevClearAllUnlockStateAsync()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[CannonInventoryService] Enter Play Mode first."); return; }
            ClearAllCloudUnlockStateAsync().Forget();
        }

        private async UniTaskVoid ClearAllCloudUnlockStateAsync()
        {
            if (!CannonStatsRepository.Instance.IsLoaded)
            {
                Debug.LogWarning("[CannonInventoryService] Stats repo not loaded yet.");
                return;
            }

            foreach (var dto in CannonStatsRepository.Instance.All)
            {
                string uk = UNLOCK_KEY_PREFIX + dto.name;
                try { await CloudSaveManager.Instance.SaveValueAsync(uk, false); }
                catch (Exception ex) { Debug.LogError($"[CannonInventoryService] Clear {uk} failed: {ex.Message}"); continue; }

                Debug.Log($"[CannonInventoryService] Cleared cloud data for {dto.name}.");
            }

            Debug.Log("[CannonInventoryService] Cloud state cleared. Restart the scene to re-initialize.");
        }
    }
}
