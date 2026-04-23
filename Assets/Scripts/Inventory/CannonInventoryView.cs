using System;
using System.Collections.Generic;
using System.Threading;
using BirdHunter.Inventory;
using BirdHunter.Inventory.Services;
using BirdHunter.Inventory.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CannonInventoryView : MonoBehaviour, IMenuPage
{
    public static CannonInventoryView Instance { get; private set; }
    
    [Header("Service")]
    [SerializeField] private CannonInventoryService service;

    [Header("Button Animator")]
    [SerializeField] private ButtonAnimator buttonAnimator;

    [Header("Items")]
    [SerializeField] private CannonDatabase database;
    [SerializeField] private CannonItem cannonItemPrefab;
    [SerializeField] private Transform itemContainer;

    [Header("Top Panel – Preview")]
    [SerializeField] private Image previewCannonImage;
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewLevelText;
    [SerializeField] private TextMeshProUGUI previewDescriptionText;

    [Header("Stat Fills – Current")]
    [SerializeField] private Image damageFillCurrent;
    [SerializeField] private Image healthFillCurrent;
    [SerializeField] private Image fireRateFillCurrent;

    [Header("Stat Fills – Next Level Preview")]
    [SerializeField] private Image damageFillNext;
    [SerializeField] private Image healthFillNext;
    [SerializeField] private Image fireRateFillNext;

    [Header("Fill Animation")]
    [SerializeField] private float fillAnimDuration = 0.35f;

    [Header("Equip Button")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;

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

    [Header("Unlock Popup")]
    [SerializeField] private GameObject unlockPopupPanel;
    [SerializeField] private GameObject unlockMainPanel;      // Right side (cannon image + name)
    [SerializeField] private RectTransform unlockSidePanel;   // Left side (slides in)
    [SerializeField] private Image popupCannonImage;
    [SerializeField] private TextMeshProUGUI popupCannonNameText;
    [SerializeField] private TextMeshProUGUI popupCoinCostText;
    [SerializeField] private TextMeshProUGUI popupGemCostText;
    [SerializeField] private Button popupConfirmButton;
    [SerializeField] private Button popupCancelButton;

    [Header("Unlock Popup Animation")]
    [Tooltip("Starting X position of side panel (relative to its parent RectTransform).")]
    [SerializeField] private float sidePanelStartX = -180f;
    [Tooltip("Ending X position of side panel (relative to its parent RectTransform).")]
    [SerializeField] private float sidePanelEndX = 175f;
    [Tooltip("Duration of side panel slide-in.")]
    [SerializeField] private float sidePanelSlideDuration = 0.4f;
    [Tooltip("Delay between each side panel child appearing.")]
    [SerializeField] private float childStagger = 0.08f;
    [Tooltip("Duration of each child's pop-in animation.")]
    [SerializeField] private float childPopDuration = 0.25f;
    [Tooltip("Side panel ease curve.")]
    [SerializeField] private Ease slideEase = Ease.OutBack;
    [Tooltip("Child pop ease curve.")]
    [SerializeField] private Ease popEase = Ease.OutBack;

    [Header("Item Visual Sprites")]
    [SerializeField] private Sprite normalBgSprite;
    [SerializeField] private Sprite selectedBgSprite;
    [SerializeField] private Sprite normalFrameSprite;
    [SerializeField] private Sprite selectedFrameSprite;

    [Header("Grayscale Material")]
    [SerializeField] private Material grayscaleMaterial;

    // ── Runtime ────────────────────────────────────────────────────────
    public PageType PageType => PageType.Inventory;

    private readonly List<CannonItem> _items = new();
    private string _selectedKey;
    private float _prevFillRatio;
    private bool _busyEquip;
    private bool _busyUpgrade;
    private bool _busyUnlock;
    private CancellationToken _destroyCT;
    // ── Unlock popup animation state ──
    private List<Transform> _sidePanelChildren = new();
    private Sequence _unlockPopupSequence;
    private bool _sidePanelPositionCached;

    // ════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _destroyCT = this.GetCancellationTokenOnDestroy();

        PopulateItems();

        if (equipButton != null)        equipButton.onClick.AddListener(OnEquipPressed);
        if (upgradeButton != null)      upgradeButton.onClick.AddListener(OnUpgradePressed);
        if (popupConfirmButton != null) popupConfirmButton.onClick.AddListener(OnPopupConfirmed);
        if (popupCancelButton != null)  popupCancelButton.onClick.AddListener(OnPopupCancelled);

        if (allTabButton != null)   allTabButton.onClick.AddListener(() => Filter("All"));
        if (unlockedButton != null) unlockedButton.onClick.AddListener(() => Filter("Unlocked"));
        if (lockedButton != null)   lockedButton.onClick.AddListener(() => Filter("Locked"));

        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);
        CacheUnlockPopupAnimation();

        if (notEnoughGoldText != null)
        {
            notEnoughGoldText.text = "Not enough Gold!";
            SetTextAlpha(notEnoughGoldText, 0f);
        }
    }


    private void OnEnable()
    {
        ResetScreen();

        

        if (service == null) service = CannonInventoryService.Instance;
        if (service != null)
        {
            service.OnReady    += HandleReady;
            service.OnUnlocked += HandleUnlocked;
            service.OnUpgraded += HandleUpgraded;
            service.OnEquipped += HandleEquipped;

            if (service.IsReady) HandleReady();
        }

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        Filter("All");
    }

    private void Start()
    {
        if (PageManager.Instance != null) PageManager.Instance.Register(this);
    }
    // ════════════════════════════════════════════════════════════════════
    // Unlock popup animation
    // ════════════════════════════════════════════════════════════════════

    private void CacheUnlockPopupAnimation()
    {
        if (unlockSidePanel == null) return;

        _sidePanelPositionCached = true;

        // Collect direct children of side panel (these animate in one by one)
        _sidePanelChildren.Clear();
        for (int i = 0; i < unlockSidePanel.childCount; i++)
            _sidePanelChildren.Add(unlockSidePanel.GetChild(i));
    }

    private void PlayUnlockPopupAnimation()
    {
        if (!_sidePanelPositionCached) CacheUnlockPopupAnimation();

        // Kill any existing animation
        _unlockPopupSequence?.Kill();

        // ── Step 1: Show main panel immediately ──
        if (unlockMainPanel != null)
            unlockMainPanel.SetActive(true);

        // ── Step 2: Setup side panel for animation ──
        if (unlockSidePanel != null)
        {
            unlockSidePanel.gameObject.SetActive(true);

            // Start at specified X position (preserves current Y)
            Vector2 currentPos = unlockSidePanel.anchoredPosition;
            unlockSidePanel.anchoredPosition = new Vector2(sidePanelStartX, currentPos.y);

            // Hide all children before animation starts
            foreach (var child in _sidePanelChildren)
            {
                if (child == null) continue;
                child.gameObject.SetActive(false);
                child.localScale = Vector3.zero;
            }
        }

        // ── Step 3: Build the animation sequence ──
        _unlockPopupSequence = DOTween.Sequence();

        // Slide side panel from startX to endX
        if (unlockSidePanel != null)
        {
            _unlockPopupSequence.Append(
                unlockSidePanel.DOAnchorPosX(sidePanelEndX, sidePanelSlideDuration)
                    .SetEase(slideEase)
            );
        }

        // Then reveal children one by one
        for (int i = 0; i < _sidePanelChildren.Count; i++)
        {
            var child = _sidePanelChildren[i];
            if (child == null) continue;

            Transform capturedChild = child;

            _unlockPopupSequence.AppendCallback(() =>
            {
                if (capturedChild == null) return;
                capturedChild.gameObject.SetActive(true);
                capturedChild.localScale = Vector3.zero;
                capturedChild.DOScale(Vector3.one, childPopDuration).SetEase(popEase);
            });

            _unlockPopupSequence.AppendInterval(childStagger);
        }

        _unlockPopupSequence.SetUpdate(true);
    }

    private void StopUnlockPopupAnimation()
    {
        _unlockPopupSequence?.Kill();
        _unlockPopupSequence = null;
    }
    public void OnPageEnter() { /* OnEnable already runs ResetScreen + HandleReady */ }
    public void OnPageExit() { }

    public void ResetScreen()
    {
        // Clear selection
        foreach (var item in _items)
            if (item != null) SetItemSelectedVisual(item.cannonKey, false);
        _selectedKey = null;

        // Clear busy flags so buttons don't stay stuck if user left mid-operation
        _busyEquip   = false;
        _busyUpgrade = false;
        _busyUnlock  = false;

        // Hide popup and feedback text
        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);
        if (notEnoughGoldText != null) SetTextAlpha(notEnoughGoldText, 0f);

        // Reset equip/upgrade button states
        if (equipButton != null)   equipButton.interactable   = true;
        if (upgradeButton != null)
        {
            upgradeButton.interactable = true;
            upgradeButton.GetComponent<Image>().material = null;
        }
            if (equipButtonText != null)   equipButtonText.text   = "EQUIP";
        if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
    }

    private void OnDisable()
    {
        if (PageManager.Instance != null) PageManager.Instance.Unregister(this);

        if (service == null) return;
        service.OnReady    -= HandleReady;
        service.OnUnlocked -= HandleUnlocked;
        service.OnUpgraded -= HandleUpgraded;
        service.OnEquipped -= HandleEquipped;
    }

    private void OnDestroy()
    {
        StopUnlockPopupAnimation();
        if (equipButton != null)        equipButton.onClick.RemoveListener(OnEquipPressed);
        if (upgradeButton != null)      upgradeButton.onClick.RemoveListener(OnUpgradePressed);
        if (popupConfirmButton != null) popupConfirmButton.onClick.RemoveListener(OnPopupConfirmed);
        if (popupCancelButton != null)  popupCancelButton.onClick.RemoveListener(OnPopupCancelled);

        foreach (var item in _items)
            if (item != null && item.button != null) item.button.onClick.RemoveAllListeners();

        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════════════════════════════════
    // Wiring
    // ════════════════════════════════════════════════════════════════════

    private void PopulateItems()
    {
        if (database == null || cannonItemPrefab == null || itemContainer == null) return;
        if (database.entries == null) return;

        foreach (var entry in database.entries)
        {
            var item = Instantiate(cannonItemPrefab, itemContainer);
            if (item.cannonSprite != null) item.cannonSprite.sprite = entry.icon;
            item.cannonKey = entry.cannonKey;
            if (item.button == null) item.button = item.GetComponentInChildren<Button>();

            if (item.button != null)
            {
                item.button.gameObject.SetActive(true);
                item.button.interactable = true;
            }

            WireItem(item);
            _items.Add(item);
        }
    }

    private void WireItem(CannonItem item)
    {
        if (item?.button == null) return;
        string key = item.cannonKey;
        item.button.onClick.AddListener(() => SelectCannon(key));
    }

    private CannonItem FindItem(string key)
    {
        foreach (var it in _items)
            if (it != null && it.cannonKey == key) return it;
        return null;
    }

    // ════════════════════════════════════════════════════════════════════
    // Service event handlers
    // ════════════════════════════════════════════════════════════════════

    private void HandleReady()
    {
        RefreshAllItemVisuals();
        string startKey = service.EquippedKey
            ?? (_items.Count > 0 ? _items[0].cannonKey : null);
        if (startKey != null) SelectCannon(startKey);
        EquipAsync(startKey).Forget();
    }

    private void HandleUnlocked(string key)
    {
        ApplyLockVisual(key, unlocked: true);
        if (key == _selectedKey) UpdatePreview(key);
    }

    private void HandleUpgraded(string key)
    {
        if (key != _selectedKey) return;
        UpdatePreviewText(key);
        AnimateFills(key).Forget();
        RefreshUpgradeButton(key);
    }

    private void HandleEquipped(string key)
    {
        if (_selectedKey != null) RefreshEquipButton(_selectedKey);
    }

    // ════════════════════════════════════════════════════════════════════
    // Selection
    // ════════════════════════════════════════════════════════════════════

    private void SelectCannon(string key)
    {
        if (FindItem(key) == null) return;

        if (_selectedKey != null) SetItemSelectedVisual(_selectedKey, false);
        _selectedKey = key;
        SetItemSelectedVisual(key, true);
        UpdatePreview(key);
    }

    private void SetItemSelectedVisual(string key, bool selected)
    {
        var item = FindItem(key);
        if (item == null) return;
        if (item.cannonBgComp != null)
            item.cannonBgComp.sprite = selected ? selectedBgSprite : normalBgSprite;
        else if (selected)
            Debug.LogWarning($"[CannonInventoryView] {key}: cannonBgComp is null — assign it in the CannonItem prefab.");
        if (item.frameComp != null)
            item.frameComp.sprite = selected ? selectedFrameSprite : normalFrameSprite;
        else if (selected)
            Debug.LogWarning($"[CannonInventoryView] {key}: frameComp is null — assign it in the CannonItem prefab.");
    }

    // ════════════════════════════════════════════════════════════════════
    // Preview panel
    // ════════════════════════════════════════════════════════════════════

    private void UpdatePreview(string key)
    {
        if (service == null) return;
        var dto = service.GetBaseData(key);
        if (dto == null) return;

        bool unlocked = service.IsUnlocked(key);
        var item = FindItem(key);

        if (previewCannonImage != null)
        {
            if (item?.cannonSprite?.sprite != null)
                previewCannonImage.sprite = item.cannonSprite.sprite;
            previewCannonImage.material = unlocked ? null : grayscaleMaterial;
        }

        UpdatePreviewText(key);
        SnapFills(key);
        RefreshEquipButton(key);
        RefreshUpgradeButton(key);
    }

    private void UpdatePreviewText(string key)
    {
        var dto = service.GetBaseData(key);
        if (dto == null) return;
        if (previewNameText != null)        previewNameText.text = dto.displayName ?? dto.name;
        if (previewLevelText != null)       previewLevelText.text = $"Level {service.GetLevel(key)}";
        if (previewDescriptionText != null) previewDescriptionText.text = dto.description;
    }

    // ════════════════════════════════════════════════════════════════════
    // Fill bars (level-based, 1..max → 0..1)
    // ════════════════════════════════════════════════════════════════════

    private float LevelRatio(string key, int level)
    {
        int max = service.GetMaxLevel(key);
        return max <= 0 ? 0f : Mathf.Clamp01((float)level / max);
    }

    private static void SetFill(Image img, float ratio)
    {
        if (img == null) return;
        if (ratio <= 0f) { img.enabled = false; return; }
        img.enabled = true;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void SnapFills(string key)
    {
        int level = service.GetLevel(key);
        float current = LevelRatio(key, level);
        float next = LevelRatio(key, level + 1);
        bool atMax = service.IsMaxLevel(key);

        SetFill(damageFillCurrent, current);
        SetFill(healthFillCurrent, current);
        SetFill(fireRateFillCurrent, current);

        SetFill(damageFillNext, atMax ? current : next);
        SetFill(healthFillNext, atMax ? current : next);
        SetFill(fireRateFillNext, atMax ? current : next);

        _prevFillRatio = current;
    }

    private async UniTask AnimateFills(string key)
    {
        int level = service.GetLevel(key);
        float target = LevelRatio(key, level);
        float elapsed = 0f;

        while (elapsed < fillAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fillAnimDuration);
            float v = Mathf.Lerp(_prevFillRatio, target, t);
            SetFill(damageFillCurrent, v);
            SetFill(healthFillCurrent, v);
            SetFill(fireRateFillCurrent, v);
            await UniTask.Yield(_destroyCT);
        }

        SnapFills(key);
    }

    // ════════════════════════════════════════════════════════════════════
    // Equip button
    // ════════════════════════════════════════════════════════════════════

    private void RefreshEquipButton(string key)
    {
        if (equipButton == null || equipButtonText == null || service == null) return;

        if (_busyEquip)
        {
            equipButtonText.text = "SAVING...";
            equipButton.interactable = false;
            StopPulse(equipButton.gameObject);
            return;
        }

        if (!service.IsUnlocked(key))
        {
            equipButtonText.text = "UNLOCK";
            equipButton.interactable = true;
            StopPulse(equipButton.gameObject);
            return;
        }

        if (service.IsEquipped(key))
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
        if (_selectedKey == null || _busyEquip || service == null) return;

        if (!service.IsUnlocked(_selectedKey))
        {
            ShowUnlockPopup(_selectedKey);
            return;
        }

        EquipAsync(_selectedKey).Forget();
    }

    private async UniTaskVoid EquipAsync(string key)
    {
        _busyEquip = true;
        RefreshEquipButton(key);
        await service.Equip(key);
        _busyEquip = false;
        RefreshEquipButton(key);
    }

    // ════════════════════════════════════════════════════════════════════
    // Upgrade button
    // ════════════════════════════════════════════════════════════════════

    private void RefreshUpgradeButton(string key)
    {
        if (upgradeButton == null || service == null) return;

        if (!service.IsUnlocked(key))
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
            upgradeButton.interactable = false;
            upgradeButton.GetComponent<Image>().material = grayscaleMaterial;
            StopPulse(upgradeButton.gameObject);
            return;
        }

        if (_busyUpgrade)
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADING...";
            upgradeButton.interactable = false;
            StopPulse(upgradeButton.gameObject);
            return;
        }

        if (service.IsMaxLevel(key))
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
            StopPulse(upgradeButton.gameObject);
            return;
        }

        int cost = service.GetUpgradeCoinCost(key);
        bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Gold >= cost;

        if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
        upgradeButton.interactable = canAfford;
        if(canAfford) upgradeButton.GetComponent<Image>().material = null;
        else           upgradeButton.GetComponent<Image>().material = grayscaleMaterial;
        if (canAfford) StartPulse(upgradeButton.gameObject);
        else           StopPulse(upgradeButton.gameObject);
    }

    private void OnUpgradePressed()
    {
        if (_selectedKey == null || _busyUpgrade || service == null) return;
        UpgradeAsync(_selectedKey).Forget();
    }

    private async UniTaskVoid UpgradeAsync(string key)
    {
        _busyUpgrade = true;
        RefreshUpgradeButton(key);

        bool ok = await service.TryUpgrade(key);
        if (!ok) ShowNotEnoughGold().Forget();

        _busyUpgrade = false;
        RefreshUpgradeButton(key);
    }

    // ════════════════════════════════════════════════════════════════════
    // Unlock popup
    // ════════════════════════════════════════════════════════════════════

    private void ShowUnlockPopup(string key)
    {
        if (service == null || unlockPopupPanel == null) return;

        var dto = service.GetBaseData(key);
        if (dto == null) return;

        var item = FindItem(key);
        if (popupCannonImage != null && item?.cannonSprite?.sprite != null)
            popupCannonImage.sprite = item.cannonSprite.sprite;
        if (popupCannonNameText != null) popupCannonNameText.text = dto.name;

        int coinCost = service.GetUnlockCoinCost(key);
        int gemCost = service.GetUnlockGemCost(key);
        if (popupCoinCostText != null) popupCoinCostText.text = coinCost == 0 ? "Free" : $"{coinCost:N0}";
        if (popupGemCostText != null) popupGemCostText.text = gemCost == 0 ? "" : $"{gemCost}";

        // Enable the popup container, then play the animation
        unlockPopupPanel.SetActive(true);
        PlayUnlockPopupAnimation();
    }

    private void OnPopupCancelled()
    {
        StopUnlockPopupAnimation();
        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);
    }

    private void OnPopupConfirmed()
    {
        if (_selectedKey == null || _busyUnlock) return;
        ConfirmUnlockAsync(_selectedKey).Forget();
    }

    private async UniTaskVoid ConfirmUnlockAsync(string key)
    {
        _busyUnlock = true;
        if (popupConfirmButton != null) popupConfirmButton.interactable = false;
        if (unlockPopupPanel != null)   unlockPopupPanel.SetActive(false);

        bool ok = await service.TryUnlock(key);
        if (!ok) ShowNotEnoughGold().Forget();

        StopUnlockPopupAnimation();
        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);
        _busyUnlock = false;
    }

    // ════════════════════════════════════════════════════════════════════
    // Lock visuals + filters
    // ════════════════════════════════════════════════════════════════════

    private void RefreshAllItemVisuals()
    {
        if (service == null) return;
        foreach (var it in _items)
            if (it != null) ApplyLockVisual(it.cannonKey, service.IsUnlocked(it.cannonKey));
    }

    private void ApplyLockVisual(string key, bool unlocked)
    {
        var item = FindItem(key);
        if (item == null) return;

        // Search from the item root, not just button children — cannonBgComp/frameComp
        // may be siblings of the button, not nested inside it.
        foreach (var g in item.GetComponentsInChildren<Graphic>(true))
            g.material = unlocked ? null : grayscaleMaterial;

        if (item.lockImageObj != null) item.lockImageObj.SetActive(!unlocked);

        if (item.unlockRequirementContainer != null)
        {
            if (unlocked)
            {
                item.unlockRequirementContainer.SetActive(false);
            }
            else
            {
                var dto = service?.GetBaseData(key);
                if (dto != null && item.unlockRequirementText != null)
                {
                    item.unlockRequirementContainer.SetActive(true);
                    item.unlockRequirementText.text = $"Unlocks at Ch. {dto.unlockAtChapter}";
                }
                else
                {
                    item.unlockRequirementContainer.SetActive(false);
                }
            }
        }
    }

    public void ApplyLockedVisual(Transform buttonTransform)
        => SetMaterialOnAllGraphics(buttonTransform, grayscaleMaterial);

    public void ApplyUnlockedVisual(Transform buttonTransform)
        => SetMaterialOnAllGraphics(buttonTransform, null);

    private static void SetMaterialOnAllGraphics(Transform root, Material mat)
    {
        if (root == null) return;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.material = mat;
    }

    private void Filter(string filter)
    {
        foreach (var item in _items)
        {
            if (item?.button == null) continue;

            bool show = filter switch
            {
                "Unlocked" => service != null && service.IsUnlocked(item.cannonKey),
                "Locked"   => service == null || !service.IsUnlocked(item.cannonKey),
                _          => true,
            };
            item.button.gameObject.SetActive(show);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Feedback text
    // ════════════════════════════════════════════════════════════════════

    private async UniTaskVoid ShowNotEnoughGold()
    {
        if (notEnoughGoldText == null) return;
        SetTextAlpha(notEnoughGoldText, 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(notEnoughGoldText, Mathf.Clamp01(elapsed / fadeDuration));
            await UniTask.Yield(_destroyCT);
        }
        SetTextAlpha(notEnoughGoldText, 1f);

        await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: _destroyCT);

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
        Color c = tmp.color; c.a = alpha; tmp.color = c;
    }

    private void StartPulse(GameObject go)
    {
        if (buttonAnimator != null) buttonAnimator.AttentionPulse(go);
    }

    private void StopPulse(GameObject go)
    {
        if (buttonAnimator != null) buttonAnimator.StopAttentionPulse(go);
    }
}
