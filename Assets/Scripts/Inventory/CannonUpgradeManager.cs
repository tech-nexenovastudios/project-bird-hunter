using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

public class CannonUpgradeManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Cannon Items")]
    [SerializeField] private List<CannonUpgradeItemUI> cannonItems;

    [Header("Stats Display")]
    [SerializeField] private Image selectedCannonImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsLevelText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI fireRateText;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;

    [Header("Description")]
    [SerializeField] private TextMeshProUGUI cannonDescriptionText;

    [Header("Visual States")]
    [SerializeField] private Sprite activeBg;
    [SerializeField] private Sprite normalBg;

    private int selectedIndex = -1;
    private bool isUpgrading = false;

    // Cloud Save key suffix
    private const string CANNON_LEVEL_SUFFIX = "_CannonLevel";

    /// <summary>Generates a cloud key from cannon name, e.g. "Shotgun_CannonLevel"</summary>
    private string GetCannonKey(int index)
    {
        return cannonHolderSO.cannonsData[index].cannonName.Replace(" ", "") + CANNON_LEVEL_SUFFIX;
    }

    public static event Action<int> OnCannonUpgraded;

    private void Awake()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradePressed);
    }

    private void Start()
    {
        CacheAllBaseValues();
        LoadAndReplayAll().Forget();
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradePressed);

        foreach (var item in cannonItems)
        {
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
        }
    }

    // ==================== Base Value Caching ====================

    private void CacheAllBaseValues()
    {
        if (cannonHolderSO == null) return;

        foreach (var cannon in cannonHolderSO.cannonsData)
        {
            cannon.CacheBaseValues();
        }
    }

    // ==================== Load from Cloud & Replay ====================

    private async UniTaskVoid LoadAndReplayAll()
    {
        if (cannonHolderSO == null) return;

        try
        {
            // Build keys using cannon names
            var keys = new HashSet<string>();
            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
                keys.Add(GetCannonKey(i));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // Replay each cannon to its saved level
            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
            {
                string key = GetCannonKey(i);
                int savedLevel = 0;

                if (data.ContainsKey(key))
                    savedLevel = data[key].Value.GetAs<int>();

                cannonHolderSO.cannonsData[i].ReplayToLevel(savedLevel);
            }

            Debug.Log("[CannonUpgrade] Loaded and replayed all cannon levels from cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonUpgrade] Failed to load cannon levels: {ex.Message}");
        }

        // Initialize UI after data is ready
        InitializeItems();

        if (cannonItems.Count > 0)
            SelectCannon(0);
    }

    // ==================== Initialization ====================

    private void InitializeItems()
    {
        if (cannonHolderSO == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (i >= cannonHolderSO.cannonsData.Length) break;

            int index = i;
            var item = cannonItems[i];
            var data = cannonHolderSO.cannonsData[i];

            if (item.cannonSprite == null)
                item.cannonSprite = data.cannonSprite;

            if (item.levelText != null)
                item.levelText.text = $"{data.cannonLevel}";

            if (item.button != null)
                item.button.onClick.AddListener(() => SelectCannon(index));
        }
    }

    // ==================== Selection ====================

    private void SelectCannon(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;

        if (selectedIndex >= 0 && selectedIndex < cannonItems.Count)
            SetItemBg(selectedIndex, false);

        selectedIndex = index;
        SetItemBg(selectedIndex, true);

        UpdateStatsDisplay();
    }

    private void SetItemBg(int index, bool active)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (cannonItems[index].backgroundImage != null)
            cannonItems[index].backgroundImage.sprite = active ? activeBg : normalBg;
    }

    // ==================== Stats Display ====================

    private void UpdateStatsDisplay()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length) return;

        var data = cannonHolderSO.cannonsData[selectedIndex];

        if (selectedCannonImage != null)
            selectedCannonImage.sprite = data.cannonSprite;

        if (nameText != null)
            nameText.text = data.cannonName;

        if (cannonDescriptionText != null)
            cannonDescriptionText.text = data.cannonDescription;

        if (statsLevelText != null)
            statsLevelText.text = $"Level {data.cannonLevel}";

        if (data.cannonStats != null)
        {
            if (damageText != null)
                damageText.text = $"DAMAGE: {data.cannonStats.bulletDamage}";

            if (healthText != null)
                healthText.text = $"HEALTH: {data.cannonStats.maxHealth}";

            if (fireRateText != null)
                fireRateText.text = $"FIRE RATE: {data.cannonStats.fireRate}";
        }
    }

    // ==================== Upgrade & Save to Cloud ====================

    private void OnUpgradePressed()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length) return;
        if (isUpgrading) return;

        var data = cannonHolderSO.cannonsData[selectedIndex];

        // Upgrade via SO's existing method
        data.UpgradeCannon();

        // Refresh UI
        UpdateStatsDisplay();
        if (cannonItems[selectedIndex].levelText != null)
            cannonItems[selectedIndex].levelText.text = $"{data.cannonLevel}";

        // Save to cloud
        SaveCannonLevel(selectedIndex, data.cannonLevel).Forget();

        OnCannonUpgraded?.Invoke(selectedIndex);
        Debug.Log($"[CannonUpgrade] Upgraded {data.cannonName} to Level {data.cannonLevel}");
    }

    private async UniTaskVoid SaveCannonLevel(int cannonIndex, int level)
    {
        isUpgrading = true;

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(GetCannonKey(cannonIndex), level);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonUpgrade] Failed to save level: {ex.Message}");
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
public class CannonUpgradeItemUI
{
    public Button button;
    public Image backgroundImage;
    public Sprite cannonSprite;
    public TextMeshProUGUI levelText;
}