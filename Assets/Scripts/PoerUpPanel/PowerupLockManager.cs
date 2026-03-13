using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.PowerUps;

/// <summary>
/// Central singleton that owns every PowerUP card's lock / unlock state.
///
/// Unlock priority (highest wins):
///   1. PowerupConfig.initiallyUnlocked  → always unlocked, no save needed
///   2. Cloud / PlayerPrefs save         → earned by the player at runtime
///   3. EvaluateForChapter()             → call on every chapter / level advance
///
/// Visual contract:
///   LOCKED   → grayscaleMaterial on RootImage + IconImage + HeaderAssetImage; HeaderText = white
///   UNLOCKED → null material restored on all three; HeaderText = rarity colour
///              Common #98F3AF | Rare #F8E64B | Epic #EAB3FF | Legendary #FF9B94
/// </summary>
public class PowerupLockManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    // Singleton
    // ═══════════════════════════════════════════════════════════════════════

    public static PowerupLockManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════
    // Inspector
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Data")]
    [Tooltip("Drag your PowerupDatabase ScriptableObject here.")]
    [SerializeField] private PowerupDatabase database;

    [Header("Grayscale Material")]
    [Tooltip("Drag your grey UI material here.")]
    [SerializeField] private Material grayscaleMaterial;

    // ═══════════════════════════════════════════════════════════════════════
    // Rarity colours
    // ═══════════════════════════════════════════════════════════════════════

    private static readonly Color ColCommon = Hex("98F3AF");
    private static readonly Color ColRare = Hex("F8E64B");
    private static readonly Color ColEpic = Hex("EAB3FF");
    private static readonly Color ColLegendary = Hex("FF9B94");

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Fired when a card transitions to unlocked. Arg = powerup id.</summary>
    public static event Action<string> OnPowerupUnlocked;

    /// <summary>Fired once all saved states have been loaded and applied to cards.</summary>
    public static event Action OnStatesLoaded;

    // ═══════════════════════════════════════════════════════════════════════
    // Internal state
    // ═══════════════════════════════════════════════════════════════════════

    private const string SAVE_PREFIX = "PowerupUnlocked_";

    /// <summary>True once the async load has completed at least once.</summary>
    private bool _statesReady = false;

    /// <summary>id → is this card currently unlocked?</summary>
    private readonly Dictionary<string, bool> _state = new();

    /// <summary>id → live card controller registered from the scene.</summary>
    private readonly Dictionary<string, PowerupCardController> _cards = new();

    // ═══════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (database == null)
        {
            Debug.LogError("[PowerupLock] PowerupDatabase is not assigned in the Inspector.");
            return;
        }

        if (grayscaleMaterial == null)
            Debug.LogWarning("[PowerupLock] Grayscale material is not assigned — no grey-out effect.");

        // Seed every card from its config default immediately and synchronously.
        // Cards that register before the async load finishes will get this state.
        SeedInitialStates();
    }

    private void Start()
    {
        // Async load overlays cloud/prefs saves on top of the seeded state,
        // then calls RefreshAllCards() once complete.
        LoadSavedStates().Forget();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // State seeding
    // ═══════════════════════════════════════════════════════════════════════

    private void SeedInitialStates()
    {
        foreach (var cfg in database.allPowerups)
        {
            _state[cfg.id] = cfg.initiallyUnlocked;
            Debug.Log($"[PowerupLock] Seeded '{cfg.id}' → {(cfg.initiallyUnlocked ? "UNLOCKED" : "locked")}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Persistence — Cloud + PlayerPrefs fallback
    // ═══════════════════════════════════════════════════════════════════════

    private string SaveKey(string id) => SAVE_PREFIX + id.Replace(" ", "");

    private async UniTaskVoid LoadSavedStates()
    {
        if (database == null) return;

        try
        {
            var keys = new HashSet<string>();
            foreach (var cfg in database.allPowerups)
                if (!cfg.initiallyUnlocked)          // already true — no need to check cloud
                    keys.Add(SaveKey(cfg.id));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            foreach (var cfg in database.allPowerups)
            {
                if (cfg.initiallyUnlocked) continue;

                string key = SaveKey(cfg.id);
                if (data.ContainsKey(key) && data[key].Value.GetAs<bool>())
                {
                    _state[cfg.id] = true;
                    Debug.Log($"[PowerupLock] Cloud restored unlock for '{cfg.id}'");
                }
            }

            Debug.Log("[PowerupLock] Cloud load complete.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PowerupLock] Cloud load failed — using PlayerPrefs. ({ex.Message})");
            LoadFromPlayerPrefs();
        }

        // States are now final — apply to all registered cards and flag ready.
        _statesReady = true;
        RefreshAllCards();
        OnStatesLoaded?.Invoke();

        foreach (var kvp in _state)
            if (kvp.Value) OnPowerupUnlocked?.Invoke(kvp.Key);
    }

    private void LoadFromPlayerPrefs()
    {
        foreach (var cfg in database.allPowerups)
        {
            if (cfg.initiallyUnlocked) continue;
            if (PlayerPrefs.GetInt(SaveKey(cfg.id), 0) == 1)
            {
                _state[cfg.id] = true;
                Debug.Log($"[PowerupLock] PlayerPrefs restored unlock for '{cfg.id}'");
            }
        }
    }

    private async UniTaskVoid PersistUnlock(string id)
    {
        string key = SaveKey(id);
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(key, true);
            Debug.Log($"[PowerupLock] Cloud saved unlock for '{id}'");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PowerupLock] Cloud save failed — using PlayerPrefs. ({ex.Message})");
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Card registration
    // Called from PowerupCardController.Start() / OnDestroy()
    // ═══════════════════════════════════════════════════════════════════════

    public void RegisterCard(PowerupCardController card)
    {
        if (card == null || string.IsNullOrEmpty(card.PowerupId))
        {
            Debug.LogWarning("[PowerupLock] RegisterCard called with null card or empty PowerupId.");
            return;
        }

        _cards[card.PowerupId] = card;

        // Look up current state — fall back to locked if id not yet seeded.
        bool unlocked = _state.TryGetValue(card.PowerupId, out bool s) && s;

        Debug.Log($"[PowerupLock] RegisterCard '{card.PowerupId}' — " +
                  $"state={(unlocked ? "UNLOCKED" : "locked")}, statesReady={_statesReady}");

        ApplyVisual(card, unlocked);
    }

    public void UnregisterCard(PowerupCardController card)
    {
        if (card != null) _cards.Remove(card.PowerupId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Is this powerup currently unlocked?</summary>
    public bool IsUnlocked(string id)
        => _state.TryGetValue(id, out bool v) && v;

    /// <summary>Is the powerup at this database index currently unlocked?</summary>
    public bool IsUnlocked(int index)
    {
        if (database == null || index < 0 || index >= database.allPowerups.Count) return false;
        return IsUnlocked(database.allPowerups[index].id);
    }

    /// <summary>
    /// Unlock a card by id. Persists the unlock and immediately refreshes the card visual.
    /// </summary>
    public void UnlockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_state.TryGetValue(id, out bool already) && already)
        {
            // Already unlocked in state — but make sure the visual is also correct.
            if (_cards.TryGetValue(id, out var existingCard))
                ApplyUnlockedVisual(existingCard);
            return;
        }

        _state[id] = true;

        // Only persist runtime unlocks — initiallyUnlocked is always true from config.
        var cfg = database.GetPowerupById(id);
        if (cfg != null && !cfg.initiallyUnlocked)
            PersistUnlock(id).Forget();

        if (_cards.TryGetValue(id, out var card))
            ApplyUnlockedVisual(card);
        else
            Debug.LogWarning($"[PowerupLock] UnlockPowerup '{id}' — card not registered yet. " +
                             "Visual will apply when card registers.");

        OnPowerupUnlocked?.Invoke(id);
        Debug.Log($"[PowerupLock] '{id}' unlocked.");
    }

    /// <summary>Unlock by database index.</summary>
    public void UnlockPowerup(int index)
    {
        if (database == null || index < 0 || index >= database.allPowerups.Count) return;
        UnlockPowerup(database.allPowerups[index].id);
    }

    /// <summary>Force-lock a card (debug / chapter reset).</summary>
    public void LockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _state[id] = false;
        if (_cards.TryGetValue(id, out var card))
            ApplyLockedVisual(card);
    }

    /// <summary>
    /// Call on every chapter / level advance.
    /// Any card whose gate is now met will be unlocked automatically.
    /// </summary>
    public void EvaluateForChapter(int currentChapter, int currentChapterLevel)
    {
        if (database == null) return;

        foreach (var cfg in database.allPowerups)
        {
            if (_state.TryGetValue(cfg.id, out bool already) && already) continue;
            if (MeetsChapterGate(cfg, currentChapter, currentChapterLevel))
                UnlockPowerup(cfg.id);
        }
    }

    /// <summary>Returns true when the player meets both unlock gates for a config.</summary>
    public bool MeetsChapterGate(PowerupConfig cfg, int chapter, int chapterLevel)
        => chapter >= cfg.unlockFromChapter && chapterLevel >= cfg.spinUnlockLevel;

    /// <summary>
    /// Resets all non-initiallyUnlocked cards back to locked.
    /// Call at "Chapter Level 0" as per the reset rule.
    /// </summary>
    public void ResetForNewChapter()
    {
        if (database == null) return;
        foreach (var cfg in database.allPowerups)
            if (!cfg.initiallyUnlocked)
                LockPowerup(cfg.id);

        Debug.Log("[PowerupLock] Chapter reset — gated cards locked.");
    }

    /// <summary>Re-apply current state to every registered card.</summary>
    public void RefreshAllCards()
    {
        Debug.Log($"[PowerupLock] RefreshAllCards — {_cards.Count} cards registered.");
        foreach (var kvp in _cards)
        {
            bool unlocked = _state.TryGetValue(kvp.Key, out bool u) && u;
            Debug.Log($"[PowerupLock]   '{kvp.Key}' → {(unlocked ? "UNLOCKED" : "locked")}");
            ApplyVisual(kvp.Value, unlocked);
        }
    }

    /// <summary>Expose database for Editor tooling only.</summary>
    public PowerupDatabase GetDatabase() => database;

    // ═══════════════════════════════════════════════════════════════════════
    // Visuals
    // ═══════════════════════════════════════════════════════════════════════

    private void ApplyVisual(PowerupCardController card, bool unlocked)
    {
        if (unlocked) ApplyUnlockedVisual(card);
        else ApplyLockedVisual(card);
    }

    public void ApplyLockedVisual(PowerupCardController card)
    {
        if (card == null) return;
        SetMat(card.RootImage, grayscaleMaterial);
        SetMat(card.IconImage, grayscaleMaterial);
        SetMat(card.HeaderAssetImage, grayscaleMaterial);
        if (card.HeaderText != null) card.HeaderText.color = Color.white;
    }

    public void ApplyUnlockedVisual(PowerupCardController card)
    {
        if (card == null) return;
        SetMat(card.RootImage, null);
        SetMat(card.IconImage, null);
        SetMat(card.HeaderAssetImage, null);
        if (card.HeaderText != null)
            card.HeaderText.color = RarityColour(card.Rarity);
    }

    private static void SetMat(Image img, Material mat)
    {
        if (img != null) img.material = mat;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Utility
    // ═══════════════════════════════════════════════════════════════════════

    private static Color RarityColour(PowerupRarity r) => r switch
    {
        PowerupRarity.Common => ColCommon,
        PowerupRarity.Rare => ColRare,
        PowerupRarity.Epic => ColEpic,
        PowerupRarity.Legendary => ColLegendary,
        _ => Color.white
    };

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
