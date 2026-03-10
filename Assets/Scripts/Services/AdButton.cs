// ============================================================================
// AdButtons.cs — Drag-and-Drop Button Actions for Ads
// ============================================================================
//
// HOW TO USE (No coding needed):
//
//   1. Attach this script to ANY GameObject in your scene (or the AdManager
//      GameObject itself — doesn't matter).
//
//   2. Select your Button in the Hierarchy.
//
//   3. In the Inspector, find the Button component → "On Click ()" section.
//
//   4. Click the "+" button to add a new entry.
//
//   5. Drag the GameObject that has THIS script into the object field.
//
//   6. From the dropdown, select:
//        AdButtons → ShowRewarded()         for rewarded ads
//        AdButtons → ShowInterstitial()     for interstitial ads
//        AdButtons → ShowBanner()           for showing banner
//        AdButtons → HideBanner()           for hiding banner
//        AdButtons → RemoveAds()            for "Remove Ads" purchase
//
//   That's it. No code needed. Just drag, drop, and select.
//
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

public class AdButtons : MonoBehaviour
{
    // ========================================================================
    // OPTIONAL REFERENCES — Drag these in Inspector if you want auto UI updates
    // ========================================================================

    [Header("=== Optional: Rewarded Ad Button ===")]
    [Tooltip("If assigned, this button auto-disables when no rewarded ad is ready")]
    [SerializeField] private Button rewardedButton;

    [Tooltip("Text on the rewarded button (auto-updates to show status)")]
    [SerializeField] private Text rewardedButtonText;

    [Header("=== Optional: Reward Settings ===")]
    [Tooltip("How many coins/gems to give when ad is watched")]
    [SerializeField] private int rewardAmount = 50;

    [Header("=== Optional: Interstitial Settings ===")]
    [Tooltip("Show interstitial only every N calls (prevents spamming players)")]
    [SerializeField] private int showInterstitialEvery = 3;

    // ========================================================================
    // PRIVATE STATE
    // ========================================================================

    private static int interstitialCallCount = 0;

    // ========================================================================
    // LIFECYCLE
    // ========================================================================

    private void OnEnable()
    {
        // Subscribe to events for UI updates and reward handling
        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardedAvailable += UpdateRewardedButtonUI;
            AdManager.Instance.OnRewardedUnavailable += UpdateRewardedButtonUI;
            AdManager.Instance.OnRewardedDismissed += UpdateRewardedButtonUI;
            AdManager.Instance.OnRewardGranted += HandleRewardGranted;
        }

        UpdateRewardedButtonUI();
    }

    private void OnDisable()
    {
        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardedAvailable -= UpdateRewardedButtonUI;
            AdManager.Instance.OnRewardedUnavailable -= UpdateRewardedButtonUI;
            AdManager.Instance.OnRewardedDismissed -= UpdateRewardedButtonUI;
            AdManager.Instance.OnRewardGranted -= HandleRewardGranted;
        }
    }

    // ========================================================================
    // PUBLIC METHODS — Assign these to Button OnClick() in Inspector
    // ========================================================================

    /// <summary>
    /// Show a rewarded ad. Assign this to your "Watch Ad" button's OnClick().
    /// </summary>
    public void ShowRewarded()
    {
        if (AdManager.Instance == null) return;

        bool shown = AdManager.Instance.ShowRewarded();

        if (!shown)
        {
            Debug.Log("[AdButtons] Rewarded ad not available right now.");
            // You can show a popup/toast to the player here
        }
    }

    /// <summary>
    /// Show an interstitial ad. Assign this to any button or call from level-complete.
    /// Respects the "showInterstitialEvery" setting to avoid spamming players.
    /// </summary>
    public void ShowInterstitial()
    {
        if (AdManager.Instance == null) return;

        interstitialCallCount++;

        if (interstitialCallCount >= showInterstitialEvery)
        {
            bool shown = AdManager.Instance.ShowInterstitial();
            if (shown)
            {
                interstitialCallCount = 0;
            }
        }
    }

    /// <summary>
    /// Show an interstitial ad immediately, ignoring the frequency counter.
    /// Use this when you ALWAYS want to show an ad (e.g., specific transition).
    /// </summary>
    public void ShowInterstitialNow()
    {
        if (AdManager.Instance == null) return;
        AdManager.Instance.ShowInterstitial();
    }

    /// <summary>
    /// Show banner ad. Assign to a button or call when entering a screen.
    /// </summary>
    public void ShowBanner()
    {
        if (AdManager.Instance == null) return;
        AdManager.Instance.ShowBanner();
    }

    /// <summary>
    /// Hide banner ad. Assign to a button or call when leaving a screen.
    /// </summary>
    public void HideBanner()
    {
        if (AdManager.Instance == null) return;
        AdManager.Instance.HideBanner();
    }

    /// <summary>
    /// Disable all ads permanently. Assign to your "Remove Ads" purchase button.
    /// Call this AFTER the purchase is confirmed successful.
    /// </summary>
    public void RemoveAds()
    {
        if (AdManager.Instance == null) return;

        PlayerPrefs.SetInt("AdsRemoved", 1);
        PlayerPrefs.Save();
        AdManager.Instance.AdsDisabled = true;

        Debug.Log("[AdButtons] Ads removed permanently!");
    }

    // ========================================================================
    // REWARD HANDLING
    // ========================================================================

    private void HandleRewardGranted(string rewardName, int amount)
    {
        // =====================================================================
        // *** PUT YOUR REWARD LOGIC HERE ***
        // =====================================================================
        //
        // Examples:
        //   PlayerData.Instance.AddCoins(rewardAmount);
        //   GameManager.Instance.AddLives(1);
        //   currencyText.text = PlayerData.Instance.Coins.ToString();
        //
        // The 'rewardAmount' field is set in the Inspector.
        // The 'amount' parameter comes from the LevelPlay dashboard config.
        //
        // Use whichever value makes sense for your game:
        //   - rewardAmount = what YOU set in Inspector (recommended)
        //   - amount = what the dashboard returns
        //
        // =====================================================================

        Debug.Log($"[AdButtons] Player earned reward! Giving {rewardAmount} to player.");

        // TODO: Replace this with your actual reward logic
        // Example: PlayerData.Instance.AddCoins(rewardAmount);
    }

    // ========================================================================
    // UI UPDATES
    // ========================================================================

    private void UpdateRewardedButtonUI()
    {
        if (rewardedButton == null) return;

        bool ready = AdManager.Instance != null && AdManager.Instance.IsRewardedReady;

        rewardedButton.interactable = ready;

        if (rewardedButtonText != null)
        {
            rewardedButtonText.text = ready
                ? $"Watch Ad (+{rewardAmount})"
                : "Loading Ad...";
        }
    }
}