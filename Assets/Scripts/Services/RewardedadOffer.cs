// ============================================================================
// RewardedAdOffer.cs — Flexible "Watch X Ads for Y Reward" Button
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System;

public class RewardedAdOffer : MonoBehaviour
{
    // ========================================================================
    // INSPECTOR — Configure each button differently
    // ========================================================================

    [Header("=== Reward Configuration ===")]
    [Tooltip("Type of reward: Gold, Gems, Lives, Skin, etc. (case-sensitive)")]
    [SerializeField] private string rewardType = "Gold";

    [Tooltip("How much to give when all ads are watched")]
    [SerializeField] private int rewardAmount = 7000;

    [Tooltip("How many ads the player must watch to earn this reward")]
    [SerializeField] private int adsRequired = 2;

    [Header("=== UI References (Drag from Hierarchy) ===")]
    [Tooltip("The button the player taps to watch an ad")]
    [SerializeField] private Button watchAdButton;

    [Tooltip("Text showing progress like '1/2 Ads' (optional)")]
    [SerializeField] private TMPro.TextMeshProUGUI progressText;

    [Tooltip("Text showing the reward like '7000 Gold' (optional)")]
    [SerializeField] private TMPro.TextMeshProUGUI rewardText;

    [Tooltip("Image or GameObject to show when reward is ready to claim (optional)")]
    [SerializeField] private GameObject claimReadyIndicator;

    [Tooltip("Image or GameObject to show a loading/unavailable state (optional)")]
    [SerializeField] private GameObject adNotReadyIndicator;

    // ========================================================================
    // STATIC EVENT — Your game's currency manager subscribes to this ONCE
    // ========================================================================

    /// <summary>
    /// Fires when the player has watched all required ads and earned the reward.
    /// Parameters: (string rewardType, int rewardAmount)
    /// 
    /// Subscribe from your currency/inventory manager:
    ///   RewardedAdOffer.OnRewardClaimed += (type, amount) => { ... };
    /// </summary>
    public static event Action<string, int> OnRewardClaimed;

    /// <summary>
    /// Fires every time the player completes one ad (even if more are needed).
    /// Parameters: (string rewardType, int adsWatched, int adsRequired)
    /// Useful for UI animations, sound effects, etc.
    /// </summary>
    public static event Action<string, int, int> OnAdProgressUpdated;

    // ========================================================================
    // PRIVATE STATE
    // ========================================================================

    private int adsWatched = 0;
    private bool isWaitingForReward = false;

    // ========================================================================
    // LIFECYCLE
    // ========================================================================

    private void OnEnable()
    {
        // Subscribe to AdManager events
        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardGranted += HandleRewardGranted;
            AdManager.Instance.OnRewardedAvailable += UpdateUI;
            AdManager.Instance.OnRewardedUnavailable += UpdateUI;
            AdManager.Instance.OnRewardedDismissed += HandleAdDismissed;
        }

        // Set up button click
        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnButtonClicked);

        UpdateUI();
        UpdateRewardText();
    }

    private void OnDisable()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardGranted -= HandleRewardGranted;
            AdManager.Instance.OnRewardedAvailable -= UpdateUI;
            AdManager.Instance.OnRewardedUnavailable -= UpdateUI;
            AdManager.Instance.OnRewardedDismissed -= HandleAdDismissed;
        }

        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(OnButtonClicked);
    }
    //helpere method to get currency type
    private CurrencyType? GetCurrencyType()
    {
        switch (rewardType)
        {
            case "Gold": return CurrencyType.Gold;
            case "Gems": return CurrencyType.Gems;
            case "Power": return CurrencyType.Power;
            default: return null;  // non-currency rewards (Skin, Lives, etc.)
        }
    }
    // ========================================================================
    // BUTTON CLICK
    // ========================================================================

    private void OnButtonClicked()
    {
        if (AdManager.Instance == null) return;

        isWaitingForReward = true;
        bool shown = AdManager.Instance.ShowRewarded();

        if (!shown)
        {
            isWaitingForReward = false;
            Debug.Log($"[RewardedAdOffer] {rewardType}: Ad not available.");
        }
    }

    // ========================================================================
    // AD CALLBACKS
    // ========================================================================

    private void HandleRewardGranted(string rewardName, int amount)
    {
        if (!isWaitingForReward) return;
        isWaitingForReward = false;

        adsWatched++;
        Debug.Log($"[RewardedAdOffer] {rewardType}: Ad {adsWatched}/{adsRequired} completed.");

        OnAdProgressUpdated?.Invoke(rewardType, adsWatched, adsRequired);

        if (adsWatched >= adsRequired)
        {
            Debug.Log($"[RewardedAdOffer] Reward claimed: {rewardAmount} {rewardType}!");
            OnRewardClaimed?.Invoke(rewardType, rewardAmount);

            // ═══ FLOW EFFECT — from this button ═════════════
            CurrencyType? type = GetCurrencyType();
            if (type.HasValue && watchAdButton != null)
            {
                Vector2 buttonPos = watchAdButton.transform.position;
                GameEvent.CurrencyCollected(type.Value, buttonPos, rewardAmount);
            }
            // ════════════════════════════════════════════════

            adsWatched = 0;
        }

        UpdateUI();
    }

    private void HandleAdDismissed()
    {
        // Player closed the ad (might not have earned reward)
        isWaitingForReward = false;
        UpdateUI();
    }

    // ========================================================================
    // UI UPDATES
    // ========================================================================

    private void UpdateUI()
    {
        bool adReady = AdManager.Instance != null && AdManager.Instance.IsRewardedReady;

        // Button interactability
        if (watchAdButton != null)
            watchAdButton.interactable = adReady;

        // Progress text: "0/2 Ads", "1/2 Ads", etc.
        if (progressText != null)
        {
            if (adsWatched > 0)
                progressText.text = $"{adsWatched}/{adsRequired} Ads";
            else
                progressText.text = $"{adsRequired} Ads";
        }

        // Indicators
        if (claimReadyIndicator != null)
            claimReadyIndicator.SetActive(adsWatched >= adsRequired - 1 && adsWatched > 0);

        if (adNotReadyIndicator != null)
            adNotReadyIndicator.SetActive(!adReady);
    }

    private void UpdateRewardText()
    {
        if (rewardText != null)
            rewardText.text = $"{rewardAmount} {rewardType}";
    }

    // ========================================================================
    // PUBLIC HELPERS
    // ========================================================================

    /// <summary>Reset the ad watch progress (e.g., on new session or daily reset).</summary>
    public void ResetProgress()
    {
        adsWatched = 0;
        UpdateUI();
    }

    /// <summary>Get current progress as a fraction (0.0 to 1.0).</summary>
    public float GetProgress()
    {
        return (float)adsWatched / adsRequired;
    }

    /// <summary>How many more ads need to be watched.</summary>
    public int AdsRemaining => adsRequired - adsWatched;
}