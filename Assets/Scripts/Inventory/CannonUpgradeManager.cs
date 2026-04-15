
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// Manages cannon upgrades.
///
/// COST RULES:
///   Base costs (1× multiplier):
///     L1→2: 500   L2→3: 800    L3→4: 1,200  L4→5: 1,800  L5→6: 2,700
///     L6→7: 4,000 L7→8: 6,000  L8→9: 9,000  L9→10: 13,500
///     Beyond L10: previous cost × 1.5, rounded to nearest 50
///
///   Multipliers:
///     Index 4 – Big Bartha    → ×2
///     Index 6 – Triple Bullet → ×3
///     All others              → ×1
///
/// FILL BARS (10 equal segments):
///   Level 1 = 10%,  Level 5 = 50%,  Level 10 = 100%.
///   CurrentFill (bright green) = current level × 10%.
///   NextFill    (light green)  = (current level + 1) × 10% — preview.
///   On upgrade, CurrentFill animates smoothly to the next segment.
///   All three bars fill identically since they represent overall cannon level.
///
/// SPRITE SETUP:
///   1. Open each fill sprite in Sprite Editor → set Border L/R for rounded ends.
///   2. Inspector: Image Type → Sliced on all fill Images.
///   3. Hierarchy per bar: NextFill first (behind), CurrentFill second (in front).
///   4. Both anchored stretch: anchorMin(0,0) anchorMax(1,1), offsets zero.
/// </summary>
public class CannonUpgradeManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

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

    [Header("Stats Fill — Current Level (Image Type = Sliced, bright green)")]
    [SerializeField] private Image damageFillCurrent;
    [SerializeField] private Image healthFillCurrent;
    [SerializeField] private Image fireRateFillCurrent;

    [Header("Stats Fill — Next Level Preview (Image Type = Sliced, light green)")]
    [SerializeField] private Image damageFillNext;
    [SerializeField] private Image healthFillNext;
    [SerializeField] private Image fireRateFillNext;

    [Header("Fill Animation")]
    [SerializeField] private float fillAnimDuration = 0.35f;

    [Header("Max Level (bar divided into this many equal segments)")]
    [SerializeField] private int maxCannonLevel = 10;

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

    [Header("Cost Display (inside upgrade panel, updates on cannon select)")]
    [SerializeField] private GameObject costContainer;
    [SerializeField] private TextMeshProUGUI costText;

    [Header("Not Enough Gold Text (fades in/out on failed upgrade)")]
    [SerializeField] private TextMeshProUGUI notEnoughGoldText;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float holdDuration = 1.0f;

    [Header("Description")]
    [SerializeField] private TextMeshProUGUI cannonDescriptionText;

    [Header("Visual States")]
    [SerializeField] private Sprite activeBg;
    [SerializeField] private Sprite normalBg;

    // ── Constants ─────────────────────────────────────────────────────────────

    private const string CANNON_LEVEL_SUFFIX = "_CannonLevel";

    private static readonly int[] BaseLevelCosts =
    {
        500,    // 1 → 2
        800,    // 2 → 3
        1200,   // 3 → 4
        1800,   // 4 → 5
        2700,   // 5 → 6
        4000,   // 6 → 7
        6000,   // 7 → 8
        9000,   // 8 → 9
        13500,  // 9 → 10
    };

    private static readonly Dictionary<int, float> CostMultipliers = new()
    {
        { 4, 2f }, // Big Bartha
        { 6, 3f }, // Triple Bullet
    };

    // ── Runtime State ─────────────────────────────────────────────────────────

    private int selectedIndex = -1;
    private bool isUpgrading = false;
    private bool isFadeRunning = false;

    // Previous fill ratio — animate FROM this value on upgrade
    private float prevFillRatio;

    // ── Events ────────────────────────────────────────────────────────────────

    public static event Action<int> OnCannonUpgraded;
    public static event Action OnAllLevelsLoaded;

    // ═════════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradePressed);

        if (notEnoughGoldText != null)
        {
            notEnoughGoldText.text = "Not enough Gold!";
            SetTextAlpha(notEnoughGoldText, 0f);
        }
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
        CacheAllBaseValues();
        LoadAndReplayAll().Forget();
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradePressed);

        foreach (var item in cannonItems)
            if (item.button != null)
                item.button.onClick.RemoveAllListeners();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cost Calculation
    // ═════════════════════════════════════════════════════════════════════════

    public int GetUpgradeCost(int cannonIndex)
    {
        if (cannonHolderSO == null || cannonIndex < 0 || cannonIndex >= cannonHolderSO.cannonsData.Length)
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

        float cost = BaseLevelCosts[BaseLevelCosts.Length - 1];
        int extra = tableIndex - (BaseLevelCosts.Length - 1);
        for (int i = 0; i < extra; i++)
            cost *= 1.5f;

        return Mathf.RoundToInt(cost / 50f) * 50;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Lock / Unlock Handlers
    // ═════════════════════════════════════════════════════════════════════════

    private void OnCannonUnlockedHandler(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (CannonLockManager.Instance == null || cannonItems[index].button == null) return;

        CannonLockManager.Instance.ApplyUnlockedVisual(cannonItems[index].button.transform);
        cannonItems[index].button.interactable = true;
    }

    private void RefreshAllLockVisuals()
    {
        if (CannonLockManager.Instance == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (cannonItems[i].button == null) continue;

            bool unlocked = CannonLockManager.Instance.IsUnlocked(i);
            if (unlocked)
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

    // ═════════════════════════════════════════════════════════════════════════
    // Base Value Caching
    // ═════════════════════════════════════════════════════════════════════════

    private void CacheAllBaseValues()
    {
        if (cannonHolderSO == null) return;
        foreach (var cannon in cannonHolderSO.cannonsData)
            cannon.CacheBaseValues();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Load & Replay
    // ═════════════════════════════════════════════════════════════════════════

    private string GetCannonKey(int index)
        => cannonHolderSO.cannonsData[index].cannonName.Replace(" ", "") + CANNON_LEVEL_SUFFIX;

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
                int savedLevel = data.ContainsKey(key) ? data[key].Value.GetAs<int>() : 0;
                cannonHolderSO.cannonsData[i].ReplayToLevel(savedLevel);
            }

            Debug.Log("[CannonUpgrade] All cannon levels loaded from cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonUpgrade] Failed to load cannon levels: {ex.Message}");
        }

        InitializeItems();
        RefreshAllLockVisuals();
        OnAllLevelsLoaded?.Invoke();

        if (cannonItems.Count > 0)
            SelectCannon(0);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Initialization
    // ═════════════════════════════════════════════════════════════════════════

    private void InitializeItems()
    {
        if (cannonHolderSO == null) return;

        for (int i = 0; i < cannonItems.Count; i++)
        {
            if (i >= cannonHolderSO.cannonsData.Length) break;

            int index = i;
            var item = cannonItems[i];
            var cannonData = cannonHolderSO.cannonsData[i];

            if (item.cannonSprite == null)
                item.cannonSprite = cannonData.cannonSprite;

            if (item.levelText != null)
                item.levelText.text = $"{cannonData.cannonLevel}";

            if (item.button != null)
                item.button.onClick.AddListener(() => SelectCannon(index));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Selection
    // ═════════════════════════════════════════════════════════════════════════

    private void SelectCannon(int index)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (CannonLockManager.Instance != null && !CannonLockManager.Instance.IsUnlocked(index)) return;

        if (selectedIndex >= 0 && selectedIndex < cannonItems.Count)
            SetItemBg(selectedIndex, false);

        selectedIndex = index;
        SetItemBg(selectedIndex, true);

        UpdateStatsDisplay();
        RefreshCostText();
        RefreshUpgradeButton();
    }

    private void SetItemBg(int index, bool active)
    {
        if (index < 0 || index >= cannonItems.Count) return;
        if (cannonItems[index].backgroundImage != null)
            cannonItems[index].backgroundImage.sprite = active ? activeBg : normalBg;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Stats Display
    // ═════════════════════════════════════════════════════════════════════════

    private void UpdateStatsDisplay()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length) return;

        var data = cannonHolderSO.cannonsData[selectedIndex];

        if (selectedCannonImage != null) selectedCannonImage.sprite = data.cannonSprite;
        if (nameText != null) nameText.text = data.cannonName;
        if (cannonDescriptionText != null) cannonDescriptionText.text = data.cannonDescription;
        if (statsLevelText != null) statsLevelText.text = $"Level {data.cannonLevel}";
        if (damageText != null) damageText.text = "DAMAGE:";
        if (healthText != null) healthText.text = "HEALTH:";
        if (fireRateText != null) fireRateText.text = "FIRE RATE:";

        SnapFills();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // 10-Segment Fill System
    //
    //   Level 1  → 10%    Level 6  → 60%
    //   Level 2  → 20%    Level 7  → 70%
    //   Level 3  → 30%    Level 8  → 80%
    //   Level 4  → 40%    Level 9  → 90%
    //   Level 5  → 50%    Level 10 → 100%
    //
    // All three stat bars fill identically — they represent cannon level.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts cannon level (1–10) to fill ratio (0.1–1.0).
    /// </summary>
    private float LevelToRatio(int level)
    {
        if (level <= 0) return 0f;
        return Mathf.Clamp01((float)level / maxCannonLevel);
    }

    /// <summary>
    /// Sets a Sliced image width via anchorMax.x.
    /// Image Type = Sliced in Inspector preserves rounded caps.
    /// </summary>
    private void SetFill(Image img, float ratio)
    {
        if (img == null) return;

        if (ratio <= 0f)
        {
            img.enabled = false;
            return;
        }

        img.enabled = true;

        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Instantly snaps all fill bars. Called on selection / load.
    /// </summary>
    private void SnapFills()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length) return;

        int level = cannonHolderSO.cannonsData[selectedIndex].cannonLevel;
        float currentRatio = LevelToRatio(level);
        float nextRatio = LevelToRatio(level + 1);

        // Bright green — current level
        SetFill(damageFillCurrent, currentRatio);
        SetFill(healthFillCurrent, currentRatio);
        SetFill(fireRateFillCurrent, currentRatio);

        // Light green — next level preview
        if (level < maxCannonLevel)
        {
            SetFill(damageFillNext, nextRatio);
            SetFill(healthFillNext, nextRatio);
            SetFill(fireRateFillNext, nextRatio);
        }
        else
        {
            SetFill(damageFillNext, currentRatio);
            SetFill(healthFillNext, currentRatio);
            SetFill(fireRateFillNext, currentRatio);
        }

        prevFillRatio = currentRatio;
    }

    /// <summary>
    /// Smoothly animates fills after upgrade. e.g. 30% → 40%.
    /// </summary>
    private async UniTask AnimateUpgradeFills()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length) return;

        int level = cannonHolderSO.cannonsData[selectedIndex].cannonLevel;
        float newRatio = LevelToRatio(level);

        // Animate current fills
        float elapsed = 0f;
        while (elapsed < fillAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fillAnimDuration);
            float lerped = Mathf.Lerp(prevFillRatio, newRatio, t);

            SetFill(damageFillCurrent, lerped);
            SetFill(healthFillCurrent, lerped);
            SetFill(fireRateFillCurrent, lerped);

            await UniTask.Yield();
        }

        // Snap exact
        SetFill(damageFillCurrent, newRatio);
        SetFill(healthFillCurrent, newRatio);
        SetFill(fireRateFillCurrent, newRatio);

        // Update next preview
        if (level < maxCannonLevel)
        {
            float nextRatio = LevelToRatio(level + 1);
            SetFill(damageFillNext, nextRatio);
            SetFill(healthFillNext, nextRatio);
            SetFill(fireRateFillNext, nextRatio);
        }
        else
        {
            SetFill(damageFillNext, newRatio);
            SetFill(healthFillNext, newRatio);
            SetFill(fireRateFillNext, newRatio);
        }

        prevFillRatio = newRatio;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cost Text + Upgrade Button
    // ═════════════════════════════════════════════════════════════════════════

    private void RefreshCostText()
    {
        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length)
        {
            if (costContainer != null) costContainer.SetActive(false);
            return;
        }

        var data = cannonHolderSO.cannonsData[selectedIndex];

        if (data.cannonLevel >= maxCannonLevel)
        {
            if (costContainer != null) costContainer.SetActive(false);
            return;
        }

        if (costContainer != null) costContainer.SetActive(true);
        if (costText != null)
        {
            int cost = GetUpgradeCost(selectedIndex);
            costText.text = $"{cost:N0}";
        }
    }

    private void RefreshUpgradeButton()
    {
        if (upgradeButton == null) return;

        if (isUpgrading)
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADING...";
            upgradeButton.interactable = false;
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= cannonHolderSO.cannonsData.Length)
        {
            upgradeButton.interactable = false;
            return;
        }

        var data = cannonHolderSO.cannonsData[selectedIndex];

        if (data.cannonLevel >= maxCannonLevel)
        {
            if (upgradeButtonText != null) upgradeButtonText.text = "MAX LEVEL";
            upgradeButton.interactable = false;
            return;
        }

        if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";
        upgradeButton.interactable = true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Upgrade Button Press
    // ═════════════════════════════════════════════════════════════════════════

    private void OnUpgradePressed()
    {
        if (selectedIndex < 0 || isUpgrading) return;

        var data = cannonHolderSO.cannonsData[selectedIndex];
        if (data.cannonLevel >= maxCannonLevel) return;

        ExecuteUpgradeAsync(selectedIndex).Forget();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Execute Upgrade
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid ExecuteUpgradeAsync(int index)
    {
        isUpgrading = true;
        RefreshUpgradeButton();

        var data = cannonHolderSO.cannonsData[index];
        int cost = GetUpgradeCost(index);

        // ── Spend Gold ────────────────────────────────────────────────────
        bool spent = await CurrencyManager.Instance.SpendGold(cost);

        if (!spent)
        {
            ShowNotEnoughGold().Forget();
            isUpgrading = false;
            RefreshUpgradeButton();
            return;
        }

        // ── Apply upgrade locally ─────────────────────────────────────────
        data.UpgradeCannon();

        // Update level badge
        if (index < cannonItems.Count && cannonItems[index].levelText != null)
            cannonItems[index].levelText.text = $"{data.cannonLevel}";

        // Refresh display + animate fills
        if (index == selectedIndex)
        {
            if (selectedCannonImage != null) selectedCannonImage.sprite = data.cannonSprite;
            if (statsLevelText != null) statsLevelText.text = $"Level {data.cannonLevel}";

            RefreshCostText();
            RefreshUpgradeButton();

            await AnimateUpgradeFills();
        }

        // ── Save to cloud ─────────────────────────────────────────────────
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(GetCannonKey(index), data.cannonLevel);
            Debug.Log($"[CannonUpgrade] {data.cannonName} → Level {data.cannonLevel} saved.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonUpgrade] Failed to save level: {ex.Message}");
        }
        finally
        {
            isUpgrading = false;
            RefreshUpgradeButton();
        }

        OnCannonUpgraded?.Invoke(index);
        Debug.Log($"[CannonUpgrade] {data.cannonName} upgraded to Level {data.cannonLevel} for {cost} Gold.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Fading "Not enough Gold!" Text
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid ShowNotEnoughGold()
    {
        if (notEnoughGoldText == null) return;

        isFadeRunning = true;
        SetTextAlpha(notEnoughGoldText, 0f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(notEnoughGoldText, Mathf.Clamp01(elapsed / fadeDuration));
            await UniTask.Yield();
        }
        SetTextAlpha(notEnoughGoldText, 1f);

        await UniTask.Delay(TimeSpan.FromSeconds(holdDuration));

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(notEnoughGoldText, Mathf.Clamp01(1f - elapsed / fadeDuration));
            await UniTask.Yield();
        }
        SetTextAlpha(notEnoughGoldText, 0f);

        isFadeRunning = false;
    }

    private static void SetTextAlpha(TextMeshProUGUI tmp, float alpha)
    {
        Color c = tmp.color;
        c.a = alpha;
        tmp.color = c;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════════════

    public int GetSelectedIndex() => selectedIndex;

    public void RefreshDisplay()
    {
        if (selectedIndex >= 0)
            UpdateStatsDisplay();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class CannonUpgradeItemUI
{
    public Button button;
    public Image backgroundImage;
    public Sprite cannonSprite;
    public TextMeshProUGUI levelText;
}