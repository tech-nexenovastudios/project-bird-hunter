using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

public class CannonUpgradeManager : MonoBehaviour
{
    [Header("Data")] [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Cannon Items")] [SerializeField]
    private List<CannonUpgradeItemUI> cannonItems;

    [Header("Stats Display")] [SerializeField]
    private Image selectedCannonImage;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsLevelText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI fireRateText;

    [Header("Stats Fill (Current)")] [SerializeField]
    private Image damageFillCurrent;

    [SerializeField] private Image healthFillCurrent;
    [SerializeField] private Image fireRateFillCurrent;

    [Header("Stats Fill (Next Level Preview)")] [SerializeField]
    private Image damageFillNext;

    [SerializeField] private Image healthFillNext;
    [SerializeField] private Image fireRateFillNext;

    [Header("Fill Gap")] [SerializeField] private float fillGap = 0.05f;

    [Header("Max Level")] [SerializeField] private int maxCannonLevel = 100;

    [Header("Upgrade Button")] [SerializeField]
    private Button upgradeButton;

    [Header("Description")] [SerializeField]
    private TextMeshProUGUI cannonDescriptionText;

    [Header("Visual States")] [SerializeField]
    private Sprite activeBg;

    [SerializeField] private Sprite normalBg;

    private int selectedIndex = -1;
    private bool isUpgrading = false;

    private const string CANNON_LEVEL_SUFFIX = "_CannonLevel";

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

    public static event Action OnAllLevelsLoaded;

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

    // ==================== Lock / Unlock ====================

    private void OnCannonUnlockedHandler(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;

        if (CannonLockManager.Instance != null && cannonItems[index].button != null)
        {
            CannonLockManager.Instance.ApplyUnlockedVisual(cannonItems[index].button.transform);
            cannonItems[index].button.interactable = true;
        }
    }

    private void RefreshAllLockVisuals()
    {
        if (CannonLockManager.Instance == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (cannonItems[i].button == null) continue;

            if (CannonLockManager.Instance.IsUnlocked(i))
            {
                CannonLockManager.Instance.ApplyUnlockedVisual(cannonItems[i].button.transform);
                cannonItems[i].button.interactable = true;
            }
            else
            {
                CannonLockManager.Instance.ApplyLockedVisual(cannonItems[i].button.transform);
                cannonItems[i].button.interactable = false;
            }
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
            var keys = new HashSet<string>();
            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
                keys.Add(GetCannonKey(i));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
            {
                string key = GetCannonKey(i);
                int savedLevel = 0;

                if (data.ContainsKey(key))
                    savedLevel = data[key].Value.GetAs<int>();

                cannonHolderSO.cannonsData[i].ReplayToLevel(savedLevel);
            }

            foreach (var cannon in cannonHolderSO.cannonsData)
            {
                //Debug.Log($"[CannonUpgrade] {cannon.cannonName} loaded at Level {cannon.cannonLevel} damege {cannon.cannonStats.baseBulletDamage}");
            }

            Debug.Log("[CannonUpgrade] Loaded and replayed all cannon levels from cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonUpgrade] Failed to load cannon levels: {ex.Message}");
        }

        InitializeItems();
        RefreshAllLockVisuals();

        OnAllLevelsLoaded?.Invoke(); //  fire after levels are ready

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

        // Block if locked
        if (CannonLockManager.Instance != null && !CannonLockManager.Instance.IsUnlocked(index)) return;

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
        var stats = data.cannonStats;

        if (selectedCannonImage != null)
            selectedCannonImage.sprite = data.cannonSprite;

        if (nameText != null)
            nameText.text = data.cannonName;

        if (cannonDescriptionText != null)
            cannonDescriptionText.text = data.cannonDescription;

        if (statsLevelText != null)
            statsLevelText.text = $"Level {data.cannonLevel}";

        if (stats == null) return;

        var bullet = stats.baseBulletConfig; // ← bullet values live here now

        // ── Max values for fill bar normalization ──
        float maxDamage = bullet != null ? bullet.baseDamage + (bullet.damageIncrement * maxCannonLevel) : 1f;
        float maxHealth = stats._baseMaxHealth + (stats.healthIncrement * maxCannonLevel);
        float maxFireRate = stats._baseFireRate + (stats.fireRateIncrement * maxCannonLevel);

        // ── Current runtime values ──
        float curDamage = bullet != null ? bullet.currentDamage : 0f;
        float curHealth = stats.maxHealth;
        float curFireRate = stats.fireRate;

        // ── Next level preview ──
        float nextDamage = curDamage + (bullet != null ? bullet.damageIncrement : 0f);
        float nextHealth = curHealth + stats.healthIncrement;
        float nextFireRate = curFireRate + stats.fireRateIncrement;

        // ── Labels ──
        if (damageText != null) damageText.text = "DAMAGE:";
        if (healthText != null) healthText.text = "HEALTH:";
        if (fireRateText != null) fireRateText.text = "FIRE RATE:";

        // ── Current fills ──
        SetFill(damageFillCurrent, curDamage / maxDamage);
        SetFill(healthFillCurrent, curHealth / maxHealth);
        SetFill(fireRateFillCurrent, curFireRate / maxFireRate);

        // ── Next level fills ──
        SetNextFill(damageFillNext, curDamage / maxDamage, nextDamage / maxDamage);
        SetNextFill(healthFillNext, curHealth / maxHealth, nextHealth / maxHealth);
        SetNextFill(fireRateFillNext, curFireRate / maxFireRate, nextFireRate / maxFireRate);
    }


    private void SetFill(Image fillImage, float ratio)
    {
        if (fillImage == null) return;

        var rt = fillImage.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private void SetNextFill(Image fillImage, float currentRatio, float nextRatio)
    {
        if (fillImage == null) return;

        var rt = fillImage.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(Mathf.Clamp01(nextRatio + fillGap), 1f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    // ==================== Upgrade & Save ====================

    private void OnUpgradePressed()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length) return;
        if (isUpgrading) return;

        var data = cannonHolderSO.cannonsData[selectedIndex];

        data.UpgradeCannon();

        UpdateStatsDisplay();
        if (cannonItems[selectedIndex].levelText != null)
            cannonItems[selectedIndex].levelText.text = $"{data.cannonLevel}";

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