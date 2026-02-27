using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

public class AbilityUpgradeManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private Abilities_SO[] abilities;

    [Header("Ability Items")]
    [SerializeField] private List<AbilityUpgradeItemUI> abilityItems;

    [Header("Stats Display")]
    [SerializeField] private Image selectedAbilityImage;
    [SerializeField] private TextMeshProUGUI statsLevelText;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Visual States")]
    [SerializeField] private Sprite activeBg;
    [SerializeField] private Sprite normalBg;

    private int selectedIndex = -1;
    private bool isUpgrading = false;

    // Cloud Save key suffix
    private const string ABILITY_LEVEL_SUFFIX = "_AbilityLevel";

    /// <summary>Generates a cloud key from ability name, e.g. "HealingCannon_AbilityLevel"</summary>
    private string GetAbilityKey(int index)
    {
        return abilities[index].abilityName.Replace(" ", "") + ABILITY_LEVEL_SUFFIX;
    }

    public static event Action<int> OnAbilityUpgraded;

    private void Awake()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradePressed);
    }

    private void Start()
    {
        LoadAbilityLevels().Forget();
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradePressed);

        foreach (var item in abilityItems)
        {
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
        }
    }

    // ==================== Load from Cloud ====================

    private async UniTaskVoid LoadAbilityLevels()
    {
        if (abilities == null) return;

        try
        {
            // Build keys using ability names
            var keys = new HashSet<string>();
            for (int i = 0; i < abilities.Length; i++)
                keys.Add(GetAbilityKey(i));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // Set each ability's powerLevel directly
            for (int i = 0; i < abilities.Length; i++)
            {
                string key = GetAbilityKey(i);

                if (data.ContainsKey(key))
                    abilities[i].powerLevel = data[key].Value.GetAs<int>();
            }

            Debug.Log("[AbilityUpgrade] Loaded all ability levels from cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AbilityUpgrade] Failed to load ability levels: {ex.Message}");
        }

        // Initialize UI after data is ready
        InitializeItems();

        if (abilityItems.Count > 0)
            SelectAbility(0);
    }

    // ==================== Initialization ====================

    private void InitializeItems()
    {
        if (abilities == null) return;

        for (int i = 0; i < abilityItems.Count; i++)
        {
            if (i >= abilities.Length) break;

            int index = i;
            var item = abilityItems[i];
            var data = abilities[i];

            if (item.abilitySprite == null)
                item.abilitySprite = data.abilitySprite;

            if (item.levelText != null)
                item.levelText.text = $"Lv {data.powerLevel}";

            if (item.button != null)
                item.button.onClick.AddListener(() => SelectAbility(index));
        }
    }

    // ==================== Selection ====================

    private void SelectAbility(int index)
    {
        if (index < 0 || index >= abilityItems.Count) return;

        if (selectedIndex >= 0 && selectedIndex < abilityItems.Count)
            SetItemBg(selectedIndex, false);

        selectedIndex = index;
        SetItemBg(selectedIndex, true);

        UpdateStatsDisplay();
    }

    private void SetItemBg(int index, bool active)
    {
        if (index < 0 || index >= abilityItems.Count) return;
        if (abilityItems[index].backgroundImage != null)
            abilityItems[index].backgroundImage.sprite = active ? activeBg : normalBg;
    }

    // ==================== Stats Display ====================

    private void UpdateStatsDisplay()
    {
        if (selectedIndex < 0 || selectedIndex >= abilities.Length) return;

        var data = abilities[selectedIndex];

        if (selectedAbilityImage != null)
            selectedAbilityImage.sprite = data.abilitySprite;

        if (statsLevelText != null)
            statsLevelText.text = $"Level {data.powerLevel}";
    }

    // ==================== Upgrade & Save to Cloud ====================

    private void OnUpgradePressed()
    {
        if (selectedIndex < 0 || selectedIndex >= abilities.Length) return;
        if (isUpgrading) return;

        var data = abilities[selectedIndex];

        // Upgrade via SO's existing method
        data.UpgradeAbility();

        // Refresh UI
        UpdateStatsDisplay();
        if (abilityItems[selectedIndex].levelText != null)
            abilityItems[selectedIndex].levelText.text = $"{data.powerLevel}";

        // Save to cloud
        SaveAbilityLevel(selectedIndex, data.powerLevel).Forget();

        OnAbilityUpgraded?.Invoke(selectedIndex);
        Debug.Log($"[AbilityUpgrade] Upgraded {data.abilityName} to Level {data.powerLevel}");
    }

    private async UniTaskVoid SaveAbilityLevel(int abilityIndex, int level)
    {
        isUpgrading = true;

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(GetAbilityKey(abilityIndex), level);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AbilityUpgrade] Failed to save level: {ex.Message}");
        }
        finally
        {
            isUpgrading = false;
        }
    }

    // ==================== Public API ====================

    public int GetSelectedIndex() => selectedIndex;

    public void RefreshDisplay()
    {
        if (selectedIndex >= 0)
            UpdateStatsDisplay();
    }
}

[System.Serializable]
public class AbilityUpgradeItemUI
{
    public Button button;
    public Image backgroundImage;
    public Sprite abilitySprite;
    public TextMeshProUGUI levelText;
}