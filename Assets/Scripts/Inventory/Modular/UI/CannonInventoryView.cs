// using System;
// using System.Collections.Generic;
// using System.Threading;
// using BirdHunter.Inventory.Data;
// using BirdHunter.Inventory.Services;
// using BirdHunter.Inventory.Stats;
// using Cysharp.Threading.Tasks;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace BirdHunter.Inventory.UI
// {
//     // ═══════════════════════════════════════════════════════════════════════
//     //  CANNON INVENTORY VIEW — thin presenter, reads CannonInventoryService
//     // ═══════════════════════════════════════════════════════════════════════
//     //
//     //  Wiring: each CannonItem's `cannonId` must match the `id` field of the
//     //  matching cloud `cannon_stats` entry. Order in the list doesn't matter.
//     //
//     //  All costs, unlock gates, upgrade math, cloud persistence live in the
//     //  service — this file only binds events and updates widgets.
//     //
//     // ═══════════════════════════════════════════════════════════════════════
//     public sealed class CannonInventoryView : MonoBehaviour
//     {
//         [Header("Service")]
//         [SerializeField] private CannonInventoryService service;
//
//         [Header("Items")]
//         [SerializeField] private CannonDatabase database;
//         [SerializeField] private CannonItem cannonItemPrefab;
//         [SerializeField] private Transform itemContainer;
//         [SerializeField] private List<CannonItem> items;
//
//         [Header("Preview Panel")]
//         [SerializeField] private Image previewCannonImage;
//         [SerializeField] private TextMeshProUGUI previewNameText;
//         [SerializeField] private TextMeshProUGUI previewLevelText;
//         [SerializeField] private TextMeshProUGUI previewDescriptionText;
//
//         [Header("Fill Bars – Current")]
//         [SerializeField] private Image damageFillCurrent;
//         [SerializeField] private Image healthFillCurrent;
//         [SerializeField] private Image fireRateFillCurrent;
//
//         [Header("Fill Bars – Next Preview")]
//         [SerializeField] private Image damageFillNext;
//         [SerializeField] private Image healthFillNext;
//         [SerializeField] private Image fireRateFillNext;
//
//         [Header("Fill Animation")]
//         [SerializeField] private float fillAnimDuration = 0.35f;
//
//         [Header("Buttons")]
//         [SerializeField] private Button equipButton;
//         [SerializeField] private TextMeshProUGUI equipButtonText;
//         [SerializeField] private Button upgradeButton;
//         [SerializeField] private TextMeshProUGUI upgradeButtonText;
//         [SerializeField] private ButtonAnimator buttonAnimator;
//
//         [Header("Unlock Popup")]
//         [SerializeField] private GameObject unlockPopupPanel;
//         [SerializeField] private Image popupCannonImage;
//         [SerializeField] private TextMeshProUGUI popupCannonNameText;
//         [SerializeField] private TextMeshProUGUI popupCoinCostText;
//         [SerializeField] private TextMeshProUGUI popupGemCostText;
//         [SerializeField] private Button popupConfirmButton;
//         [SerializeField] private Button popupCancelButton;
//
//         [Header("Tabs")]
//         [SerializeField] private ScrollRect scrollRect;
//         [SerializeField] private Button allTabButton;
//         [SerializeField] private Button unlockedTabButton;
//         [SerializeField] private Button lockedTabButton;
//
//         [Header("Feedback")]
//         [SerializeField] private TextMeshProUGUI notEnoughGoldText;
//         [SerializeField] private float fadeDuration = 0.4f;
//         [SerializeField] private float holdDuration = 1.0f;
//
//         [Header("Item Visuals")]
//         [SerializeField] private Sprite normalBgSprite;
//         [SerializeField] private Sprite selectedBgSprite;
//         [SerializeField] private Sprite normalFrameSprite;
//         [SerializeField] private Sprite selectedFrameSprite;
//         [SerializeField] private Material grayscaleMaterial;
//
//         // ── Runtime ────────────────────────────────────────────────────────
//         private int _selectedId = -1;
//         private float _prevFillRatio;
//         private bool _busy;
//         private CancellationToken _destroyCT;
//
//         // ════════════════════════════════════════════════════════════════════
//         // Lifecycle
//         // ════════════════════════════════════════════════════════════════════
//
//         private void Awake()
//         {
//             _destroyCT = this.GetCancellationTokenOnDestroy();
//
//             PopulateItems();
//
//             if (equipButton != null)   equipButton.onClick.AddListener(OnEquipPressed);
//             if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradePressed);
//             if (popupConfirmButton != null) popupConfirmButton.onClick.AddListener(OnPopupConfirm);
//             if (popupCancelButton != null)  popupCancelButton.onClick.AddListener(() => SetPopup(false));
//
//             if (allTabButton != null)      allTabButton.onClick.AddListener(() => Filter("All"));
//             if (unlockedTabButton != null) unlockedTabButton.onClick.AddListener(() => Filter("Unlocked"));
//             if (lockedTabButton != null)   lockedTabButton.onClick.AddListener(() => Filter("Locked"));
//
//             SetPopup(false);
//
//             if (notEnoughGoldText != null)
//             {
//                 notEnoughGoldText.text = "Not enough Gold!";
//                 SetAlpha(notEnoughGoldText, 0f);
//             }
//         }
//
//         private void OnEnable()
//         {
//             if (service == null) service = CannonInventoryService.Instance;
//             if (service == null) return;
//
//             service.OnReady     += HandleReady;
//             service.OnUnlocked  += HandleUnlocked;
//             service.OnUpgraded  += HandleUpgraded;
//             service.OnEquipped  += HandleEquipped;
//
//             if (service.IsReady) HandleReady();
//
//             if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
//             Filter("All");
//         }
//
//         private void OnDisable()
//         {
//             if (service == null) return;
//             service.OnReady     -= HandleReady;
//             service.OnUnlocked  -= HandleUnlocked;
//             service.OnUpgraded  -= HandleUpgraded;
//             service.OnEquipped  -= HandleEquipped;
//         }
//
//         private void OnDestroy()
//         {
//             if (equipButton != null)   equipButton.onClick.RemoveAllListeners();
//             if (upgradeButton != null) upgradeButton.onClick.RemoveAllListeners();
//             foreach (var item in items)
//                 if (item.button != null) item.button.onClick.RemoveAllListeners();
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Wiring
//         // ════════════════════════════════════════════════════════════════════
//
//         private void PopulateItems()
//         {
//             var sprites = database.cannonSprites;
//
//             foreach (var db in sprites)
//             {
//                 var item = Instantiate(cannonItemPrefab, itemContainer);
//                 item.cannonSprite.sprite = db.Value;
//                 item.cannonId = db.Key;
//                 item.button = item.GetComponentInChildren<Button>();
//                 item.button.interactable = false;
//                 item.button.gameObject.SetActive(true);
//                 WireItem(item);
//             }
//         }
//         
//         private void WireItem(CannonItem item)
//         {
//             int id = item.cannonId;
//             item.button.onClick.AddListener(() => SelectCannon(id));
//             item.button.interactable = true;
//         }
//
//         private CannonItem FindItem(int id)
//         {
//             foreach (var it in items)
//                 if (it.cannonId == id) return it;
//             return null;
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Service event handlers
//         // ════════════════════════════════════════════════════════════════════
//
//         private void HandleReady()
//         {
//             RefreshAllItemVisuals();
//             int startId = service.EquippedId >= 0 ? service.EquippedId : (items.Count > 0 ? items[0].cannonId : -1);
//             if (startId >= 0) SelectCannon(startId);
//         }
//
//         private void HandleUnlocked(int id)
//         {
//             ApplyLockVisual(id, unlocked: true);
//             if (id == _selectedId) UpdatePreview(id);
//         }
//
//         private void HandleUpgraded(int id)
//         {
//             if (id == _selectedId)
//             {
//                 UpdatePreviewText(id);
//                 AnimateFills(id).Forget();
//                 RefreshUpgradeButton(id);
//             }
//         }
//
//         private void HandleEquipped(int id)
//         {
//             if (_selectedId >= 0) RefreshEquipButton(_selectedId);
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Selection
//         // ════════════════════════════════════════════════════════════════════
//
//         private void SelectCannon(int id)
//         {
//             if (_selectedId >= 0) SetItemSelectedVisual(_selectedId, false);
//             _selectedId = id;
//             SetItemSelectedVisual(id, true);
//             UpdatePreview(id);
//         }
//
//         private void SetItemSelectedVisual(int id, bool selected)
//         {
//             var item = FindItem(id);
//             if (item == null) return;
//             if (item.cannonBgComp != null)
//                 item.cannonBgComp.sprite = selected ? selectedBgSprite : normalBgSprite;
//             if (item.frameComp != null)
//                 item.frameComp.sprite = selected ? selectedFrameSprite : normalFrameSprite;
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Preview panel
//         // ════════════════════════════════════════════════════════════════════
//
//         private void UpdatePreview(int id)
//         {
//             var dto = service.GetBaseData(id);
//             if (dto == null) return;
//
//             bool unlocked = service.IsUnlocked(id);
//             var item = FindItem(id);
//
//             if (previewCannonImage != null)
//             {
//                 // previewCannonImage.sprite = item?.cannonSprite;
//                 previewCannonImage.material = unlocked ? null : grayscaleMaterial;
//             }
//
//             UpdatePreviewText(id);
//             SnapFills(id);
//             RefreshEquipButton(id);
//             RefreshUpgradeButton(id);
//         }
//
//         private void UpdatePreviewText(int id)
//         {
//             var dto = service.GetBaseData(id);
//             if (dto == null) return;
//             if (previewNameText != null)        previewNameText.text = dto.name;
//             if (previewLevelText != null)       previewLevelText.text = $"Level {service.GetLevel(id)}";
//             if (previewDescriptionText != null) previewDescriptionText.text = dto.description;
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Fill bars (level-based, 1..max → 0..1)
//         // ════════════════════════════════════════════════════════════════════
//
//         private float LevelRatio(int id, int level)
//         {
//             int max = service.GetMaxLevel(id);
//             return max <= 0 ? 0f : Mathf.Clamp01((float)level / max);
//         }
//
//         private void SetFill(Image img, float ratio)
//         {
//             if (img == null) return;
//             if (ratio <= 0f) { img.enabled = false; return; }
//             img.enabled = true;
//             var rt = img.rectTransform;
//             rt.anchorMin = Vector2.zero;
//             rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
//             rt.offsetMin = rt.offsetMax = Vector2.zero;
//         }
//
//         private void SnapFills(int id)
//         {
//             int level = service.GetLevel(id);
//             float current = LevelRatio(id, level);
//             float next = LevelRatio(id, level + 1);
//             bool atMax = service.IsMaxLevel(id);
//
//             SetFill(damageFillCurrent, current);
//             SetFill(healthFillCurrent, current);
//             SetFill(fireRateFillCurrent, current);
//
//             SetFill(damageFillNext, atMax ? current : next);
//             SetFill(healthFillNext, atMax ? current : next);
//             SetFill(fireRateFillNext, atMax ? current : next);
//
//             _prevFillRatio = current;
//         }
//
//         private async UniTask AnimateFills(int id)
//         {
//             int level = service.GetLevel(id);
//             float target = LevelRatio(id, level);
//             float elapsed = 0f;
//
//             while (elapsed < fillAnimDuration)
//             {
//                 elapsed += Time.deltaTime;
//                 float t = Mathf.SmoothStep(0f, 1f, elapsed / fillAnimDuration);
//                 float v = Mathf.Lerp(_prevFillRatio, target, t);
//                 SetFill(damageFillCurrent, v);
//                 SetFill(healthFillCurrent, v);
//                 SetFill(fireRateFillCurrent, v);
//                 await UniTask.Yield(_destroyCT);
//             }
//
//             SnapFills(id);
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Equip button
//         // ════════════════════════════════════════════════════════════════════
//
//         private void RefreshEquipButton(int id)
//         {
//             if (equipButton == null || equipButtonText == null) return;
//
//             bool unlocked = service.IsUnlocked(id);
//
//             if (_busy)
//             {
//                 equipButtonText.text = "SAVING...";
//                 equipButton.interactable = false;
//                 StopPulse(equipButton.gameObject);
//                 return;
//             }
//
//             if (!unlocked)
//             {
//                 equipButtonText.text = "UNLOCK";
//                 equipButton.interactable = true;
//                 StopPulse(equipButton.gameObject);
//                 return;
//             }
//
//             if (service.IsEquipped(id))
//             {
//                 equipButtonText.text = "EQUIPPED";
//                 equipButton.interactable = false;
//                 StopPulse(equipButton.gameObject);
//             }
//             else
//             {
//                 equipButtonText.text = "EQUIP";
//                 equipButton.interactable = true;
//                 StartPulse(equipButton.gameObject);
//             }
//         }
//
//         private void OnEquipPressed()
//         {
//             if (_selectedId < 0 || _busy) return;
//
//             if (!service.IsUnlocked(_selectedId))
//             {
//                 ShowUnlockPopup(_selectedId);
//                 return;
//             }
//
//             EquipAsync(_selectedId).Forget();
//         }
//
//         private async UniTaskVoid EquipAsync(int id)
//         {
//             _busy = true;
//             RefreshEquipButton(id);
//             await service.Equip(id);
//             _busy = false;
//             RefreshEquipButton(id);
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Upgrade button
//         // ════════════════════════════════════════════════════════════════════
//
//         private void RefreshUpgradeButton(int id)
//         {
//             if (upgradeButton == null) return;
//
//             bool unlocked = service.IsUnlocked(id);
//             if (!unlocked)
//             {
//                 if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
//                 upgradeButton.interactable = false;
//                 StopPulse(upgradeButton.gameObject);
//                 return;
//             }
//
//             if (service.IsMaxLevel(id))
//             {
//                 if (upgradeButtonText != null) upgradeButtonText.text = "MAX LEVEL";
//                 upgradeButton.interactable = false;
//                 StopPulse(upgradeButton.gameObject);
//                 return;
//             }
//
//             int cost = service.GetUpgradeCoinCost(id);
//             bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Gold >= cost;
//
//             if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
//             upgradeButton.interactable = canAfford;
//             if (canAfford) StartPulse(upgradeButton.gameObject);
//             else StopPulse(upgradeButton.gameObject);
//         }
//
//         private void OnUpgradePressed()
//         {
//             if (_selectedId < 0) return;
//             UpgradeAsync(_selectedId).Forget();
//         }
//
//         private async UniTaskVoid UpgradeAsync(int id)
//         {
//             if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADING...";
//             upgradeButton.interactable = false;
//
//             bool ok = await service.TryUpgrade(id);
//             if (!ok) ShowNotEnoughGold().Forget();
//
//             RefreshUpgradeButton(id);
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Unlock popup
//         // ════════════════════════════════════════════════════════════════════
//
//         private void ShowUnlockPopup(int id)
//         {
//             var dto = service.GetBaseData(id);
//             if (dto == null || unlockPopupPanel == null) return;
//
//             var item = FindItem(id);
//             // if (popupCannonImage != null && item != null) popupCannonImage.sprite = item.cannonSprite;
//             if (popupCannonNameText != null) popupCannonNameText.text = dto.name;
//
//             int coinCost = service.GetUnlockCoinCost(id);
//             int gemCost = service.GetUnlockGemCost(id);
//             if (popupCoinCostText != null) popupCoinCostText.text = coinCost == 0 ? "Free" : $"{coinCost:N0} Gold";
//             if (popupGemCostText != null)  popupGemCostText.text  = gemCost  == 0 ? "" : $"{gemCost} Gems";
//
//             SetPopup(true);
//         }
//
//         private void OnPopupConfirm()
//         {
//             if (_selectedId < 0) return;
//             ConfirmUnlockAsync(_selectedId).Forget();
//         }
//
//         private async UniTaskVoid ConfirmUnlockAsync(int id)
//         {
//             if (popupConfirmButton != null) popupConfirmButton.interactable = false;
//             SetPopup(false);
//
//             bool ok = await service.TryUnlock(id);
//             if (!ok) ShowNotEnoughGold().Forget();
//
//             if (popupConfirmButton != null) popupConfirmButton.interactable = true;
//         }
//
//         private void SetPopup(bool on)
//         {
//             if (unlockPopupPanel != null) unlockPopupPanel.SetActive(on);
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Lock visuals + filters
//         // ════════════════════════════════════════════════════════════════════
//
//         private void RefreshAllItemVisuals()
//         {
//             foreach (var item in items)
//                 ApplyLockVisual(item.cannonId, service.IsUnlocked(item.cannonId));
//         }
//
//         private void ApplyLockVisual(int id, bool unlocked)
//         {
//             var item = FindItem(id);
//             if (item?.button == null) return;
//
//             foreach (var g in item.button.GetComponentsInChildren<Graphic>(true))
//                 g.material = unlocked ? null : grayscaleMaterial;
//
//             if (item.lockImageObj != null) item.lockImageObj.SetActive(!unlocked);
//
//             if (item.unlockRequirementContainer != null)
//             {
//                 if (unlocked)
//                 {
//                     item.unlockRequirementContainer.SetActive(false);
//                 }
//                 else
//                 {
//                     var dto = service.GetBaseData(id);
//                     if (dto != null && item.unlockRequirementText != null)
//                     {
//                         item.unlockRequirementContainer.SetActive(true);
//                         item.unlockRequirementText.text = $"Unlocks at Ch. {dto.unlockAtChapter}";
//                     }
//                 }
//             }
//         }
//
//         private void Filter(string filter)
//         {
//             foreach (var item in items)
//             {
//                 if (item.button == null) continue;
//                 bool show = filter switch
//                 {
//                     "Unlocked" => service.IsUnlocked(item.cannonId),
//                     "Locked"   => !service.IsUnlocked(item.cannonId),
//                     _          => true
//                 };
//                 item.button.gameObject.SetActive(show);
//             }
//         }
//
//         // ════════════════════════════════════════════════════════════════════
//         // Feedback text
//         // ════════════════════════════════════════════════════════════════════
//
//         private async UniTaskVoid ShowNotEnoughGold()
//         {
//             if (notEnoughGoldText == null) return;
//             SetAlpha(notEnoughGoldText, 0f);
//
//             float elapsed = 0f;
//             while (elapsed < fadeDuration)
//             {
//                 elapsed += Time.deltaTime;
//                 SetAlpha(notEnoughGoldText, Mathf.Clamp01(elapsed / fadeDuration));
//                 await UniTask.Yield(_destroyCT);
//             }
//             SetAlpha(notEnoughGoldText, 1f);
//
//             await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: _destroyCT);
//
//             elapsed = 0f;
//             while (elapsed < fadeDuration)
//             {
//                 elapsed += Time.deltaTime;
//                 SetAlpha(notEnoughGoldText, Mathf.Clamp01(1f - elapsed / fadeDuration));
//                 await UniTask.Yield(_destroyCT);
//             }
//             SetAlpha(notEnoughGoldText, 0f);
//         }
//
//         private static void SetAlpha(TextMeshProUGUI tmp, float a)
//         {
//             var c = tmp.color; c.a = a; tmp.color = c;
//         }
//
//         private void StartPulse(GameObject go)
//         {
//             if (buttonAnimator != null) buttonAnimator.AttentionPulse(go);
//         }
//
//         private void StopPulse(GameObject go)
//         {
//             if (buttonAnimator != null) buttonAnimator.StopAttentionPulse(go);
//         }
//     }
// }
