using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ═══════════════════════════════════════════════════════════════════════════════
//  CANNON SELECTION & UPGRADE MANAGER  —  Fully Self-Contained
// ═══════════════════════════════════════════════════════════════════════════════
//
//  This single script owns THREE responsibilities that were previously split
//  across CannonSelectionManager, CannonUpgradeManager, and CannonLockManager:
//
//    1. SELECTION   — tap a cannon item to preview it, equip it
//    2. UPGRADE     — spend gold to level up a cannon
//    3. LOCK/UNLOCK — chapter-gated unlocking with gold + gem costs
//
//  After dropping this into your project you can DELETE:
//    • CannonUpgradeManager.cs
//    • CannonLockManager.cs
//    • CannonUpgradeItemUI (class)
//
// ─── HOW EACH CANNON ITEM IS STRUCTURED (hierarchy) ─────────────────────────
//
//   [Button]  ← root, assigned in cannonItems list
//     ├── CannonImg      → Image   – the cannon sprite
//     ├── CannonBg       → Image   – background (normal / selected sprite)
//     ├── Frame          → Image   – frame border (normal / selected sprite)
//     └── LockImage      → GameObject – lock icon overlay (active = locked)
//
//   Child references are auto-wired by name if left null in the Inspector.
//
// ─── BUTTON STATE MACHINE ───────────────────────────────────────────────────
//
//   EQUIP BUTTON:
//     "Ch.X Required" → chapter gate not met          (disabled)
//     "UNLOCK"        → chapter met, still locked     (enabled → opens popup)
//     "EQUIP"         → unlocked, not equipped        (enabled)
//     "EQUIPPED"      → unlocked, currently equipped  (disabled)
//     "SAVING..."     → cloud save in progress        (disabled)
//
//   UPGRADE BUTTON:
//     "UPGRADE"       → unlocked, below max level     (enabled)
//     "MAX LEVEL"     → at cap                        (disabled)
//     "UPGRADING..."  → save in progress              (disabled)
//     hidden/disabled → cannon is locked              (disabled)
//
// ─── FILL BAR SYSTEM (10-segment level-based) ───────────────────────────────
//
//   Level 1 = 10%,  Level 5 = 50%,  Level 10 = 100%.
//   CurrentFill (bright green) = current level × 10%.
//   NextFill    (light green)  = (current level + 1) × 10% — preview.
//   On upgrade, CurrentFill animates smoothly to the next segment.
//   All three bars (damage, health, fire rate) fill identically because
//   they represent overall cannon level, not individual stats.
//
//   SPRITE SETUP (do this once in Unity Editor):
//     1. Open each fill sprite in Sprite Editor → set Border L/R for caps.
//     2. Inspector: Image Type → Sliced on all fill Images.
//     3. Hierarchy: NextFill first (behind), CurrentFill second (in front).
//     4. Both anchored stretch: anchorMin(0,0) anchorMax(1,1), offsets zero.
//
// ─── COST RULES ─────────────────────────────────────────────────────────────
//
//   Base costs (1× multiplier):
//     L1→2: 500   L2→3: 800    L3→4: 1,200  L4→5: 1,800  L5→6: 2,700
//     L6→7: 4,000 L7→8: 6,000  L8→9: 9,000  L9→10: 13,500
//     Beyond L10: previous cost × 1.5, rounded to nearest 50
//
//   Multipliers:
//     Index 4 – Big Bartha    → ×2
//     Index 6 – Triple Bullet → ×3
//     All others              → ×1
//
// ═══════════════════════════════════════════════════════════════════════════════

public class CannonSelectionAndUpgradeManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════════
    // Singleton
    // ═════════════════════════════════════════════════════════════════════════

    public static CannonSelectionAndUpgradeManager Instance { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Data
    // ═════════════════════════════════════════════════════════════════════════
    [Header("Button Animator")]
    [SerializeField] private ButtonAnimator buttonAnimator;
    [Header("Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Cannon Items (one per cannon, order matches SO array)")]
    [SerializeField] private List<CannonItemUI> cannonItems;

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Top Preview Panel
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Top Panel – Preview")]
    [SerializeField] private Image previewCannonImage;
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewLevelText;
    [SerializeField] private TextMeshProUGUI previewDescriptionText;

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Stat Fill Bars
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Stat Fills – Current (bright green, Image Type = Sliced)")]
    [SerializeField] private Image damageFillCurrent;
    [SerializeField] private Image healthFillCurrent;
    [SerializeField] private Image fireRateFillCurrent;

    [Header("Stat Fills – Next Level Preview (light green, Image Type = Sliced)")]
    [SerializeField] private Image damageFillNext;
    [SerializeField] private Image healthFillNext;
    [SerializeField] private Image fireRateFillNext;

    [Header("Fill Animation")]
    [SerializeField] private float fillAnimDuration = 0.35f;
    [SerializeField] private int maxCannonLevel = 10;

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Equip Button
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Equip Button")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Upgrade Button
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

   

    [Header("Not Enough Gold Text")]
    [SerializeField] private TextMeshProUGUI notEnoughGoldText;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float holdDuration = 1.0f;

    [Header("Cannon Tab Filters")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button allTabButton;
    [SerializeField] private Button unlockedButton;
    [SerializeField] private Button lockedButton;


    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Unlock Popup
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Unlock Popup")]
    [SerializeField] private GameObject unlockPopupPanel;
    [SerializeField] private Image popupCannonImage;
    [SerializeField] private TextMeshProUGUI popupCannonNameText;
    [SerializeField] private TextMeshProUGUI popupCoinCostText;
    [SerializeField] private TextMeshProUGUI popupGemCostText;
    [SerializeField] private Button popupConfirmButton;
    [SerializeField] private Button popupCancelButton;

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Visual Sprites & Materials
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Item Visual Sprites")]
    [SerializeField] private Sprite normalBgSprite;
    [SerializeField] private Sprite selectedBgSprite;
    [SerializeField] private Sprite normalFrameSprite;
    [SerializeField] private Sprite selectedFrameSprite;

    [Header("Grayscale Material (assign your grayscale UI material)")]
    [SerializeField] private Material grayscaleMaterial;

    // ═════════════════════════════════════════════════════════════════════════
    // Inspector — Lock/Unlock Configuration
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Lock Defaults")]
    [Tooltip("Cannon indices that start unlocked (e.g. index 0 = starter cannon)")]
    [SerializeField] private int[] defaultUnlockedIndices = { 0 };

    [Header("Dev Override")]
    [Tooltip("Tick to unlock ALL cannons instantly — never ship with this enabled")]
    [SerializeField] private bool devUnlockAll = false;

    // ═════════════════════════════════════════════════════════════════════════
    // Constants
    // ═════════════════════════════════════════════════════════════════════════

    private const string SELECTED_CANNON_KEY = "SelectedCannonIndex";
    private const string CANNON_LEVEL_SUFFIX = "_CannonLevel";
    private const string UNLOCK_KEY_PREFIX = "CannonUnlocked_";

    private static readonly int[] BaseLevelCosts =
    {
        500, 800, 1200, 1800, 2700, 4000, 6000, 9000, 13500,
    };

    private static readonly Dictionary<int, float> CostMultipliers = new()
    {
        { 4, 2f }, // Big Bartha
        { 6, 3f }, // Triple Bullet
    };

    // ═════════════════════════════════════════════════════════════════════════
    // Runtime State
    // ═════════════════════════════════════════════════════════════════════════

    private int _selectedIndex = -1;
    private int _equippedIndex = -1;
    private bool _isSaving = false;
    private bool _isUpgrading = false;
    private bool _isUnlocking = false;
    private bool _isDataLoaded = false;
    private float _prevFillRatio = 0f;

    // Lock state — one entry per cannon
    private bool[] _unlockedState;

    // Cached cloud keys — built once, never rebuilt
    private string[] _cachedLevelKeys;
    private string[] _cachedUnlockKeys;

    // CancellationToken tied to this GameObject's lifetime.
    // Every async method uses this so tasks cancel cleanly on destroy.
    private CancellationToken _destroyCT;

    // ═════════════════════════════════════════════════════════════════════════
    // Events
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Fires when the player equips a cannon. Payload = cannon index.</summary>
    public static event Action<int> OnEquipped;

    /// <summary>Fires when any cannon is unlocked. Payload = cannon index.</summary>
    public static event Action<int> OnCannonUnlocked;

    /// <summary>Fires when any cannon is upgraded. Payload = cannon index.</summary>
    public static event Action<int> OnCannonUpgraded;

    /// <summary>Fires once after ALL cloud data (levels + unlocks + equipped) is loaded.</summary>
    public static event Action OnAllDataLoaded;

    // ═════════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // ── Singleton ─────────────────────────────────────────────────────
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ── Grab the cancellation token ONCE ──────────────────────────────
        _destroyCT = this.GetCancellationTokenOnDestroy();

        // ── Build cached cloud keys ONCE ──────────────────────────────────
        int count = cannonHolderSO != null ? cannonHolderSO.cannonsData.Length : 0;
        _unlockedState = new bool[count];
        _cachedLevelKeys = new string[count];
        _cachedUnlockKeys = new string[count];

        for (int i = 0; i < count; i++)
        {
            string safeName = cannonHolderSO.cannonsData[i].cannonName.Replace(" ", "");
            _cachedLevelKeys[i] = safeName + CANNON_LEVEL_SUFFIX;
            _cachedUnlockKeys[i] = UNLOCK_KEY_PREFIX + i;
        }

        // ── Wire button listeners ─────────────────────────────────────────
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipPressed);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradePressed);
        if (popupConfirmButton != null) popupConfirmButton.onClick.AddListener(OnPopupConfirmed);
        if (popupCancelButton != null) popupCancelButton.onClick.AddListener(OnPopupCancelled);

        if (allTabButton != null) allTabButton.onClick.AddListener(() => FilterCannonItems("All"));

        if (lockedButton != null) lockedButton.onClick.AddListener(() => FilterCannonItems("Locked"));
        if (unlockedButton != null) unlockedButton.onClick.AddListener(() => FilterCannonItems("Unlocked"));


        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);

        if (notEnoughGoldText != null)
        {
            notEnoughGoldText.text = "Not enough Gold!";
            SetTextAlpha(notEnoughGoldText, 0f);
        }
    }

    private void FilterCannonItems(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            filter = "All";

        switch (filter)
        {
            case "All":
                SetAllCannonItemsActive(true);
                break;

            case "Locked":
                SetCannonItemsActiveByLockState(unlocked: false);
                break;

            case "Unlocked":
                SetCannonItemsActiveByLockState(unlocked: true);
                break;

            default:
                SetAllCannonItemsActive(true);
                break;
        }
    }

    private void SetAllCannonItemsActive(bool active)
    {
        foreach (var item in cannonItems)
            if (item.button != null)
                item.button.gameObject.SetActive(active);
    }

    private void SetCannonItemsActiveByLockState(bool unlocked)
    {
        for (int i = 0; i < cannonItems.Count; i++)
        {
            bool isUnlocked = IsUnlocked(i);
            bool shouldShow = isUnlocked == unlocked;
            
            if (cannonItems[i].button != null)
                cannonItems[i].button.gameObject.SetActive(shouldShow);
        }
    }
    private void OnEnable()
    {
        ResetScrollAndTabs();
    }

    private void ResetScrollAndTabs()
    {
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
        // Reset tab buttons to "All"
        if (allTabButton != null)
            allTabButton.onClick.Invoke();
    }

    private void Start()
    {
        CacheAllBaseValues();
        InitializeCannonItems();
        LoadAllDataAsync().Forget();
    }

    private void OnDestroy()
    {
        // ── Remove listeners ──────────────────────────────────────────────
        if (equipButton != null) equipButton.onClick.RemoveListener(OnEquipPressed);
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnUpgradePressed);
        if (popupConfirmButton != null) popupConfirmButton.onClick.RemoveListener(OnPopupConfirmed);
        if (popupCancelButton != null) popupCancelButton.onClick.RemoveListener(OnPopupCancelled);

        foreach (var item in cannonItems)
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();

        // ── Null static events to prevent ghost listeners across scenes ───
        if (Instance == this)
        {
            OnEquipped = null;
            OnCannonUnlocked = null;
            OnCannonUpgraded = null;
            OnAllDataLoaded = null;
            Instance = null;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Initialization
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Wire up each cannon item button and auto-find child references
    /// (CannonImg, CannonBg, Frame, LockImage) by name if not manually assigned.
    /// </summary>
    private void InitializeCannonItems()
    {
        if (cannonHolderSO == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (i >= cannonHolderSO.cannonsData.Length) break;

            var item = cannonItems[i];
            int index = i; // capture for closure

            if (item.button != null)
            {
                Transform root = item.button.transform;

                // Auto-wire child references if left null in Inspector
                if (item.cannonImgComp == null) item.cannonImgComp = FindChildImage(root, "CannonImg");
                if (item.cannonBgComp == null) item.cannonBgComp = FindChildImage(root, "CannonBg");
                if (item.frameComp == null) item.frameComp = FindChildImage(root, "Frame");
                if (item.lockImageObj == null)
                {
                    Transform lt = root.Find("LockImage");
                    if (lt != null) item.lockImageObj = lt.gameObject;
                }
                if (item.unlockRequirementContainer == null)
                {
                    Transform urt = root.Find("UnlockRequirementContainer");
                    if (urt != null)
                    {
                        item.unlockRequirementContainer = urt.gameObject;
                        item.unlockRequirementText = urt.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
                // Start disabled — RefreshAllLockVisuals() will enable
                // unlocked ones after cloud data finishes loading.
                item.button.interactable = true;
                item.button.onClick.AddListener(() => SelectCannon(index));
            }
          
        }
    }

    private static Image FindChildImage(Transform root, string childName)
    {
        Transform t = root.Find(childName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private void CacheAllBaseValues()
    {
        if (cannonHolderSO == null) return;
        foreach (var cannon in cannonHolderSO.cannonsData)
            cannon.CacheBaseValues();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Load Everything (levels + unlock states + equipped index)
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid LoadAllDataAsync()
    {
        if (cannonHolderSO == null) return;

        int count = cannonHolderSO.cannonsData.Length;

        try
        {
            // ── Gather all keys we need in one batch ──────────────────────
            var keys = new HashSet<string> { SELECTED_CANNON_KEY };
            for (int i = 0; i < count; i++)
            {
                keys.Add(_cachedLevelKeys[i]);
                keys.Add(_cachedUnlockKeys[i]);
            }

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // ── Replay cannon levels ──────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                string key = _cachedLevelKeys[i];
                int savedLevel = data.ContainsKey(key)
                    ? data[key].Value.GetAs<int>()
                    : 0;
                cannonHolderSO.cannonsData[i].ReplayToLevel(savedLevel);
            }

            // ── Restore unlock states ─────────────────────────────────────
            if (devUnlockAll)
            {
                // Dev override — unlock everything without touching cloud
                for (int i = 0; i < count; i++)
                    _unlockedState[i] = true;
            }
            else
            {
                // Apply defaults first (starter cannons)
                if (defaultUnlockedIndices != null)
                    foreach (int idx in defaultUnlockedIndices)
                        if (idx >= 0 && idx < count)
                            _unlockedState[idx] = true;

                // Overlay cloud data on top
                for (int i = 0; i < count; i++)
                {
                    string key = _cachedUnlockKeys[i];
                    if (data.ContainsKey(key))
                        _unlockedState[i] = data[key].Value.GetAs<bool>();
                }
            }

            // ── Restore equipped index ────────────────────────────────────
            _equippedIndex = data.ContainsKey(SELECTED_CANNON_KEY)
                ? Mathf.Clamp(data[SELECTED_CANNON_KEY].Value.GetAs<int>(), 0, count - 1)
                : 0;

            Debug.Log($"[CannonManager] Cloud data loaded. Equipped: {_equippedIndex}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonManager] Load failed: {ex.Message}");
            _equippedIndex = 0;

            // Apply defaults as fallback
            if (defaultUnlockedIndices != null)
                foreach (int idx in defaultUnlockedIndices)
                    if (idx >= 0 && idx < count)
                        _unlockedState[idx] = true;
        }

        _isDataLoaded = true;

        // ── Refresh all visuals ───────────────────────────────────────────

        RefreshAllLockVisuals();
        SelectCannon(_equippedIndex >= 0 ? _equippedIndex : 0);

        // ── Notify listeners ──────────────────────────────────────────────
        OnEquipped?.Invoke(_equippedIndex);
        OnAllDataLoaded?.Invoke();

    }

    // ═════════════════════════════════════════════════════════════════════════
    // Lock / Unlock — State Queries
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Is the cannon at this index unlocked?</summary>
    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= _unlockedState.Length) return false;
        return _unlockedState[index];
    }

    /// <summary>True if all cloud data has finished loading.</summary>
    public bool IsReady => _isDataLoaded;

    /// <summary>
    /// Returns the chapter index required to unlock this cannon.
    /// The value comes from CannonUpgrade_SO.unlockChapterRequired.
    /// </summary>
    public int GetRequiredChapter(int index)
    {
        if (!ValidateIndex(index)) return 0;
        return cannonHolderSO.cannonsData[index].unlockChapterRequired;
    }

    /// <summary>
    /// True if the player has cleared the chapter needed for this cannon.
    /// Falls back to true if ChapterUnlockManager is not present
    /// (so the cannon panel doesn't silently break in editor/test scenes).
    /// </summary>
    public bool IsChapterRequirementMet(int index)
    {
        return true; // TEMP OVERRIDE — disable chapter gating for now while we test other systems.
        if (!ValidateIndex(index)) return false;

        int requiredChapter = cannonHolderSO.cannonsData[index].unlockChapterRequired;

        if (ChapterUnlockManager.Instance == null)
        {
            Debug.LogWarning("[CannonManager] ChapterUnlockManager not found — chapter gate bypassed.");
            return true;
        }

        return ChapterUnlockManager.Instance.IsUnlocked(requiredChapter);
    }

    /// <summary>Gold cost to unlock this cannon (from SO).</summary>
    public int GetCoinCost(int index)
        => ValidateIndex(index) ? cannonHolderSO.cannonsData[index].coinCost : 0;

    /// <summary>Gem cost to unlock this cannon (from SO).</summary>
    public int GetGemCost(int index)
        => ValidateIndex(index) ? cannonHolderSO.cannonsData[index].gemCost : 0;

    // ═════════════════════════════════════════════════════════════════════════
    // Lock / Unlock — Visual Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshAllLockVisuals()
    {
        for (int i = 0; i < cannonItems.Count; i++)
        {
            bool unlocked = _unlockedState.Length > i && _unlockedState[i];

            if (unlocked)
                ApplyUnlockedVisual(i);
            else
                ApplyLockedVisual(i);

            // ALL cannons are tappable so the player can preview them.
            // The equip button handles the actual lock state (UNLOCK / Ch.X Required).
            if (cannonItems[i].button != null)
                cannonItems[i].button.interactable = true;

            RefreshUnlockRequirement(i, unlocked);
        }
    }

    private void RefreshUnlockRequirement(int index, bool unlocked)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        var item = cannonItems[index];

        if (item.unlockRequirementContainer == null) return;

        if (unlocked)
        {
            item.unlockRequirementContainer.SetActive(false);
        }
        else
        {
            bool chapterMet = IsChapterRequirementMet(index);
            if (!chapterMet)
            {
                int requiredChapter = GetRequiredChapter(index) + 1; // 1-based display
                item.unlockRequirementContainer.SetActive(true);
                if (item.unlockRequirementText != null)
                    item.unlockRequirementText.text = $"Unlocks at Ch. {requiredChapter}";
            }
            else
            {
                // Chapter met but not yet purchased — hide the requirement text
                item.unlockRequirementContainer.SetActive(false);
            }
        }
    }

    /// <summary>Apply grayscale material to all Graphic children + show lock icon.</summary>
    private void ApplyLockedVisual(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        var item = cannonItems[index];
        if (item.button == null) return;

        SetMaterialOnAllGraphics(item.button.transform, grayscaleMaterial);

        if (item.lockImageObj != null)
            item.lockImageObj.SetActive(true);
    }

    /// <summary>Remove grayscale material from all Graphic children + hide lock icon.</summary>
    private void ApplyUnlockedVisual(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        var item = cannonItems[index];
        if (item.button == null) return;

        SetMaterialOnAllGraphics(item.button.transform, null);

        if (item.lockImageObj != null)
            item.lockImageObj.SetActive(false);
    }

    /// <summary>
    /// Public overload so external UI (e.g. a second panel showing cannons)
    /// can apply the same locked/unlocked visual without duplicating logic.
    /// </summary>
    public void ApplyLockedVisual(Transform buttonTransform)
        => SetMaterialOnAllGraphics(buttonTransform, grayscaleMaterial);

    public void ApplyUnlockedVisual(Transform buttonTransform)
        => SetMaterialOnAllGraphics(buttonTransform, null);

    private static void SetMaterialOnAllGraphics(Transform root, Material mat)
    {
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.material = mat;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Lock / Unlock — Try Unlock
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to unlock a cannon. Checks chapter gate, then spends gold + gems.
    /// Returns true on success.
    /// </summary>
    public async UniTask<bool> TryUnlock(int index)
    {
        if (!ValidateIndex(index)) return false;
        if (_unlockedState[index]) return false; // already unlocked

        if (!IsChapterRequirementMet(index))
        {
            Debug.Log($"[CannonManager] Cannot unlock {index}: chapter not cleared.");
            return false;
        }

        var data = cannonHolderSO.cannonsData[index];
        bool isFree = data.coinCost == 0 && data.gemCost == 0;

        if (!isFree)
        {
            // Spend both currencies atomically
            bool spent = await CurrencyManager.Instance.SpendMultiple(
                (CurrencyType.Gold, data.coinCost),
                (CurrencyType.Gems, data.gemCost)
            );

            if (!spent)
            {
                Debug.LogWarning($"[CannonManager] Unlock failed — insufficient funds for cannon {index}.");
                return false;
            }
        }

        // ── Mark unlocked ─────────────────────────────────────────────────
        _unlockedState[index] = true;
        ApplyUnlockedVisual(index);
        RefreshUnlockRequirement(index, true);


        // ── Re-enable the button now that it's unlocked ───────────────────
        //if (index < cannonItems.Count && cannonItems[index].button != null)
        //    cannonItems[index].button.interactable = true;

        // ── Save to cloud ─────────────────────────────────────────────────
        SaveUnlockAsync(index).Forget();

        // ── Notify listeners ──────────────────────────────────────────────
        OnCannonUnlocked?.Invoke(index);

        // ── Refresh top panel if this cannon is currently selected ─────────
        if (_selectedIndex == index)
            UpdateTopPanel(index);

        Debug.Log($"[CannonManager] '{data.cannonName}' unlocked — Cost: {data.coinCost}g + {data.gemCost} gems.");
        return true;
    }

    /// <summary>
    /// Bypasses all checks. Use ONLY for admin / IAP / reward grant flows.
    /// </summary>
    public void ForceUnlock(int index)
    {
        if (!ValidateIndex(index) || _unlockedState[index]) return;

        _unlockedState[index] = true;
        ApplyUnlockedVisual(index);
        RefreshUnlockRequirement(index, true);


        // Re-enable the button
        //if (index < cannonItems.Count && cannonItems[index].button != null)
        //    cannonItems[index].button.interactable = true;

        SaveUnlockAsync(index).Forget();
        OnCannonUnlocked?.Invoke(index);

        if (_selectedIndex == index)
            UpdateTopPanel(index);

        Debug.Log($"[CannonManager] Cannon {index} force-unlocked.");
    }

    private async UniTaskVoid SaveUnlockAsync(int index)
    {
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(_cachedUnlockKeys[index], true);
            Debug.Log($"[CannonManager] Unlock saved for index {index}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonManager] Failed to save unlock: {ex.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Level Text Helpers
    // ═════════════════════════════════════════════════════════════════════════





    // ═════════════════════════════════════════════════════════════════════════
    // Selection
    // ═════════════════════════════════════════════════════════════════════════

    private void SelectCannon(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;

        // Allow selection of locked cannons too — the top panel preview
        // will show their UNLOCK / Ch.X Required button state.
        // (removed the IsUnlocked guard here)

        if (_selectedIndex >= 0 && _selectedIndex < cannonItems.Count)
            SetItemVisualState(_selectedIndex, false);

        _selectedIndex = index;
        SetItemVisualState(_selectedIndex, true);

        UpdateTopPanel(index);
    }

    private void SetItemVisualState(int index, bool selected)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        var item = cannonItems[index];

        if (item.cannonBgComp != null)
            item.cannonBgComp.sprite = selected ? selectedBgSprite : normalBgSprite;

        if (item.frameComp != null)
            item.frameComp.sprite = selected ? selectedFrameSprite : normalFrameSprite;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Top Panel — Updates preview image, name, level, description, fills, buttons
    // ═════════════════════════════════════════════════════════════════════════

    private void UpdateTopPanel(int index)
    {
        if (index < 0 || index >= cannonHolderSO.cannonsData.Length) return;

        var cannonData = cannonHolderSO.cannonsData[index];
        bool unlocked = IsUnlocked(index);

        // ── Cannon image ──────────────────────────────────────────────────
        if (previewCannonImage != null)
        {
            previewCannonImage.sprite = cannonData.cannonSprite;
            previewCannonImage.material = unlocked ? null : grayscaleMaterial;
        }

        // ── Text fields ───────────────────────────────────────────────────
        if (previewNameText != null) previewNameText.text = cannonData.cannonName;
        if (previewLevelText != null) previewLevelText.text = $"Level {cannonData.cannonLevel}";
        if (previewDescriptionText != null) previewDescriptionText.text = cannonData.cannonDescription;

        // ── Fill bars ─────────────────────────────────────────────────────
        SnapFills(index);

        // ── Buttons ───────────────────────────────────────────────────────
        RefreshEquipButton(index, unlocked);
        RefreshUpgradeButton(index, unlocked);

    }

    // ═════════════════════════════════════════════════════════════════════════
    // Equip Button
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshEquipButton(int index, bool unlocked)
    {
        if (equipButton == null || equipButtonText == null) return;

        if (_isSaving)
        {
            equipButtonText.text = "SAVING...";
            equipButton.interactable = false;
            StopPulse(equipButton.gameObject);
            return;
        }

        if (!unlocked)
        {
            bool chapterMet = IsChapterRequirementMet(index);

            if (!chapterMet)
            {
                int req = GetRequiredChapter(index) + 1;
                equipButtonText.text = $"Ch.{req} Required";
                equipButton.interactable = false;
                StopPulse(equipButton.gameObject);
            }
            else
            {
                equipButtonText.text = "UNLOCK";
                equipButton.interactable = true;
                StopPulse(equipButton.gameObject);
            }
            return;
        }

        if (index == _equippedIndex)
        {
            equipButtonText.text = "EQUIPPED";
            equipButton.interactable = false;
            StopPulse(equipButton.gameObject);
        }
        else
        {
            equipButtonText.text = "EQUIP";
            equipButton.interactable = true;
            StartPulse(equipButton.gameObject);
        }
    }

    private void OnEquipPressed()
    {
        if (_selectedIndex < 0 || _isSaving) return;

        bool unlocked = IsUnlocked(_selectedIndex);

        if (unlocked)
        {
            if (_selectedIndex == _equippedIndex) return;

            _equippedIndex = _selectedIndex;
            RefreshEquipButton(_equippedIndex, true);
            SaveEquippedAsync().Forget();
            OnEquipped?.Invoke(_equippedIndex);
            Debug.Log($"[CannonManager] Equipped cannon: {_equippedIndex}");
        }
        else
        {
            // Locked but chapter requirement met → show unlock popup
            ShowUnlockPopup(_selectedIndex);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Upgrade Button
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshUpgradeButton(int index, bool unlocked)
    {
        if (upgradeButton == null) return;

        if (!unlocked)
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
            upgradeButton.interactable = false;
            StopPulse(upgradeButton.gameObject);
            return;
        }

        if (_isUpgrading)
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADING...";
            upgradeButton.interactable = false;
            StopPulse(upgradeButton.gameObject);
            return;
        }

        if (index < 0 || index >= cannonHolderSO.cannonsData.Length)
        {
            upgradeButton.interactable = false;
            StopPulse(upgradeButton.gameObject);
            return;
        }

        var data = cannonHolderSO.cannonsData[index];
        if (data.cannonLevel >= maxCannonLevel)
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
            StopPulse(upgradeButton.gameObject);
        }
        else
        {
            int cost = GetUpgradeCost(index);
            bool canAfford = CurrencyManager.Instance != null
                             && CurrencyManager.Instance.Gold >= cost;

            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
            upgradeButton.interactable = canAfford;

            if (canAfford)
                StartPulse(upgradeButton.gameObject);
            else
                StopPulse(upgradeButton.gameObject);
        }
    }



    private void OnUpgradePressed()
    {
        if (_selectedIndex < 0 || _isUpgrading) return;

        bool unlocked = IsUnlocked(_selectedIndex);
        if (!unlocked) return;

        var data = cannonHolderSO.cannonsData[_selectedIndex];
        if (data.cannonLevel >= maxCannonLevel) return;

        ExecuteUpgradeAsync(_selectedIndex).Forget();
    }

    private async UniTaskVoid ExecuteUpgradeAsync(int index)
    {
        _isUpgrading = true;
        RefreshUpgradeButton(index, true);

        var data = cannonHolderSO.cannonsData[index];
        int cost = GetUpgradeCost(index);

        // ── Spend Gold ────────────────────────────────────────────────────
        bool spent = await CurrencyManager.Instance.SpendGold(cost);

        if (!spent)
        {
            ShowNotEnoughGold().Forget();
            _isUpgrading = false;
            RefreshUpgradeButton(index, true);
            return;
        }

        // ── Apply upgrade locally ─────────────────────────────────────────
        data.UpgradeCannon();
        //RefreshCannonLevelText(index);

        // ── Refresh display + animate fills ───────────────────────────────
        if (index == _selectedIndex)
        {
            if (previewLevelText != null)
                previewLevelText.text = $"Level {data.cannonLevel}";

 
            RefreshUpgradeButton(index, true);
            await AnimateFills(index);
        }

        // ── Save to cloud ─────────────────────────────────────────────────
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(
                _cachedLevelKeys[index], data.cannonLevel);
            Debug.Log($"[CannonManager] {data.cannonName} → Level {data.cannonLevel} saved.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonManager] Failed to save level: {ex.Message}");
        }
        finally
        {
            _isUpgrading = false;
            RefreshUpgradeButton(index, true);
        }

        // ── Notify listeners ──────────────────────────────────────────────
        OnCannonUpgraded?.Invoke(index);
        Debug.Log($"[CannonManager] Upgraded {data.cannonName} to Lv{data.cannonLevel} for {cost}g.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Unlock Popup
    // ═════════════════════════════════════════════════════════════════════════

    private void ShowUnlockPopup(int index)
    {
        if (unlockPopupPanel == null) return;

        var data = cannonHolderSO.cannonsData[index];
        int coinCost = GetCoinCost(index);
        int gemCost = GetGemCost(index);

        if (popupCannonImage != null) popupCannonImage.sprite = cannonItems[index].cannonSprite;
        if (popupCannonNameText != null) popupCannonNameText.text = data.cannonName;
        if (popupCoinCostText != null) popupCoinCostText.text = coinCost == 0 ? "Free" : $"{coinCost:N0} Gold";
        if (popupGemCostText != null) popupGemCostText.text = gemCost == 0 ? "" : $"{gemCost} Gems";

        unlockPopupPanel.SetActive(true);
    }

    private void OnPopupCancelled()
    {
        if (unlockPopupPanel != null)
            unlockPopupPanel.SetActive(false);
    }

    private void OnPopupConfirmed()
    {
        if (_isUnlocking) return;
        ConfirmUnlockAsync(_selectedIndex).Forget();
    }

    private async UniTaskVoid ConfirmUnlockAsync(int index)
    {
        _isUnlocking = true;

        if (popupConfirmButton != null)
            popupConfirmButton.interactable = false;

        if (unlockPopupPanel != null)
            unlockPopupPanel.SetActive(false);

        bool success = await TryUnlock(index);

        if (!success)
        {
            bool unlocked = IsUnlocked(index);
            RefreshEquipButton(index, unlocked);
            Debug.LogWarning("[CannonManager] Unlock failed after confirm.");
        }

        if (popupConfirmButton != null)
            popupConfirmButton.interactable = true;

        _isUnlocking = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Fill Bar System
    // ═════════════════════════════════════════════════════════════════════════

    private float LevelToRatio(int level)
        => level <= 0 ? 0f : Mathf.Clamp01((float)level / maxCannonLevel);

    private void SetFill(Image img, float ratio)
    {
        if (img == null) return;

        if (ratio <= 0f) { img.enabled = false; return; }

        img.enabled = true;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void SnapFills(int index)
    {
        if (index < 0 || index >= cannonHolderSO.cannonsData.Length) return;

        int level = cannonHolderSO.cannonsData[index].cannonLevel;
        float currentRatio = LevelToRatio(level);
        float nextRatio = LevelToRatio(level + 1);
        bool atMax = level >= maxCannonLevel;

        SetFill(damageFillCurrent, currentRatio);
        SetFill(healthFillCurrent, currentRatio);
        SetFill(fireRateFillCurrent, currentRatio);

        SetFill(damageFillNext, atMax ? currentRatio : nextRatio);
        SetFill(healthFillNext, atMax ? currentRatio : nextRatio);
        SetFill(fireRateFillNext, atMax ? currentRatio : nextRatio);

        _prevFillRatio = currentRatio;
    }

    private async UniTask AnimateFills(int index)
    {
        if (index < 0 || index >= cannonHolderSO.cannonsData.Length) return;

        int level = cannonHolderSO.cannonsData[index].cannonLevel;
        float newRatio = LevelToRatio(level);

        float elapsed = 0f;
        while (elapsed < fillAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fillAnimDuration);
            float lerped = Mathf.Lerp(_prevFillRatio, newRatio, t);

            SetFill(damageFillCurrent, lerped);
            SetFill(healthFillCurrent, lerped);
            SetFill(fireRateFillCurrent, lerped);

            await UniTask.Yield(_destroyCT);
        }

        // Snap exact
        SetFill(damageFillCurrent, newRatio);
        SetFill(healthFillCurrent, newRatio);
        SetFill(fireRateFillCurrent, newRatio);

        bool atMax = level >= maxCannonLevel;
        float nextRatio = LevelToRatio(level + 1);
        SetFill(damageFillNext, atMax ? newRatio : nextRatio);
        SetFill(healthFillNext, atMax ? newRatio : nextRatio);
        SetFill(fireRateFillNext, atMax ? newRatio : nextRatio);

        _prevFillRatio = newRatio;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Save Equipped
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid SaveEquippedAsync()
    {
        _isSaving = true;
        bool unlocked = IsUnlocked(_selectedIndex);
        RefreshEquipButton(_selectedIndex, unlocked);

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(SELECTED_CANNON_KEY, _equippedIndex);
            Debug.Log("[CannonManager] Equipped cannon saved to cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonManager] Failed to save equipped: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
            unlocked = IsUnlocked(_selectedIndex);
            RefreshEquipButton(_selectedIndex, unlocked);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Upgrade Cost Calculation
    // ═════════════════════════════════════════════════════════════════════════

    public int GetUpgradeCost(int cannonIndex)
    {
        if (cannonHolderSO == null
            || cannonIndex < 0
            || cannonIndex >= cannonHolderSO.cannonsData.Length)
            return 0;

        int currentLevel = cannonHolderSO.cannonsData[cannonIndex].cannonLevel;
        int baseCost = GetBaseCostForLevel(currentLevel);
        float multiplier = CostMultipliers.TryGetValue(cannonIndex, out float m) ? m : 1f;

        return Mathf.RoundToInt(baseCost * multiplier / 50f) * 50;
    }

    private static int GetBaseCostForLevel(int currentLevel)
    {
        int tableIndex = Mathf.Max(0, currentLevel - 1);

        if (tableIndex < BaseLevelCosts.Length)
            return BaseLevelCosts[tableIndex];

        // Beyond table: multiply last entry by 1.5 for each extra level
        float cost = BaseLevelCosts[BaseLevelCosts.Length - 1];
        int extra = tableIndex - (BaseLevelCosts.Length - 1);
        for (int i = 0; i < extra; i++) cost *= 1.5f;

        return Mathf.RoundToInt(cost / 50f) * 50;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // "Not enough Gold!" Fade
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid ShowNotEnoughGold()
    {
        if (notEnoughGoldText == null) return;

        SetTextAlpha(notEnoughGoldText, 0f);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(notEnoughGoldText, Mathf.Clamp01(elapsed / fadeDuration));
            await UniTask.Yield(_destroyCT);
        }
        SetTextAlpha(notEnoughGoldText, 1f);

        // Hold
        await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: _destroyCT);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(notEnoughGoldText, Mathf.Clamp01(1f - elapsed / fadeDuration));
            await UniTask.Yield(_destroyCT);
        }
        SetTextAlpha(notEnoughGoldText, 0f);
    }

    private static void SetTextAlpha(TextMeshProUGUI tmp, float alpha)
    {
        Color c = tmp.color;
        c.a = alpha;
        tmp.color = c;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════
    private void StartPulse(GameObject target)
    {
        if (buttonAnimator != null)
            buttonAnimator.AttentionPulse(target);
    }

    private void StopPulse(GameObject target)
    {
        if (buttonAnimator != null)
            buttonAnimator.StopAttentionPulse(target);
    }
    private bool ValidateIndex(int index)
        => cannonHolderSO != null && index >= 0 && index < cannonHolderSO.cannonsData.Length;

    // ═════════════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the equipped cannon index from cloud. Use this from other scenes
    /// (e.g. gameplay scene) to know which cannon the player has equipped.
    /// </summary>
    public static async UniTask<int> GetEquippedCannonIndex()
        => await CloudSaveManager.Instance.LoadValueAsync<int>(SELECTED_CANNON_KEY, 0);

    public int GetCurrentSelectedIndex() => _selectedIndex;
    public int GetCurrentEquippedIndex() => _equippedIndex;
    public int GetTotalCannons() => cannonHolderSO != null ? cannonHolderSO.cannonsData.Length : 0;
}

// ═════════════════════════════════════════════════════════════════════════════
// Per-cannon UI data — assign in Inspector
// Auto-wired by name from button's children if left null:
//   CannonImg, CannonBg, Frame, LockImage
// ═════════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class CannonItemUI
{
    [Tooltip("Root button of the cannon item (parent)")]
    public Button button;

    [Tooltip("The cannon's sprite used in popups / preview (assign in Inspector)")]
    public Sprite cannonSprite;



    // Auto-wired by name if left null ↓
    [HideInInspector] public Image cannonImgComp;      // child "CannonImg"
    [HideInInspector] public Image cannonBgComp;       // child "CannonBg"
    [HideInInspector] public Image frameComp;          // child "Frame"
    [HideInInspector] public GameObject lockImageObj;   // child "LockImage"
                                                        // In CannonItemUI class, add:
    [HideInInspector] public GameObject unlockRequirementContainer; // child "UnlockRequirementContainer"
    [HideInInspector] public TextMeshProUGUI unlockRequirementText; // child "UnlockRequirementText"
}