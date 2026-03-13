using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central manager for cannon lock / unlock state.
///
/// UNLOCK RULES:
///   1. Required chapter must be unlocked (via ChapterUnlockManager).
///   2. Player must afford BOTH coinCost (Gold) AND gemCost (Gems) together.
///   3. Both currencies are deducted atomically via CurrencyManager.SpendMultiple().
///   4. State is saved to cloud and OnCannonUnlocked event fires.
///
/// CLOUD KEY FORMAT:
///   "CannonUnlocked_<CannonNameNoSpaces>"
///   e.g. CannonUnlocked_SingleShot, CannonUnlocked_RapidFire
///
/// FREE CANNONS (coinCost == 0 and gemCost == 0):
///   No currency deducted — unlock is instant.
/// </summary>
public class CannonLockManager : MonoBehaviour
{
    public static CannonLockManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Grayscale")]
    [SerializeField] private Material grayscaleMaterial;

    // ── Constants ─────────────────────────────────────────────────────────────
    private const string UNLOCK_KEY_PREFIX = "CannonUnlocked_";

    // ── Runtime State ─────────────────────────────────────────────────────────
    private bool[] _unlockedState;

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fires when a cannon is successfully unlocked. int = cannon index.</summary>
    public static event Action<int> OnCannonUnlocked;

    /// <summary>Fires after all unlock states finish loading from cloud.</summary>
    public static event Action OnAllUnlockStatesLoaded;

    // ═════════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (cannonHolderSO != null)
        {
            _unlockedState = new bool[cannonHolderSO.cannonsData.Length];
            _unlockedState[0] = true; // Single Shot is always free
        }
    }

    private void Start()
    {
        LoadUnlockState().Forget();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud Key
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cloud key per cannon — built from its name so it is human-readable in the dashboard.
    /// Example: "CannonUnlocked_RapidFire"
    /// </summary>
    private string GetUnlockKey(int index)
        => UNLOCK_KEY_PREFIX + cannonHolderSO.cannonsData[index].cannonName.Replace(" ", "");

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Load
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid LoadUnlockState()
    {
        if (cannonHolderSO == null) return;

        try
        {
            var keys = new HashSet<string>();
            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
                keys.Add(GetUnlockKey(i));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
            {
                string key = GetUnlockKey(i);
                if (data.ContainsKey(key))
                    _unlockedState[i] = data[key].Value.GetAs<bool>();
            }

            _unlockedState[0] = true; // Index 0 always unlocked — never let cloud override this
            Debug.Log("[CannonLock] Unlock states loaded from cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonLock] Failed to load: {ex.Message}");
        }

        // Tell all listeners loading is complete
        OnAllUnlockStatesLoaded?.Invoke();

        // Fire event for every already-unlocked cannon so UI can rebuild correctly
        for (int i = 0; i < _unlockedState.Length; i++)
            if (_unlockedState[i])
                OnCannonUnlocked?.Invoke(i);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Save
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid SaveUnlock(int index)
    {
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(GetUnlockKey(index), true);
            Debug.Log($"[CannonLock] Saved — index {index}, key: {GetUnlockKey(index)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonLock] Failed to save unlock: {ex.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API – Queries
    // ═════════════════════════════════════════════════════════════════════════

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= _unlockedState.Length) return false;
        return _unlockedState[index];
    }

    public int GetTotalCannons() => cannonHolderSO != null ? cannonHolderSO.cannonsData.Length : 0;
    public Material GetGrayscaleMaterial() => grayscaleMaterial;
    public int GetCoinCost(int index) => ValidateIndex(index) ? cannonHolderSO.cannonsData[index].coinCost : 0;
    public int GetGemCost(int index) => ValidateIndex(index) ? cannonHolderSO.cannonsData[index].gemCost : 0;

    /// <summary>Returns the required chapter (0-based). Ch1=0, Ch2=1, etc.</summary>
    public int GetRequiredChapter(int index) => ValidateIndex(index) ? cannonHolderSO.cannonsData[index].unlockChapterRequired : 0;

    /// <summary>True if the chapter gate is satisfied for this cannon.</summary>
    public bool IsChapterRequirementMet(int index)
        => ValidateIndex(index) && IsChapterUnlocked(cannonHolderSO.cannonsData[index].unlockChapterRequired);

    // ═════════════════════════════════════════════════════════════════════════
    // Public API – Unlock Gate Check
    // ═════════════════════════════════════════════════════════════════════════

    public enum UnlockBlockReason
    {
        None,
        AlreadyUnlocked,
        ChapterNotUnlocked,
        NotEnoughGold,
        NotEnoughGems,
        NotEnoughBoth
    }

    /// <summary>
    /// Checks every condition needed to unlock a cannon.
    /// Returns true if the player can unlock right now.
    /// <paramref name="reason"/> tells you exactly why it is blocked (for UI feedback).
    /// </summary>
    public bool CanUnlock(int index, out UnlockBlockReason reason)
    {
        reason = UnlockBlockReason.None;
        if (!ValidateIndex(index)) return false;
        if (_unlockedState[index]) { reason = UnlockBlockReason.AlreadyUnlocked; return false; }

        var data = cannonHolderSO.cannonsData[index];

        // Gate 1 — chapter must be cleared first
        if (!IsChapterUnlocked(data.unlockChapterRequired))
        {
            reason = UnlockBlockReason.ChapterNotUnlocked;
            return false;
        }

        // Gate 2 — player must afford BOTH currencies
        bool hasGold = CurrencyManager.Instance.CanAffordGold(data.coinCost);
        bool hasGems = CurrencyManager.Instance.CanAffordGems(data.gemCost);

        if (!hasGold && !hasGems) { reason = UnlockBlockReason.NotEnoughBoth; return false; }
        if (!hasGold) { reason = UnlockBlockReason.NotEnoughGold; return false; }
        if (!hasGems) { reason = UnlockBlockReason.NotEnoughGems; return false; }

        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API – Unlock Action
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to unlock a cannon. Spends BOTH Gold and Gems atomically.
    /// Free cannons (both costs == 0) skip currency deduction entirely.
    /// Returns true on success.
    ///
    /// Called by CannonSelectionManager after the player confirms the popup.
    /// </summary>
    public async UniTask<bool> TryUnlock(int index)
    {
        if (!CanUnlock(index, out UnlockBlockReason reason))
        {
            Debug.Log($"[CannonLock] Cannot unlock index {index}: {reason}");
            return false;
        }

        var data = cannonHolderSO.cannonsData[index];
        bool isFree = data.coinCost == 0 && data.gemCost == 0;

        if (!isFree)
        {
            // SpendMultiple does a local affordability check first, then hits the server.
            // Returns false (and resyncs balances) if the server rejects either spend.
            bool spent = await CurrencyManager.Instance.SpendMultiple(
                (CurrencyType.Gold, data.coinCost),
                (CurrencyType.Gems, data.gemCost)
            );

            if (!spent)
            {
                Debug.LogWarning($"[CannonLock] SpendMultiple failed for cannon {index}. Balances resynced.");
                return false;
            }
        }

        ExecuteUnlock(index);
        Debug.Log($"[CannonLock] '{data.cannonName}' unlocked — Cost: {data.coinCost} Gold + {data.gemCost} Gems.");
        return true;
    }

    /// <summary>
    /// Bypasses all checks. Use ONLY for admin / IAP / reward grant flows.
    /// </summary>
    public void ForceUnlock(int index)
    {
        if (!ValidateIndex(index) || _unlockedState[index]) return;
        ExecuteUnlock(index);
        Debug.Log($"[CannonLock] Cannon {index} force-unlocked.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Visual Helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Applies locked visuals — grayscale, shows LockIcon and LockedText.</summary>
    public void ApplyLockedVisual(Transform buttonTransform)
    {
        SetGrayscale(buttonTransform, "Background", true);
        SetGrayscale(buttonTransform, "Cannon", true);
        SetChildActive(buttonTransform, "LockIcon", true);

        var bg = buttonTransform.Find("Background");
        if (bg != null)
        {
            SetChildActive(bg, "LevelDesc", false);
            SetChildActive(bg, "LockedText", true);
            SetChildActive(bg, "LevelCount", false);
        }
    }

    /// <summary>Removes locked visuals — clears grayscale, hides LockIcon and LockedText.</summary>
    public void ApplyUnlockedVisual(Transform buttonTransform)
    {
        SetGrayscale(buttonTransform, "Background", false);
        SetGrayscale(buttonTransform, "Cannon", false);
        SetChildActive(buttonTransform, "LockIcon", false);

        var bg = buttonTransform.Find("Background");
        if (bg != null)
        {
            SetChildActive(bg, "LevelDesc", true);
            SetChildActive(bg, "LockedText", false);
            SetChildActive(bg, "LevelCount", true);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Private Helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The ONE place that marks a cannon as unlocked, saves to cloud, and fires the event.
    /// Every unlock path (TryUnlock, ForceUnlock) must route through here.
    /// </summary>
    private void ExecuteUnlock(int index)
    {
        _unlockedState[index] = true;
        SaveUnlock(index).Forget();
        OnCannonUnlocked?.Invoke(index);
    }

    private bool ValidateIndex(int index)
        => cannonHolderSO != null && index >= 0 && index < cannonHolderSO.cannonsData.Length;

    /// <summary>
    /// Queries ChapterUnlockManager. If missing (editor/test scenes) falls back to true
    /// so the cannon panel doesn't silently break while you are building.
    /// </summary>
    private bool IsChapterUnlocked(int chapterIndex)
    {
        if (ChapterUnlockManager.Instance == null)
        {
            Debug.LogWarning("[CannonLock] ChapterUnlockManager not found — chapter gate bypassed.");
            return true;
        }
        return ChapterUnlockManager.Instance.IsUnlocked(chapterIndex);
    }

    private void SetGrayscale(Transform parent, string childName, bool grey)
    {
        var child = parent.Find(childName);
        if (child == null) return;
        var img = child.GetComponent<Image>();
        if (img != null) img.material = grey ? grayscaleMaterial : null;
    }

    private void SetChildActive(Transform parent, string childName, bool active)
    {
        var child = parent.Find(childName);
        if (child != null) child.gameObject.SetActive(active);
    }
}