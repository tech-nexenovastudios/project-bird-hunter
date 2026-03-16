//using System;
//using System.Collections.Generic;
//using Cysharp.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.UI;
//using Gameplay.PowerUps;

///// <summary>
///// Central singleton that owns every PowerUP card's lock / unlock state.
/////
///// Unlock priority (highest wins):
/////   1. PowerupConfig.initiallyUnlocked  → always unlocked, no save needed
/////   2. Cloud / PlayerPrefs save         → earned by the player at runtime
/////   3. EvaluateForChapter()             → call on every chapter / level advance
/////
///// Visual contract:
/////   LOCKED   → grayscaleMaterial on RootImage + IconImage + HeaderAssetImage; HeaderText = white
/////   UNLOCKED → null material restored on all three; HeaderText = rarity colour
/////              Common #98F3AF | Rare #F8E64B | Epic #EAB3FF | Legendary #FF9B94
///// </summary>
//public class PowerupLockManager : MonoBehaviour
//{
//    // ═══════════════════════════════════════════════════════════════════════
//    // Singleton
//    // ═══════════════════════════════════════════════════════════════════════

//    public static PowerupLockManager Instance { get; private set; }

//    // ═══════════════════════════════════════════════════════════════════════
//    // Inspector
//    // ═══════════════════════════════════════════════════════════════════════

//    [Header("Data")]
//    [Tooltip("Drag your PowerupDatabase ScriptableObject here.")]
//    [SerializeField] private PowerupDatabase database;

//    [Header("Grayscale Material")]
//    [Tooltip("Drag your grey UI material here.")]
//    [SerializeField] private Material grayscaleMaterial;

//    // ═══════════════════════════════════════════════════════════════════════
//    // Rarity colours
//    // ═══════════════════════════════════════════════════════════════════════

//    private static readonly Color ColCommon = Hex("98F3AF");
//    private static readonly Color ColRare = Hex("F8E64B");
//    private static readonly Color ColEpic = Hex("EAB3FF");
//    private static readonly Color ColLegendary = Hex("FF9B94");

//    // ═══════════════════════════════════════════════════════════════════════
//    // Events
//    // ═══════════════════════════════════════════════════════════════════════

//    /// <summary>Fired when a card transitions to unlocked. Arg = powerup id.</summary>
//    public static event Action<string> OnPowerupUnlocked;

//    /// <summary>Fired once all saved states have been loaded and applied to cards.</summary>
//    public static event Action OnStatesLoaded;

//    // ═══════════════════════════════════════════════════════════════════════
//    // Internal state
//    // ═══════════════════════════════════════════════════════════════════════

//    private const string SAVE_PREFIX = "PowerupUnlocked_";

//    /// <summary>True once the async load has completed at least once.</summary>
//    private bool _statesReady = false;

//    /// <summary>id → is this card currently unlocked?</summary>
//    private readonly Dictionary<string, bool> _state = new();

//    /// <summary>id → live card controller registered from the scene.</summary>
//    private readonly Dictionary<string, PowerupCardController> _cards = new();

//    // ═══════════════════════════════════════════════════════════════════════
//    // Unity lifecycle
//    // ═══════════════════════════════════════════════════════════════════════

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        if (database == null)
//        {
//            Debug.LogError("[PowerupLock] PowerupDatabase is not assigned in the Inspector.");
//            return;
//        }

//        if (grayscaleMaterial == null)
//            Debug.LogWarning("[PowerupLock] Grayscale material is not assigned — no grey-out effect.");

//        // Seed every card from its config default immediately and synchronously.
//        // Cards that register before the async load finishes will get this state.
//        SeedInitialStates();
//    }

//    private void Start()
//    {
//        // Async load overlays cloud/prefs saves on top of the seeded state,
//        // then calls RefreshAllCards() once complete.
//        LoadSavedStates().Forget();
//    }

//    // ═══════════════════════════════════════════════════════════════════════
//    // State seeding
//    // ═══════════════════════════════════════════════════════════════════════

//    private void SeedInitialStates()
//    {
//        foreach (var cfg in database.allPowerups)
//        {
//            _state[cfg.id] = cfg.initiallyUnlocked;
//            Debug.Log($"[PowerupLock] Seeded '{cfg.id}' → {(cfg.initiallyUnlocked ? "UNLOCKED" : "locked")}");
//        }
//    }

//    // ═══════════════════════════════════════════════════════════════════════
//    // Persistence — Cloud + PlayerPrefs fallback
//    // ═══════════════════════════════════════════════════════════════════════

//    private string SaveKey(string id) => SAVE_PREFIX + id.Replace(" ", "");

//    private async UniTaskVoid LoadSavedStates()
//    {
//        if (database == null) return;

//        try
//        {
//            var keys = new HashSet<string>();
//            foreach (var cfg in database.allPowerups)
//                if (!cfg.initiallyUnlocked)          // already true — no need to check cloud
//                    keys.Add(SaveKey(cfg.id));

//            var data = await CloudSaveManager.Instance.LoadAsync(keys);

//            foreach (var cfg in database.allPowerups)
//            {
//                if (cfg.initiallyUnlocked) continue;

//                string key = SaveKey(cfg.id);
//                if (data.ContainsKey(key) && data[key].Value.GetAs<bool>())
//                {
//                    _state[cfg.id] = true;
//                    Debug.Log($"[PowerupLock] Cloud restored unlock for '{cfg.id}'");
//                }
//            }

//            Debug.Log("[PowerupLock] Cloud load complete.");
//        }
//        catch (Exception ex)
//        {
//            Debug.LogWarning($"[PowerupLock] Cloud load failed — using PlayerPrefs. ({ex.Message})");
//            LoadFromPlayerPrefs();
//        }

//        // States are now final — apply to all registered cards and flag ready.
//        _statesReady = true;
//        RefreshAllCards();
//        OnStatesLoaded?.Invoke();

//        foreach (var kvp in _state)
//            if (kvp.Value) OnPowerupUnlocked?.Invoke(kvp.Key);
//    }

//    private void LoadFromPlayerPrefs()
//    {
//        foreach (var cfg in database.allPowerups)
//        {
//            if (cfg.initiallyUnlocked) continue;
//            if (PlayerPrefs.GetInt(SaveKey(cfg.id), 0) == 1)
//            {
//                _state[cfg.id] = true;
//                Debug.Log($"[PowerupLock] PlayerPrefs restored unlock for '{cfg.id}'");
//            }
//        }
//    }

//    private async UniTaskVoid PersistUnlock(string id)
//    {
//        string key = SaveKey(id);
//        try
//        {
//            await CloudSaveManager.Instance.SaveValueAsync(key, true);
//            Debug.Log($"[PowerupLock] Cloud saved unlock for '{id}'");
//        }
//        catch (Exception ex)
//        {
//            Debug.LogWarning($"[PowerupLock] Cloud save failed — using PlayerPrefs. ({ex.Message})");
//            PlayerPrefs.SetInt(key, 1);
//            PlayerPrefs.Save();
//        }
//    }

//    // ═══════════════════════════════════════════════════════════════════════
//    // Card registration
//    // Called from PowerupCardController.Start() / OnDestroy()
//    // ═══════════════════════════════════════════════════════════════════════

//    public void RegisterCard(PowerupCardController card)
//    {
//        if (card == null || string.IsNullOrEmpty(card.PowerupId))
//        {
//            Debug.LogWarning("[PowerupLock] RegisterCard called with null card or empty PowerupId.");
//            return;
//        }

//        _cards[card.PowerupId] = card;

//        // Look up current state — fall back to locked if id not yet seeded.
//        bool unlocked = _state.TryGetValue(card.PowerupId, out bool s) && s;

//        Debug.Log($"[PowerupLock] RegisterCard '{card.PowerupId}' — " +
//                  $"state={(unlocked ? "UNLOCKED" : "locked")}, statesReady={_statesReady}");

//        ApplyVisual(card, unlocked);
//    }

//    public void UnregisterCard(PowerupCardController card)
//    {
//        if (card != null) _cards.Remove(card.PowerupId);
//    }

//    // ═══════════════════════════════════════════════════════════════════════
//    // Public API
//    // ═══════════════════════════════════════════════════════════════════════

//    /// <summary>Is this powerup currently unlocked?</summary>
//    public bool IsUnlocked(string id)
//        => _state.TryGetValue(id, out bool v) && v;

//    /// <summary>Is the powerup at this database index currently unlocked?</summary>
//    public bool IsUnlocked(int index)
//    {
//        if (database == null || index < 0 || index >= database.allPowerups.Count) return false;
//        return IsUnlocked(database.allPowerups[index].id);
//    }

//    /// <summary>
//    /// Unlock a card by id. Persists the unlock and immediately refreshes the card visual.
//    /// </summary>
//    public void UnlockPowerup(string id)
//    {
//        if (string.IsNullOrEmpty(id)) return;
//        if (_state.TryGetValue(id, out bool already) && already)
//        {
//            // Already unlocked in state — but make sure the visual is also correct.
//            if (_cards.TryGetValue(id, out var existingCard))
//                ApplyUnlockedVisual(existingCard);
//            return;
//        }

//        _state[id] = true;

//        // Only persist runtime unlocks — initiallyUnlocked is always true from config.
//        var cfg = database.GetPowerupById(id);
//        if (cfg != null && !cfg.initiallyUnlocked)
//            PersistUnlock(id).Forget();

//        if (_cards.TryGetValue(id, out var card))
//            ApplyUnlockedVisual(card);
//        else
//            Debug.LogWarning($"[PowerupLock] UnlockPowerup '{id}' — card not registered yet. " +
//                             "Visual will apply when card registers.");

//        OnPowerupUnlocked?.Invoke(id);
//        Debug.Log($"[PowerupLock] '{id}' unlocked.");
//    }

//    /// <summary>Unlock by database index.</summary>
//    public void UnlockPowerup(int index)
//    {
//        if (database == null || index < 0 || index >= database.allPowerups.Count) return;
//        UnlockPowerup(database.allPowerups[index].id);
//    }

//    /// <summary>Force-lock a card (debug / chapter reset).</summary>
//    public void LockPowerup(string id)
//    {
//        if (string.IsNullOrEmpty(id)) return;
//        _state[id] = false;
//        if (_cards.TryGetValue(id, out var card))
//            ApplyLockedVisual(card);
//    }

//    /// <summary>
//    /// Call on every chapter / level advance.
//    /// Any card whose gate is now met will be unlocked automatically.
//    /// </summary>
//    public void EvaluateForChapter(int currentChapter, int currentChapterLevel)
//    {
//        if (database == null) return;

//        foreach (var cfg in database.allPowerups)
//        {
//            if (_state.TryGetValue(cfg.id, out bool already) && already) continue;
//            if (MeetsChapterGate(cfg, currentChapter, currentChapterLevel))
//                UnlockPowerup(cfg.id);
//        }
//    }

//    /// <summary>Returns true when the player meets both unlock gates for a config.</summary>
//    public bool MeetsChapterGate(PowerupConfig cfg, int chapter, int chapterLevel)
//        => chapter >= cfg.unlockFromChapter && chapterLevel >= cfg.spinUnlockLevel;

//    /// <summary>
//    /// Resets all non-initiallyUnlocked cards back to locked.
//    /// Call at "Chapter Level 0" as per the reset rule.
//    /// </summary>
//    public void ResetForNewChapter()
//    {
//        if (database == null) return;
//        foreach (var cfg in database.allPowerups)
//            if (!cfg.initiallyUnlocked)
//                LockPowerup(cfg.id);

//        Debug.Log("[PowerupLock] Chapter reset — gated cards locked.");
//    }

//    /// <summary>Re-apply current state to every registered card.</summary>
//    public void RefreshAllCards()
//    {
//        Debug.Log($"[PowerupLock] RefreshAllCards — {_cards.Count} cards registered.");
//        foreach (var kvp in _cards)
//        {
//            bool unlocked = _state.TryGetValue(kvp.Key, out bool u) && u;
//            Debug.Log($"[PowerupLock]   '{kvp.Key}' → {(unlocked ? "UNLOCKED" : "locked")}");
//            ApplyVisual(kvp.Value, unlocked);
//        }
//    }

//    /// <summary>Expose database for Editor tooling only.</summary>
//    public PowerupDatabase GetDatabase() => database;

//    // ═══════════════════════════════════════════════════════════════════════
//    // Visuals
//    // ═══════════════════════════════════════════════════════════════════════

//    private void ApplyVisual(PowerupCardController card, bool unlocked)
//    {
//        if (unlocked) ApplyUnlockedVisual(card);
//        else ApplyLockedVisual(card);
//    }

//    public void ApplyLockedVisual(PowerupCardController card)
//    {
//        if (card == null) return;
//        SetMat(card.RootImage, grayscaleMaterial);
//        SetMat(card.IconImage, grayscaleMaterial);
//        SetMat(card.HeaderAssetImage, grayscaleMaterial);
//        if (card.HeaderText != null) card.HeaderText.color = Color.white;
//    }

//    public void ApplyUnlockedVisual(PowerupCardController card)
//    {
//        if (card == null) return;
//        SetMat(card.RootImage, null);
//        SetMat(card.IconImage, null);
//        SetMat(card.HeaderAssetImage, null);
//        if (card.HeaderText != null)
//            card.HeaderText.color = RarityColour(card.Rarity);
//    }

//    private static void SetMat(Image img, Material mat)
//    {
//        if (img != null) img.material = mat;
//    }

//    // ═══════════════════════════════════════════════════════════════════════
//    // Utility
//    // ═══════════════════════════════════════════════════════════════════════

//    private static Color RarityColour(PowerupRarity r) => r switch
//    {
//        PowerupRarity.Common => ColCommon,
//        PowerupRarity.Rare => ColRare,
//        PowerupRarity.Epic => ColEpic,
//        PowerupRarity.Legendary => ColLegendary,
//        _ => Color.white
//    };

//    private static Color Hex(string hex)
//    {
//        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
//        return c;
//    }
//}


using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.PowerUps;

/// <summary>
/// Central singleton that owns every PowerUP card's lock / unlock state.
///
/// ── How unlock works ────────────────────────────────────────────────────────
/// Each PowerupConfig has two gate fields:
///   • unlockFromChapter  — the chapter number where this card first appears
///   • spinUnlockLevel    — the level WITHIN that chapter (0, 6, 11, 16)
///
/// A card unlocks when BOTH are satisfied:
///   currentChapter >= unlockFromChapter  AND  currentChapterLevel >= spinUnlockLevel
///
/// Cards with initiallyUnlocked = true skip all gates and are always unlocked.
///
/// ── How to trigger evaluation ────────────────────────────────────────────────
/// Call ONE of these from your level-complete / chapter-advance logic:
///
///   PowerupLockManager.Instance.OnLevelCompleted(chapter, levelWithinChapter);
///
/// Or fire the static event from anywhere in your codebase:
///
///   PowerupLockManager.OnPlayerProgressChanged?.Invoke(chapter, levelWithinChapter);
///
/// ── Reset rule ───────────────────────────────────────────────────────────────
/// At Chapter Level 0 (new chapter start) call:
///   PowerupLockManager.Instance.ResetForNewChapter();
/// This locks all non-initiallyUnlocked cards so the new chapter's gates apply.
///
/// ── Visual contract ──────────────────────────────────────────────────────────
///   LOCKED   → grayscaleMaterial on RootImage + IconImage + HeaderAssetImage
///              HeaderText colour = white
///   UNLOCKED → null material (Unity default) on all three
///              HeaderText colour = rarity colour
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
    // Static event — fire this from anywhere to trigger unlock evaluation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fire this whenever the player completes a level or advances progress.
    /// Args: (currentChapter, currentChapterLevel)
    /// Example:
    ///   PowerupLockManager.OnPlayerProgressChanged?.Invoke(2, 6);
    /// </summary>
    public static event Action<int, int> OnPlayerProgressChanged;

    // ═══════════════════════════════════════════════════════════════════════
    // Output events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Fired when a card transitions to unlocked. Arg = powerup id.</summary>
    public static event Action<string> OnPowerupUnlocked;

    /// <summary>Fired once saved states have loaded and all cards are refreshed.</summary>
    public static event Action OnStatesLoaded;

    // ═══════════════════════════════════════════════════════════════════════
    // Rarity colours
    // ═══════════════════════════════════════════════════════════════════════

    private static readonly Color ColCommon = Hex("98F3AF");
    private static readonly Color ColRare = Hex("F8E64B");
    private static readonly Color ColEpic = Hex("EAB3FF");
    private static readonly Color ColLegendary = Hex("FF9B94");

    // ═══════════════════════════════════════════════════════════════════════
    // Internal state
    // ═══════════════════════════════════════════════════════════════════════

    private const string SAVE_PREFIX = "PowerupUnlocked_";

    private bool _statesReady;

    /// <summary>id → is this card currently unlocked?</summary>
    private readonly Dictionary<string, bool> _state = new();

    /// <summary>id → live card controller registered from the scene.</summary>
    private readonly Dictionary<string, PowerupCardController> _cards = new();

    // Tracks last known progress so cards spawned late still evaluate correctly
    private int _lastChapter = 0;
    private int _lastChapterLevel = 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (database == null)
        {
            Debug.LogError("[PowerupLock] PowerupDatabase not assigned in Inspector.");
            return;
        }
        if (grayscaleMaterial == null)
            Debug.LogWarning("[PowerupLock] Grayscale material not assigned — no grey-out effect.");

        SeedInitialStates();
    }

    private void OnEnable()
    {
        OnPlayerProgressChanged += HandleProgressChanged;
    }

    private void OnDisable()
    {
        OnPlayerProgressChanged -= HandleProgressChanged;
    }

    private void Start()
    {
        LoadSavedStates().Forget();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Progress listener
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Subscribed to OnPlayerProgressChanged.
    /// Called automatically whenever your game fires the static event.
    /// </summary>
    private void HandleProgressChanged(int chapter, int chapterLevel)
    {
        _lastChapter = chapter;
        _lastChapterLevel = chapterLevel;

        // Chapter Level 0 = new chapter starting → reset gated cards first
        if (chapterLevel == 0)
            ResetForNewChapter();

        EvaluateForChapter(chapter, chapterLevel);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // State seeding
    // ═══════════════════════════════════════════════════════════════════════

    private void SeedInitialStates()
    {
        foreach (var cfg in database.allPowerups)
            _state[cfg.id] = cfg.initiallyUnlocked;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Persistence
    // ═══════════════════════════════════════════════════════════════════════

    private string SaveKey(string id) => SAVE_PREFIX + id.Replace(" ", "");

    private async UniTaskVoid LoadSavedStates()
    {
        if (database == null) return;

        try
        {
            var keys = new HashSet<string>();
            foreach (var cfg in database.allPowerups)
                if (!cfg.initiallyUnlocked)
                    keys.Add(SaveKey(cfg.id));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            foreach (var cfg in database.allPowerups)
            {
                if (cfg.initiallyUnlocked) continue;
                string key = SaveKey(cfg.id);
                if (data.ContainsKey(key) && data[key].Value.GetAs<bool>())
                    _state[cfg.id] = true;
            }

            Debug.Log("[PowerupLock] Cloud load complete.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PowerupLock] Cloud load failed — using PlayerPrefs. ({ex.Message})");
            LoadFromPlayerPrefs();
        }

        _statesReady = true;

        // Re-evaluate with last known progress in case progress was set
        // before states finished loading
        if (_lastChapter > 0)
            EvaluateForChapter(_lastChapter, _lastChapterLevel);

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
                _state[cfg.id] = true;
        }
    }

    private async UniTaskVoid PersistUnlock(string id)
    {
        string key = SaveKey(id);
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(key, true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PowerupLock] Cloud save failed — using PlayerPrefs. ({ex.Message})");
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Chapter gate evaluation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks every locked card against its unlock gates for the given progress.
    /// Any card that passes both unlockFromChapter and spinUnlockLevel is unlocked.
    /// Called automatically via OnPlayerProgressChanged — but can also be called
    /// manually if needed.
    /// </summary>
    public void EvaluateForChapter(int chapter, int chapterLevel)
    {
        if (database == null) return;

        bool anyUnlocked = false;
        foreach (var cfg in database.allPowerups)
        {
            if (_state.TryGetValue(cfg.id, out bool already) && already) continue;
            if (MeetsChapterGate(cfg, chapter, chapterLevel))
            {
                UnlockPowerup(cfg.id);
                anyUnlocked = true;
            }
        }

        if (anyUnlocked)
            Debug.Log($"[PowerupLock] EvaluateForChapter({chapter}, {chapterLevel}) — new cards unlocked.");
    }

    /// <summary>True when the player's progress satisfies both gate fields.</summary>
    public bool MeetsChapterGate(PowerupConfig cfg, int chapter, int chapterLevel)
        => chapter >= cfg.unlockFromChapter && chapterLevel >= cfg.spinUnlockLevel;

    /// <summary>
    /// Locks all non-initiallyUnlocked cards.
    /// Called automatically when chapterLevel == 0 (new chapter starts).
    /// Also call manually if your game has a hard chapter reset.
    /// </summary>
    public void ResetForNewChapter()
    {
        if (database == null) return;
        foreach (var cfg in database.allPowerups)
            if (!cfg.initiallyUnlocked)
                LockPowerup(cfg.id);

        Debug.Log("[PowerupLock] New chapter — gated cards reset to locked.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Card registration
    // ═══════════════════════════════════════════════════════════════════════

    public void RegisterCard(PowerupCardController card)
    {
        if (card == null || string.IsNullOrEmpty(card.PowerupId))
        {
            Debug.LogWarning("[PowerupLock] RegisterCard — null card or empty PowerupId.");
            return;
        }

        _cards[card.PowerupId] = card;

        // If states are already loaded, apply final state immediately.
        // If still loading, apply seeded state now — RefreshAllCards will
        // correct it once the async load finishes.
        bool unlocked = _state.TryGetValue(card.PowerupId, out bool s) && s;
        ApplyVisual(card, unlocked);
    }

    public void UnregisterCard(PowerupCardController card)
    {
        if (card != null) _cards.Remove(card.PowerupId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Primary entry point for your level-complete / progress system.
    /// Call this whenever the player finishes a level.
    /// Automatically resets on chapterLevel == 0 then evaluates gates.
    /// </summary>
    public void OnLevelCompleted(int chapter, int chapterLevel)
    {
        // Fire the event so any other listeners also get notified
        OnPlayerProgressChanged?.Invoke(chapter, chapterLevel);
    }

    public bool IsUnlocked(string id)
        => _state.TryGetValue(id, out bool v) && v;

    public bool IsUnlocked(int index)
    {
        if (database == null || index < 0 || index >= database.allPowerups.Count) return false;
        return IsUnlocked(database.allPowerups[index].id);
    }

    public void UnlockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_state.TryGetValue(id, out bool already) && already)
        {
            if (_cards.TryGetValue(id, out var existing))
                ApplyUnlockedVisual(existing);
            return;
        }

        _state[id] = true;

        var cfg = database.GetPowerupById(id);
        if (cfg != null && !cfg.initiallyUnlocked)
            PersistUnlock(id).Forget();

        if (_cards.TryGetValue(id, out var card))
            ApplyUnlockedVisual(card);

        OnPowerupUnlocked?.Invoke(id);
        Debug.Log($"[PowerupLock] '{id}' unlocked.");
    }

    public void UnlockPowerup(int index)
    {
        if (database == null || index < 0 || index >= database.allPowerups.Count) return;
        UnlockPowerup(database.allPowerups[index].id);
    }

    public void LockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _state[id] = false;
        if (_cards.TryGetValue(id, out var card))
            ApplyLockedVisual(card);
    }

    public void RefreshAllCards()
    {
        foreach (var kvp in _cards)
            ApplyVisual(kvp.Value, _state.TryGetValue(kvp.Key, out bool u) && u);
    }

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
