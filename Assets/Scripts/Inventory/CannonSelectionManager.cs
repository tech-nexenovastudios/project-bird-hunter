using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

public class CannonSelectionManager : MonoBehaviour
{
    [Header("Cannon Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Cannon Items")]
    [SerializeField] private List<CannonItemUI> cannonItems;

    [Header("Common Display")]
    [SerializeField] private Image cannonImage;
    [SerializeField] private TextMeshProUGUI cannonNameText;

    [Header("Action Button (Equip / Unlock)")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("XP Display")]
    [SerializeField] private GameObject xpContainer;
    [SerializeField] private TextMeshProUGUI xpRequiredText;

    [Header("Visual States")]
    [SerializeField] private Sprite activeBgSprite;
    [SerializeField] private Sprite inactiveBgSprite;

    private const string SELECTED_CANNON_KEY = "SelectedCannonIndex";

    private int previewedCannonIndex = -1;
    private int equippedCannonIndex = -1;
    private bool isSaving = false;

    public static event Action<int> OnEquipped;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonPressed);
    }

    private void OnEnable()
    {
        CannonLockManager.OnCannonUnlocked += OnCannonUnlockedHandler;
        CannonLockManager.OnAllUnlockStatesLoaded += RefreshAllLockVisuals;
    }

    private void OnDisable()
    {
        CannonLockManager.OnCannonUnlocked -= OnCannonUnlockedHandler;
        CannonLockManager.OnAllUnlockStatesLoaded -= RefreshAllLockVisuals;
    }

    private void Start()
    {
        InitializeCannonItems();
        RefreshAllLockVisuals();
        LoadEquippedData().Forget();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonPressed);

        foreach (var item in cannonItems)
        {
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
        }
    }

    // ==================== Lock / Unlock ====================

    private void OnCannonUnlockedHandler(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;

        if (CannonLockManager.Instance != null && cannonItems[index].button != null)
            CannonLockManager.Instance.ApplyUnlockedVisual(cannonItems[index].button.transform);

        // Refresh if currently viewing this cannon
        if (previewedCannonIndex == index)
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

            // All buttons stay interactive for preview
            cannonItems[i].button.interactable = true;
        }
    }

    // ==================== Initialization ====================

    private void InitializeCannonItems()
    {
        if (cannonHolderSO == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (i >= cannonHolderSO.cannonsData.Length) break;

            int index = i;
            if (cannonItems[i].button != null)
            {
                cannonItems[i].button.interactable = true;
                cannonItems[i].button.onClick.AddListener(() => PreviewCannon(index));
            }
        }
    }

    // ==================== Load from Cloud ====================

    private async UniTaskVoid LoadEquippedData()
    {
        try
        {
            var keys = new HashSet<string> { SELECTED_CANNON_KEY };
            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            if (data.ContainsKey(SELECTED_CANNON_KEY))
            {
                equippedCannonIndex = data[SELECTED_CANNON_KEY].Value.GetAs<int>();
                equippedCannonIndex = Mathf.Clamp(equippedCannonIndex, 0, cannonHolderSO.cannonsData.Length - 1);
            }
            else
            {
                equippedCannonIndex = 0;
            }

            // Show previously equipped cannon highlighted with all its data
            PreviewCannon(equippedCannonIndex);

            Debug.Log($"[Selection] Loaded — Cannon: {equippedCannonIndex}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Selection] Failed to load: {ex.Message}");
            // Fallback to first cannon
            equippedCannonIndex = 0;
            PreviewCannon(0);
        }
    }

    // ==================== Cannon Preview ====================

    private void PreviewCannon(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (previewedCannonIndex == index) return;

        // Deselect previous
        if (previewedCannonIndex >= 0 && previewedCannonIndex < cannonItems.Count)
            SetCannonBg(previewedCannonIndex, false);

        previewedCannonIndex = index;
        SetCannonBg(previewedCannonIndex, true);

        UpdatePreviewDisplay(index);
    }

    private void UpdatePreviewDisplay(int index)
    {
        if (index < 0 || index >= cannonHolderSO.cannonsData.Length) return;

        // Cannon image
        if (cannonImage != null && cannonItems[index].cannonSprite != null)
            cannonImage.sprite = cannonItems[index].cannonSprite;

        // Cannon name
        if (cannonNameText != null)
            cannonNameText.text = cannonHolderSO.cannonsData[index].cannonName;

        bool unlocked = CannonLockManager.Instance != null
            && CannonLockManager.Instance.IsUnlocked(index);

        // XP: show only for locked
        if (xpContainer != null)
            xpContainer.SetActive(!unlocked);

        if (!unlocked)
            UpdateXPRequired(index);

        // Update button text
        UpdateActionButtonState();
    }

    // ==================== XP Required ====================

    /// <summary>TODO: Fill with actual XP requirement from your XPManager later.</summary>
    private void UpdateXPRequired(int cannonIndex)
    {
        if (xpRequiredText == null) return;

        // Placeholder — replace with real XP values later
        int xpNeeded = 0;
        xpRequiredText.text = xpNeeded.ToString();
    }

    // ==================== Visual ====================

    private void SetCannonBg(int index, bool active)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (cannonItems[index].backgroundImage != null)
            cannonItems[index].backgroundImage.sprite = active ? activeBgSprite : inactiveBgSprite;
    }

    // ==================== Action Button (Equip / Unlock) ====================

    private void OnActionButtonPressed()
    {
        if (previewedCannonIndex < 0) return;
        if (isSaving) return;

        bool unlocked = CannonLockManager.Instance != null
            && CannonLockManager.Instance.IsUnlocked(previewedCannonIndex);

        if (unlocked)
        {
            // Equip
            if (previewedCannonIndex == equippedCannonIndex) return;

            equippedCannonIndex = previewedCannonIndex;
            UpdateActionButtonState();
            SaveEquippedData().Forget();

            OnEquipped?.Invoke(equippedCannonIndex);
            Debug.Log($"[Equipped] Cannon: {equippedCannonIndex}");
        }
        else
        {
            // Try unlock
            if (CannonLockManager.Instance != null)
                CannonLockManager.Instance.TryUnlockCannon(previewedCannonIndex);
        }
    }

    private void UpdateActionButtonState()
    {
        if (actionButtonText == null) return;

        if (previewedCannonIndex < 0)
        {
            actionButtonText.text = "SELECT";
            return;
        }

        bool unlocked = CannonLockManager.Instance != null
            && CannonLockManager.Instance.IsUnlocked(previewedCannonIndex);

        if (isSaving)
            actionButtonText.text = "SAVING...";
        else if (!unlocked)
            actionButtonText.text = "UNLOCK";
        else if (previewedCannonIndex == equippedCannonIndex)
            actionButtonText.text = "EQUIPPED";
        else
            actionButtonText.text = "EQUIP";
    }

    private async UniTaskVoid SaveEquippedData()
    {
        isSaving = true;

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(SELECTED_CANNON_KEY, equippedCannonIndex);
            Debug.Log("[Selection] Saved to Cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Selection] Failed to save: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    // ==================== Public API ====================

    public static async UniTask<int> GetEquippedCannonIndex()
    {
        return await CloudSaveManager.Instance.LoadValueAsync<int>(SELECTED_CANNON_KEY, 0);
    }
}

[System.Serializable]
public class CannonItemUI
{
    public Button button;
    public Sprite cannonSprite;
    public Image backgroundImage;
}