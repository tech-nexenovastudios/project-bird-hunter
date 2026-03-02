using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

public class CannonAndAbilitySelectionManager : MonoBehaviour
{
    [Header("Cannon Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Cannon Items")]
    [SerializeField] private List<CannonItemUI> cannonItems;

    [Header("Ability Items")]
    [SerializeField] private List<AbilityItemUI> abilityItems;

    [Header("External Display")]
    [SerializeField] private Image cannonImage;
    [SerializeField] private Image abilityImage;

    [Header("Equip Button")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;

    [Header("Description")]
    [SerializeField] private TextMeshProUGUI cannonDescriptionText;

    [Header("Visual States")]
    [SerializeField] private Sprite activeBgSprite;
    [SerializeField] private Sprite inactiveBgSprite;

    // Cloud Save Keys
    private const string SELECTED_CANNON_KEY = "SelectedCannonIndex";
    private const string SELECTED_ABILITY_KEY = "AbilityIndex";

    // Previewed = currently clicked/highlighted
    private int previewedCannonIndex = -1;
    private int previewedAbilityIndex = -1;

    // Equipped = saved selection
    private int equippedCannonIndex = -1;
    private int equippedAbilityIndex = -1;

    private bool isSaving = false;

    public static event Action<int, int> OnEquipped;

    private void Awake()
    {
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipPressed);
    }

    private void Start()
    {
        InitializeCannonItems();
        InitializeAbilityItems();
        LoadEquippedData().Forget();
    }

    private void OnDestroy()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(OnEquipPressed);

        foreach (var item in cannonItems)
        {
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
        }

        foreach (var item in abilityItems)
        {
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
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
                cannonItems[i].button.onClick.AddListener(() => PreviewCannon(index));
        }
    }

    private void InitializeAbilityItems()
    {
        for (int i = 0; i < abilityItems.Count; i++)
        {
            int index = i;
            if (abilityItems[i].button != null)
                abilityItems[i].button.onClick.AddListener(() => PreviewAbility(index));
        }
    }

    // ==================== Load from Cloud ====================

    private async UniTaskVoid LoadEquippedData()
    {
        try
        {
            var keys = new HashSet<string> { SELECTED_CANNON_KEY, SELECTED_ABILITY_KEY };
            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // Load cannon
            if (data.ContainsKey(SELECTED_CANNON_KEY))
            {
                equippedCannonIndex = data[SELECTED_CANNON_KEY].Value.GetAs<int>();
                equippedCannonIndex = Mathf.Clamp(equippedCannonIndex, 0, cannonHolderSO.cannonsData.Length - 1);
                PreviewCannon(equippedCannonIndex);
            }

            // Load ability
            if (data.ContainsKey(SELECTED_ABILITY_KEY))
            {
                equippedAbilityIndex = data[SELECTED_ABILITY_KEY].Value.GetAs<int>();
                equippedAbilityIndex = Mathf.Clamp(equippedAbilityIndex, 0, abilityItems.Count - 1);
                PreviewAbility(equippedAbilityIndex);
            }

            UpdateEquipButtonState();
            Debug.Log($"[Selection] Loaded — Cannon: {equippedCannonIndex}, Ability: {equippedAbilityIndex}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Selection] Failed to load equipped data: {ex.Message}");
        }
    }

    // ==================== Cannon Preview ====================

    private void PreviewCannon(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;

        // Already selected — do nothing
        if (previewedCannonIndex == index) return;

        if (previewedCannonIndex >= 0 && previewedCannonIndex < cannonItems.Count)
            SetCannonBg(previewedCannonIndex, false);

        previewedCannonIndex = index;
        SetCannonBg(previewedCannonIndex, true);

        if (cannonImage != null && cannonItems[index].cannonSprite != null)
            cannonImage.sprite = cannonItems[index].cannonSprite;

        // Show cannon description from SO
        if (cannonDescriptionText != null && cannonHolderSO != null
            && index < cannonHolderSO.cannonsData.Length)
        {
            cannonDescriptionText.text = cannonHolderSO.cannonsData[index].cannonDescription;
        }

        UpdateEquipButtonState();
    }

    private void SetCannonBg(int index, bool active)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (cannonItems[index].backgroundImage != null)
            cannonItems[index].backgroundImage.sprite = active ? activeBgSprite : inactiveBgSprite;
    }

    // ==================== Ability Preview ====================

    private void PreviewAbility(int index)
    {
        if (index < 0 || index >= abilityItems.Count) return;

        // Already selected — do nothing
        if (previewedAbilityIndex == index) return;

        if (previewedAbilityIndex >= 0 && previewedAbilityIndex < abilityItems.Count)
            SetAbilityBg(previewedAbilityIndex, false);

        previewedAbilityIndex = index;
        SetAbilityBg(previewedAbilityIndex, true);

        if (abilityImage != null && abilityItems[index].abilitySprite != null)
            abilityImage.sprite = abilityItems[index].abilitySprite;

        UpdateEquipButtonState();
    }

    private void SetAbilityBg(int index, bool active)
    {
        if (index < 0 || index >= abilityItems.Count) return;
        if (abilityItems[index].backgroundImage != null)
            abilityItems[index].backgroundImage.sprite = active ? activeBgSprite : inactiveBgSprite;
    }

    // ==================== Equip & Save to Cloud ====================

    private void OnEquipPressed()
    {
        if (previewedCannonIndex < 0 || previewedAbilityIndex < 0)
            return;

        if (previewedCannonIndex == equippedCannonIndex && previewedAbilityIndex == equippedAbilityIndex)
            return;

        if (isSaving) return;

        equippedCannonIndex = previewedCannonIndex;
        equippedAbilityIndex = previewedAbilityIndex;

        UpdateEquipButtonState();
        SaveEquippedData().Forget();

        OnEquipped?.Invoke(equippedCannonIndex, equippedAbilityIndex);
        Debug.Log($"[Equipped] Cannon: {equippedCannonIndex}, Ability: {equippedAbilityIndex}");
    }

    private async UniTaskVoid SaveEquippedData()
    {
        isSaving = true;

        try
        {
            await CloudSaveManager.Instance.SaveBatchAsync(
                (SELECTED_CANNON_KEY, equippedCannonIndex),
                (SELECTED_ABILITY_KEY, equippedAbilityIndex)
            );
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

    private void UpdateEquipButtonState()
    {
        if (equipButtonText == null) return;

        if (isSaving)
            equipButtonText.text = "SAVING...";
        else if (previewedCannonIndex < 0 || previewedAbilityIndex < 0)
            equipButtonText.text = "SELECT BOTH";
        else if (previewedCannonIndex == equippedCannonIndex && previewedAbilityIndex == equippedAbilityIndex)
            equipButtonText.text = "EQUIPPED";
        else
            equipButtonText.text = "EQUIP";
    }

    // ==================== Public API ====================

    public static async UniTask<int> GetEquippedCannonIndex()
    {
        return await CloudSaveManager.Instance.LoadValueAsync<int>(SELECTED_CANNON_KEY, 0);
    }

    public static async UniTask<int> GetEquippedAbilityIndex()
    {
        return await CloudSaveManager.Instance.LoadValueAsync<int>(SELECTED_ABILITY_KEY, -1);
    }
}

[System.Serializable]
public class CannonItemUI
{
    public Button button;
    public Sprite cannonSprite;
    public Image backgroundImage;
}

[System.Serializable]
public class AbilityItemUI
{
    public Button button;
    public Sprite abilitySprite;
    public Image backgroundImage;
}