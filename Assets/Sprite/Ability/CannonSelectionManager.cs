using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the cannon selection panel UI.
///
/// ── SINGLE ACTION BUTTON — TEXT STATES ──────────────────────────────────────
///   "Ch.X Required"  → locked, chapter gate not passed       (button disabled)
///   "UNLOCK"         → locked, chapter met, can afford       (button enabled)
///   "UNLOCK"         → locked, chapter met, cannot afford    (button disabled)
///   "EQUIP"          → unlocked, not currently equipped      (button enabled)
///   "EQUIPPED"       → unlocked, currently equipped          (button disabled)
///   "SAVING..."      → cloud save in progress                (button disabled)
///
/// ── UNLOCK POPUP ─────────────────────────────────────────────────────────────
///   When the player presses UNLOCK (and chapter is met), a popup appears showing:
///     - Cannon name
///     - Gold cost   (e.g. "1,500 Gold")
///     - Gem cost    (e.g. "40 Gems")
///   The player can then Confirm (spends both) or Cancel.
///
/// ── INSPECTOR WIRING ─────────────────────────────────────────────────────────
///   actionButton / actionButtonText     → the one button + its label
///   unlockPopupPanel                    → root panel (set inactive by default)
///   popupCannonNameText                 → cannon name inside popup
///   popupCoinCostText                   → e.g. "1,500 Gold"
///   popupGemCostText                    → e.g. "40 Gems"
///   popupConfirmButton                  → "Confirm" button inside popup
///   popupCancelButton                   → "Cancel"  button inside popup
/// </summary>
public class CannonSelectionManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Cannon Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Cannon Items (same order as SO)")]
    [SerializeField] private List<CannonItemUI> cannonItems;

    [Header("Common Display")]
    [SerializeField] private Image cannonImage;
    [SerializeField] private TextMeshProUGUI cannonNameText;

    [Header("Single Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("Unlock Popup")]
    [SerializeField] private GameObject unlockPopupPanel;
    [SerializeField] private Image popupCannonImage;     // shows the cannon sprite
    [SerializeField] private TextMeshProUGUI popupCannonNameText;
    [SerializeField] private TextMeshProUGUI popupCoinCostText;
    [SerializeField] private TextMeshProUGUI popupGemCostText;
    [SerializeField] private Button popupConfirmButton;
    [SerializeField] private Button popupCancelButton;

    [Header("Visual States")]
    [SerializeField] private Sprite activeBgSprite;
    [SerializeField] private Sprite inactiveBgSprite;

    // ── Constants ─────────────────────────────────────────────────────────────
    private const string SELECTED_CANNON_KEY = "SelectedCannonIndex";

    // ── Runtime State ─────────────────────────────────────────────────────────
    private int _previewedIndex = -1;
    private int _equippedIndex = -1;
    private bool _isSaving = false;
    private bool _isUnlocking = false; // prevent double-tap on confirm

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fires when the player equips a cannon. int = cannon index.</summary>
    public static event Action<int> OnEquipped;

    // ═════════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonPressed);
        if (popupConfirmButton != null) popupConfirmButton.onClick.AddListener(OnPopupConfirmed);
        if (popupCancelButton != null) popupCancelButton.onClick.AddListener(OnPopupCancelled);

        // Make sure popup starts hidden
        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);
    }

    private void OnEnable()
    {
        CannonLockManager.OnCannonUnlocked += OnCannonUnlockedHandler;
        CannonLockManager.OnAllUnlockStatesLoaded += RefreshAllLockVisuals;
        CannonUpgradeManager.OnCannonUpgraded += OnCannonUpgradedHandler;
        CannonUpgradeManager.OnAllLevelsLoaded += RefreshAllLevelTexts;
    }

    private void OnDisable()
    {
        CannonLockManager.OnCannonUnlocked -= OnCannonUnlockedHandler;
        CannonLockManager.OnAllUnlockStatesLoaded -= RefreshAllLockVisuals;
        CannonUpgradeManager.OnCannonUpgraded -= OnCannonUpgradedHandler;
        CannonUpgradeManager.OnAllLevelsLoaded -= RefreshAllLevelTexts;
    }

    private void Start()
    {
        InitializeCannonItems();
        RefreshAllLockVisuals();
        LoadEquippedData().Forget();
    }

    private void OnDestroy()
    {
        if (actionButton != null) actionButton.onClick.RemoveListener(OnActionButtonPressed);
        if (popupConfirmButton != null) popupConfirmButton.onClick.RemoveListener(OnPopupConfirmed);
        if (popupCancelButton != null) popupCancelButton.onClick.RemoveListener(OnPopupCancelled);

        foreach (var item in cannonItems)
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Initialization
    // ═════════════════════════════════════════════════════════════════════════

    private void InitializeCannonItems()
    {
        if (cannonHolderSO == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (i >= cannonHolderSO.cannonsData.Length) break;

            int index = i; // local copy — prevents the classic closure bug in loops
            if (cannonItems[i].button != null)
            {
                cannonItems[i].button.interactable = true;
                cannonItems[i].button.onClick.AddListener(() => PreviewCannon(index));
            }

            RefreshCannonLevelText(i);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Lock / Upgrade Event Handlers
    // ═════════════════════════════════════════════════════════════════════════

    private void OnCannonUnlockedHandler(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;

        if (CannonLockManager.Instance != null && cannonItems[index].button != null)
            CannonLockManager.Instance.ApplyUnlockedVisual(cannonItems[index].button.transform);

        // If currently previewing this cannon, refresh the right panel too
        if (_previewedIndex == index)
            UpdatePreviewDisplay(index);
    }

    private void RefreshAllLockVisuals()
    {
        if (CannonLockManager.Instance == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (cannonItems[i].button == null) continue;

            if (CannonLockManager.Instance.IsUnlocked(i))
                CannonLockManager.Instance.ApplyUnlockedVisual(cannonItems[i].button.transform);
            else
                CannonLockManager.Instance.ApplyLockedVisual(cannonItems[i].button.transform);

            // Always keep item buttons tappable so players can preview locked cannons
            cannonItems[i].button.interactable = true;
        }
    }

    private void RefreshAllLevelTexts()
    {
        for (int i = 0; i < cannonItems.Count; i++)
            RefreshCannonLevelText(i);
    }

    private void OnCannonUpgradedHandler(int index)
    {
        RefreshCannonLevelText(index);
        if (index == _previewedIndex)
            UpdatePreviewDisplay(index);
    }

    private void RefreshCannonLevelText(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (index >= cannonHolderSO.cannonsData.Length) return;
        if (cannonItems[index].levelText == null) return;
        cannonItems[index].levelText.text = $"{cannonHolderSO.cannonsData[index].cannonLevel}";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Load Equipped
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid LoadEquippedData()
    {
        try
        {
            var keys = new HashSet<string> { SELECTED_CANNON_KEY };
            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            _equippedIndex = data.ContainsKey(SELECTED_CANNON_KEY)
                ? Mathf.Clamp(data[SELECTED_CANNON_KEY].Value.GetAs<int>(), 0, cannonHolderSO.cannonsData.Length - 1)
                : 0;

            PreviewCannon(_equippedIndex);
            Debug.Log($"[Selection] Loaded equipped cannon: {_equippedIndex}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Selection] Failed to load: {ex.Message}");
            _equippedIndex = 0;
            PreviewCannon(0);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Preview
    // ═════════════════════════════════════════════════════════════════════════

    private void PreviewCannon(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (_previewedIndex == index) return;

        if (_previewedIndex >= 0 && _previewedIndex < cannonItems.Count)
            SetCannonBg(_previewedIndex, false);

        _previewedIndex = index;
        SetCannonBg(_previewedIndex, true);

        UpdatePreviewDisplay(index);
    }

    private void UpdatePreviewDisplay(int index)
    {
        if (index < 0 || index >= cannonHolderSO.cannonsData.Length) return;

        var data = cannonHolderSO.cannonsData[index];
        bool unlocked = CannonLockManager.Instance != null
                        && CannonLockManager.Instance.IsUnlocked(index);

        // Cannon image
        if (cannonImage != null && cannonItems[index].cannonSprite != null)
            cannonImage.sprite = cannonItems[index].cannonSprite;

        // Cannon name
        if (cannonNameText != null)
            cannonNameText.text = data.cannonName;

        // Refresh the single action button
        RefreshActionButton(index, unlocked);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Action Button
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the single button's text and interactable state based on cannon state.
    ///
    /// Priority order (first match wins):
    ///   1. Saving in progress  → "SAVING..."      disabled
    ///   2. No preview          → "SELECT"         disabled
    ///   3. Locked, chapter not met → "Ch.X Required"  disabled
    ///   4. Locked, can afford both → "UNLOCK"     enabled
    ///   5. Locked, can't afford    → "UNLOCK"     disabled
    ///   6. Unlocked, equipped  → "EQUIPPED"       disabled
    ///   7. Unlocked, not equipped → "EQUIP"        enabled
    /// </summary>
    private void RefreshActionButton(int index, bool unlocked)
    {
        if (actionButtonText == null) return;

        // 1 — Saving
        if (_isSaving)
        {
            actionButtonText.text = "SAVING...";
            actionButton.interactable = false;
            return;
        }

        // 2 — No preview
        if (index < 0)
        {
            actionButtonText.text = "SELECT";
            actionButton.interactable = false;
            return;
        }

        // 3 & 4 & 5 — Locked
        if (!unlocked)
        {
            bool chapterMet = CannonLockManager.Instance != null
                              && CannonLockManager.Instance.IsChapterRequirementMet(index);

            if (!chapterMet)
            {
                int reqChapter = CannonLockManager.Instance != null
                                 ? CannonLockManager.Instance.GetRequiredChapter(index) + 1 // display 1-based
                                 : 1;
                actionButtonText.text = $"Ch.{reqChapter} Required";
                actionButton.interactable = false;
            }
            else
            {
                // Chapter is met — always allow the player to tap UNLOCK and see the popup.
                // Whether they can actually afford it is checked on popup Confirm,
                // not here. Disabling the button here would prevent players from even
                // seeing what the cannon costs.
                actionButtonText.text = "UNLOCK";
                actionButton.interactable = true;
            }
            return;
        }

        // 6 & 7 — Unlocked
        if (index == _equippedIndex)
        {
            actionButtonText.text = "EQUIPPED";
            actionButton.interactable = false;
        }
        else
        {
            actionButtonText.text = "EQUIP";
            actionButton.interactable = true;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Action Button Press
    // ═════════════════════════════════════════════════════════════════════════

    private void OnActionButtonPressed()
    {
        if (_previewedIndex < 0 || _isSaving) return;

        bool unlocked = CannonLockManager.Instance != null
                        && CannonLockManager.Instance.IsUnlocked(_previewedIndex);

        if (unlocked)
        {
            // ── EQUIP ──────────────────────────────────────────────────────────
            if (_previewedIndex == _equippedIndex) return;

            _equippedIndex = _previewedIndex;
            RefreshActionButton(_equippedIndex, true);
            SaveEquippedData().Forget();

            OnEquipped?.Invoke(_equippedIndex);
            Debug.Log($"[Selection] Equipped cannon: {_equippedIndex}");
        }
        else
        {
            // ── Show Unlock Popup ──────────────────────────────────────────────
            ShowUnlockPopup(_previewedIndex);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Unlock Popup
    // ═════════════════════════════════════════════════════════════════════════

    private void ShowUnlockPopup(int index)
    {
        if (unlockPopupPanel == null || CannonLockManager.Instance == null) return;

        var data = cannonHolderSO.cannonsData[index];
        int coinCost = CannonLockManager.Instance.GetCoinCost(index);
        int gemCost = CannonLockManager.Instance.GetGemCost(index);

        // Populate popup fields
        // Sprite is fetched directly from CannonItemUI — same source as the main preview
        if (popupCannonImage != null)
            popupCannonImage.sprite = cannonItems[index].cannonSprite;

        if (popupCannonNameText != null)
            popupCannonNameText.text = data.cannonName;

        if (popupCoinCostText != null)
            popupCoinCostText.text = coinCost == 0 ? "Free" : $"{coinCost:N0} Gold";

        if (popupGemCostText != null)
            popupGemCostText.text = gemCost == 0 ? "" : $"{gemCost} Gems";

        unlockPopupPanel.SetActive(true);
    }

    private void OnPopupCancelled()
    {
        if (unlockPopupPanel != null)
            unlockPopupPanel.SetActive(false);
    }

    private void OnPopupConfirmed()
    {
        if (_isUnlocking) return; // prevent double-tap
        ConfirmUnlockAsync(_previewedIndex).Forget();
    }

    private async UniTaskVoid ConfirmUnlockAsync(int index)
    {
        _isUnlocking = true;

        // Disable confirm button during the async spend to prevent double-tap
        if (popupConfirmButton != null)
            popupConfirmButton.interactable = false;

        // Hide popup immediately so the player gets instant feedback
        if (unlockPopupPanel != null)
            unlockPopupPanel.SetActive(false);

        // TryUnlock handles: CanUnlock check → SpendMultiple (Gold + Gems) → ExecuteUnlock → cloud save → event
        bool success = await CannonLockManager.Instance.TryUnlock(index);

        if (!success)
        {
            // Currency may have changed between popup open and confirm (e.g. race condition)
            // Refresh the button so the player sees the correct disabled state
            bool unlocked = CannonLockManager.Instance.IsUnlocked(index);
            RefreshActionButton(index, unlocked);

            Debug.LogWarning("[Selection] Unlock failed after popup confirm — balances resynced.");
        }
        // On success, OnCannonUnlocked event fires → OnCannonUnlockedHandler refreshes everything automatically

        if (popupConfirmButton != null)
            popupConfirmButton.interactable = true;

        _isUnlocking = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Visual Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private void SetCannonBg(int index, bool selected)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (cannonItems[index].backgroundImage == null) return;
        cannonItems[index].backgroundImage.sprite = selected ? activeBgSprite : inactiveBgSprite;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Save Equipped
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid SaveEquippedData()
    {
        _isSaving = true;
        RefreshActionButton(_previewedIndex,
            CannonLockManager.Instance != null && CannonLockManager.Instance.IsUnlocked(_previewedIndex));

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(SELECTED_CANNON_KEY, _equippedIndex);
            Debug.Log("[Selection] Saved equipped cannon to cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Selection] Failed to save: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
            RefreshActionButton(_previewedIndex,
                CannonLockManager.Instance != null && CannonLockManager.Instance.IsUnlocked(_previewedIndex));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════════════

    public static async UniTask<int> GetEquippedCannonIndex()
        => await CloudSaveManager.Instance.LoadValueAsync<int>(SELECTED_CANNON_KEY, 0);
}

// ─────────────────────────────────────────────────────────────────────────────
// Data class — assign in Inspector (one entry per cannon scroll-list button)
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class CannonItemUI
{
    public Button button;
    public Sprite cannonSprite;
    public Image backgroundImage;
    public TextMeshProUGUI levelText;
}





