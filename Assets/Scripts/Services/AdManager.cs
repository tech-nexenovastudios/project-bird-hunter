// ============================================================================
// AdManager.cs — Central Ad Controller for LevelPlay Mediation
// ============================================================================
// ARCHITECTURE:
//   Your Game Code  →  AdManager (this file)  →  LevelPlay SDK  →  Ad Networks
//
//   Your game NEVER touches LevelPlay directly. If SDK APIs change or you
//   switch mediation platforms, you only update this one file.
//
// SETUP:
//   1. Create an empty GameObject in your FIRST scene.
//   2. Attach this script to it.
//   3. Fill in App Key and Ad Unit IDs in the Inspector.
//   4. That's it. The script handles everything else.
//
// USAGE FROM YOUR GAME:
//   AdManager.Instance.ShowRewarded();              // show rewarded ad
//   AdManager.Instance.ShowInterstitial();          // show interstitial
//   AdManager.Instance.ShowBanner();                // show banner
//   AdManager.Instance.HideBanner();                // hide banner
//   bool ready = AdManager.Instance.IsRewardedReady;  // check availability
//
// LISTEN TO EVENTS:
//   AdManager.Instance.OnRewardGranted += (name, amount) => { give coins; };
//   AdManager.Instance.OnInterstitialDismissed += () => { load next level; };
// ============================================================================

using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdManager : MonoBehaviour
{
    // ========================================================================
    // INSPECTOR CONFIGURATION
    // ========================================================================

    [Header("=== LevelPlay Settings ===")]
    [Tooltip("App Key from LevelPlay dashboard")]
    [SerializeField] private string appKey = "";

    [Header("=== Ad Unit IDs (from LevelPlay Dashboard) ===")]
    [Tooltip("Rewarded ad unit ID — leave empty to disable")]
    [SerializeField] private string rewardedAdUnitId = "";

    [Tooltip("Interstitial ad unit ID — leave empty to disable")]
    [SerializeField] private string interstitialAdUnitId = "";

    [Tooltip("Banner ad unit ID — leave empty to disable")]
    [SerializeField] private string bannerAdUnitId = "";

    [Header("=== Behavior Settings ===")]
    [Tooltip("Enable detailed logs in console (disable for production builds)")]
    [SerializeField] private bool enableDebugLogs = true;

    [Tooltip("Seconds to wait before retrying a failed ad load")]
    [SerializeField] private float retryBaseDelay = 5f;

    [Tooltip("Maximum retry delay in seconds (caps exponential backoff)")]
    [SerializeField] private float retryMaxDelay = 120f;

    [Tooltip("If true, no ads will load or show (e.g., for premium/no-ad users)")]
    [SerializeField] private bool adsDisabled = false;

    // ========================================================================
    // PUBLIC EVENTS — Subscribe from your game scripts
    // ========================================================================

    // --- SDK ---
    /// <summary>SDK is initialized and ready. Ad objects are created.</summary>
    public event Action OnSDKReady;

    // --- Rewarded ---
    /// <summary>Player completed the video and deserves a reward.</summary>
    public event Action<string, int> OnRewardGranted;         // (rewardName, amount)
    /// <summary>Rewarded ad is now loaded and available to show.</summary>
    public event Action OnRewardedAvailable;
    /// <summary>Rewarded ad is no longer available (shown, failed, or not loaded).</summary>
    public event Action OnRewardedUnavailable;
    /// <summary>Rewarded ad was closed (regardless of reward status).</summary>
    public event Action OnRewardedDismissed;

    // --- Interstitial ---
    /// <summary>Interstitial ad is now loaded and available to show.</summary>
    public event Action OnInterstitialAvailable;
    /// <summary>Interstitial ad is no longer available.</summary>
    public event Action OnInterstitialUnavailable;
    /// <summary>Interstitial was closed by the user.</summary>
    public event Action OnInterstitialDismissed;

    // --- Banner ---
    /// <summary>Banner ad loaded and is visible on screen.</summary>
    public event Action OnBannerVisible;
    /// <summary>Banner failed to load.</summary>
    public event Action OnBannerFailed;

    // ========================================================================
    // SINGLETON
    // ========================================================================

    public static AdManager Instance { get; private set; }

    // ========================================================================
    // PUBLIC READ-ONLY STATE
    // ========================================================================

    /// <summary>True after LevelPlay has initialized successfully.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>True if a rewarded ad is loaded and ready to show.</summary>
    public bool IsRewardedReady => rewardedAd != null && rewardedAd.IsAdReady();

    /// <summary>True if an interstitial ad is loaded and ready to show.</summary>
    public bool IsInterstitialReady => interstitialAd != null && interstitialAd.IsAdReady();

    /// <summary>True if banner is currently active on screen.</summary>
    public bool IsBannerActive { get; private set; }

    /// <summary>Toggle ads on/off at runtime (e.g., user bought "Remove Ads").</summary>
    public bool AdsDisabled
    {
        get => adsDisabled;
        set
        {
            adsDisabled = value;
            if (adsDisabled)
            {
                HideBanner();
                DestroyBanner();
            }
            Log("Ads " + (value ? "DISABLED" : "ENABLED"));
        }
    }

    // ========================================================================
    // PRIVATE STATE
    // ========================================================================

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayBannerAd bannerAd;

    // Retry tracking — exponential backoff per ad type
    private int rewardedRetryCount;
    private int interstitialRetryCount;

    // Prevent duplicate load calls
    private bool isRewardedLoading;
    private bool isInterstitialLoading;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void Awake()
    {
        // --- Singleton Setup ---
        if (Instance != null && Instance != this)
        {
            Log("Duplicate AdManager found — destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (adsDisabled)
        {
            Log("Ads are disabled. Skipping initialization.");
            return;
        }

        InitializeSDK();
    }
    public void TestLevelplay()
    {
        LevelPlay.LaunchTestSuite();
    }
    private void OnDestroy()
    {
        if (Instance != this) return;

        // Unsubscribe SDK events
        LevelPlay.OnInitSuccess -= HandleInitSuccess;
        LevelPlay.OnInitFailed -= HandleInitFailed;

        // Cleanup ad objects
        CleanupRewarded();
        CleanupInterstitial();
        CleanupBanner();

        Instance = null;
    }

    // ========================================================================
    // SDK INITIALIZATION
    // ========================================================================

    private void InitializeSDK()
    {
        if (string.IsNullOrEmpty(appKey))
        {
            LogError("App Key is empty! Set it in the Inspector.");
            return;
        }

        Debug.Log($"[AdManager] About to init with key: {appKey}");

        LevelPlay.OnInitSuccess += HandleInitSuccess;
        LevelPlay.OnInitFailed += HandleInitFailed;

        LevelPlay.Init(appKey);
        Log("Initializing LevelPlay SDK...");
    }

    // FIX: OnInitSuccess passes LevelPlayConfiguration parameter in SDK 9.x
    private void HandleInitSuccess(LevelPlayConfiguration configuration)
    {
        IsInitialized = true;
        Log("SDK initialized successfully!");

        // Create ad objects (only for ad units that have IDs configured)
        if (!string.IsNullOrEmpty(rewardedAdUnitId))
        {
            CreateRewardedAd();
            LoadRewarded();
        }

        if (!string.IsNullOrEmpty(interstitialAdUnitId))
        {
            CreateInterstitialAd();
            LoadInterstitial();
        }

        // Banner is NOT pre-loaded — call ShowBanner() when you want it
        OnSDKReady?.Invoke();
    }

    private void HandleInitFailed(LevelPlayInitError error)
    {
        IsInitialized = false;
        LogError($"SDK init failed: {error.ErrorMessage}");

        // Retry after 30 seconds
        Invoke(nameof(RetryInit), 30f);
    }

    private void RetryInit()
    {
        Log("Retrying SDK initialization...");
        LevelPlay.Init(appKey);
    }

    // ========================================================================
    // REWARDED ADS
    // ========================================================================

    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += Rewarded_OnLoaded;
        rewardedAd.OnAdLoadFailed += Rewarded_OnLoadFailed;
        rewardedAd.OnAdDisplayed += Rewarded_OnDisplayed;
        rewardedAd.OnAdDisplayFailed += Rewarded_OnDisplayFailed;
        rewardedAd.OnAdRewarded += Rewarded_OnRewarded;
        rewardedAd.OnAdClosed += Rewarded_OnClosed;
        rewardedAd.OnAdClicked += Rewarded_OnClicked;

        Log("Rewarded ad object created.");
    }

    /// <summary>Load a rewarded ad. Called automatically — you rarely need this.</summary>
    public void LoadRewarded()
    {
        if (adsDisabled || !IsInitialized || rewardedAd == null) return;
        if (isRewardedLoading || IsRewardedReady) return;

        isRewardedLoading = true;
        rewardedAd.LoadAd();
        Log("Loading rewarded ad...");
    }

    /// <summary>
    /// Show a rewarded ad. Returns true if the ad started displaying.
    /// Optionally pass a placement name for analytics/capping.
    /// </summary>
    public bool ShowRewarded(string placement = null)
    {
        if (adsDisabled)
        {
            Log("Ads disabled — cannot show rewarded.");
            return false;
        }

        if (!IsRewardedReady)
        {
            LogWarning("Rewarded ad not ready.");
            OnRewardedUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement)
            && LevelPlayRewardedAd.IsPlacementCapped(placement))
        {
            LogWarning($"Rewarded placement '{placement}' is capped.");
            OnRewardedUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement))
            rewardedAd.ShowAd(placementName: placement);
        else
            rewardedAd.ShowAd();

        return true;
    }

    // --- Rewarded Callbacks ---

    private void Rewarded_OnLoaded(LevelPlayAdInfo info)
    {
        isRewardedLoading = false;
        rewardedRetryCount = 0; // Reset backoff on success
        Log($"Rewarded loaded — network: {info.AdNetwork}");
        OnRewardedAvailable?.Invoke();
    }

    private void Rewarded_OnLoadFailed(LevelPlayAdError error)
    {
        isRewardedLoading = false;
        LogWarning($"Rewarded load failed: {error.ErrorMessage}");
        OnRewardedUnavailable?.Invoke();
        ScheduleRetry(ref rewardedRetryCount, nameof(LoadRewarded));
    }

    private void Rewarded_OnDisplayed(LevelPlayAdInfo info)
    {
        Log("Rewarded ad displayed.");
        PauseGame(true);
    }

    private void Rewarded_OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        LogWarning($"Rewarded display failed: {error.ErrorMessage}");
        PauseGame(false);
        OnRewardedUnavailable?.Invoke();
        LoadRewarded();
    }

    // SDK signature: (LevelPlayAdInfo first, LevelPlayReward second)
    private void Rewarded_OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        Log($"Reward granted: {reward.Name} x{reward.Amount}");
        OnRewardGranted?.Invoke(reward.Name, reward.Amount);
    }

    private void Rewarded_OnClosed(LevelPlayAdInfo info)
    {
        Log("Rewarded ad closed.");
        PauseGame(false);
        OnRewardedDismissed?.Invoke();
        LoadRewarded(); // Pre-load next ad
    }

    private void Rewarded_OnClicked(LevelPlayAdInfo info)
    {
        Log("Rewarded ad clicked.");
    }

    private void CleanupRewarded()
    {
        if (rewardedAd == null) return;

        rewardedAd.OnAdLoaded -= Rewarded_OnLoaded;
        rewardedAd.OnAdLoadFailed -= Rewarded_OnLoadFailed;
        rewardedAd.OnAdDisplayed -= Rewarded_OnDisplayed;
        rewardedAd.OnAdDisplayFailed -= Rewarded_OnDisplayFailed;
        rewardedAd.OnAdRewarded -= Rewarded_OnRewarded;
        rewardedAd.OnAdClosed -= Rewarded_OnClosed;
        rewardedAd.OnAdClicked -= Rewarded_OnClicked;

        rewardedAd = null;
    }

    // ========================================================================
    // INTERSTITIAL ADS
    // ========================================================================

    private void CreateInterstitialAd()
    {
        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);

        interstitialAd.OnAdLoaded += Interstitial_OnLoaded;
        interstitialAd.OnAdLoadFailed += Interstitial_OnLoadFailed;
        interstitialAd.OnAdDisplayed += Interstitial_OnDisplayed;
        interstitialAd.OnAdDisplayFailed += Interstitial_OnDisplayFailed;
        interstitialAd.OnAdClosed += Interstitial_OnClosed;
        interstitialAd.OnAdClicked += Interstitial_OnClicked;

        Log("Interstitial ad object created.");
    }

    /// <summary>Load an interstitial ad. Called automatically — you rarely need this.</summary>
    public void LoadInterstitial()
    {
        if (adsDisabled || !IsInitialized || interstitialAd == null) return;
        if (isInterstitialLoading || IsInterstitialReady) return;

        isInterstitialLoading = true;
        interstitialAd.LoadAd();
        Log("Loading interstitial ad...");
    }

    /// <summary>
    /// Show an interstitial ad. Returns true if the ad started displaying.
    /// Optionally pass a placement name for analytics/capping.
    /// </summary>
    public bool ShowInterstitial(string placement = null)
    {
        if (adsDisabled)
        {
            Log("Ads disabled — cannot show interstitial.");
            return false;
        }

        if (!IsInterstitialReady)
        {
            LogWarning("Interstitial ad not ready.");
            OnInterstitialUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement)
            && LevelPlayInterstitialAd.IsPlacementCapped(placement))
        {
            LogWarning($"Interstitial placement '{placement}' is capped.");
            OnInterstitialUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement))
            interstitialAd.ShowAd(placementName: placement);
        else
            interstitialAd.ShowAd();

        return true;
    }

    // --- Interstitial Callbacks ---

    private void Interstitial_OnLoaded(LevelPlayAdInfo info)
    {
        isInterstitialLoading = false;
        interstitialRetryCount = 0;
        Log($"Interstitial loaded — network: {info.AdNetwork}");
        OnInterstitialAvailable?.Invoke();
    }

    private void Interstitial_OnLoadFailed(LevelPlayAdError error)
    {
        isInterstitialLoading = false;
        LogWarning($"Interstitial load failed: {error.ErrorMessage}");
        OnInterstitialUnavailable?.Invoke();
        ScheduleRetry(ref interstitialRetryCount, nameof(LoadInterstitial));
    }

    private void Interstitial_OnDisplayed(LevelPlayAdInfo info)
    {
        Log("Interstitial displayed.");
        PauseGame(true);
    }

    private void Interstitial_OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        LogWarning($"Interstitial display failed: {error.ErrorMessage}");
        PauseGame(false);
        OnInterstitialUnavailable?.Invoke();
        LoadInterstitial();
    }

    private void Interstitial_OnClosed(LevelPlayAdInfo info)
    {
        Log("Interstitial closed.");
        PauseGame(false);
        OnInterstitialDismissed?.Invoke();
        LoadInterstitial(); // Pre-load next ad
    }

    private void Interstitial_OnClicked(LevelPlayAdInfo info)
    {
        Log("Interstitial clicked.");
    }

    private void CleanupInterstitial()
    {
        if (interstitialAd == null) return;

        interstitialAd.OnAdLoaded -= Interstitial_OnLoaded;
        interstitialAd.OnAdLoadFailed -= Interstitial_OnLoadFailed;
        interstitialAd.OnAdDisplayed -= Interstitial_OnDisplayed;
        interstitialAd.OnAdDisplayFailed -= Interstitial_OnDisplayFailed;
        interstitialAd.OnAdClosed -= Interstitial_OnClosed;
        interstitialAd.OnAdClicked -= Interstitial_OnClicked;

        interstitialAd = null;
    }

    // ========================================================================
    // BANNER ADS
    // ========================================================================

    /// <summary>Create and show a banner ad at the bottom of the screen.</summary>
    public void ShowBanner()
    {
        if (adsDisabled || !IsInitialized) return;
        if (string.IsNullOrEmpty(bannerAdUnitId)) return;

        if (bannerAd == null)
        {
            bannerAd = new LevelPlayBannerAd(bannerAdUnitId);

            bannerAd.OnAdLoaded += Banner_OnLoaded;
            bannerAd.OnAdLoadFailed += Banner_OnLoadFailed;
            bannerAd.OnAdClicked += Banner_OnClicked;

            Log("Banner ad object created.");
        }

        bannerAd.LoadAd();
        Log("Loading banner ad...");
    }

    /// <summary>Hide the banner but keep it in memory (can unhide later).</summary>
    public void HideBanner()
    {
        if (bannerAd == null) return;

        bannerAd.HideAd();
        IsBannerActive = false;
        Log("Banner hidden.");
    }

    /// <summary>Show a previously hidden banner.</summary>
    public void UnhideBanner()
    {
        if (adsDisabled || bannerAd == null) return;

        bannerAd.ShowAd();
        IsBannerActive = true;
        Log("Banner shown.");
    }

    /// <summary>Completely destroy the banner. Call ShowBanner() to create a new one.</summary>
    public void DestroyBanner()
    {
        CleanupBanner();
        IsBannerActive = false;
        Log("Banner destroyed.");
    }

    /// <summary>Pause/resume banner auto-refresh (useful during gameplay).</summary>
    public void SetBannerRefresh(bool enabled)
    {
        if (bannerAd == null) return;

        if (enabled)
            bannerAd.ResumeAutoRefresh();
        else
            bannerAd.PauseAutoRefresh();

        Log($"Banner refresh {(enabled ? "resumed" : "paused")}.");
    }

    // --- Banner Callbacks ---

    private void Banner_OnLoaded(LevelPlayAdInfo info)
    {
        IsBannerActive = true;
        Log($"Banner loaded — network: {info.AdNetwork}");
        OnBannerVisible?.Invoke();
    }

    private void Banner_OnLoadFailed(LevelPlayAdError error)
    {
        IsBannerActive = false;
        LogWarning($"Banner load failed: {error.ErrorMessage}");
        OnBannerFailed?.Invoke();
    }

    private void Banner_OnClicked(LevelPlayAdInfo info)
    {
        Log("Banner clicked.");
    }

    private void CleanupBanner()
    {
        if (bannerAd == null) return;

        bannerAd.OnAdLoaded -= Banner_OnLoaded;
        bannerAd.OnAdLoadFailed -= Banner_OnLoadFailed;
        bannerAd.OnAdClicked -= Banner_OnClicked;

        bannerAd.DestroyAd();
        bannerAd = null;
    }

    // ========================================================================
    // UTILITY — Retry Logic, Pause, Logging
    // ========================================================================

    /// <summary>
    /// Exponential backoff: wait longer after each consecutive failure.
    /// Resets to base delay after a successful load.
    /// Pattern: 5s → 10s → 20s → 40s → ... → max 120s
    /// </summary>
    private void ScheduleRetry(ref int retryCount, string methodName)
    {
        float delay = Mathf.Min(
            retryBaseDelay * Mathf.Pow(2, retryCount),
            retryMaxDelay
        );

        retryCount++;
        Log($"Retry #{retryCount} for {methodName} in {delay:F0}s");
        Invoke(methodName, delay);
    }

    /// <summary>Pause/resume game during full-screen ads.</summary>
    private void PauseGame(bool pause)
    {
        AudioListener.pause = pause;
        // Time.timeScale is NOT changed here — modifying it can break
        // animations, physics, and timers across your entire game.
        // If you need to pause gameplay, do it through your own game
        // manager (e.g., GameManager.Instance.SetPaused(pause)).
    }

    // --- Logging helpers (only active when enableDebugLogs is true) ---

    private void Log(string message)
    {
        if (enableDebugLogs) Debug.Log($"[AdManager] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs) Debug.LogWarning($"[AdManager] {message}");
    }

    private void LogError(string message)
    {
        // Errors always show, regardless of debug setting
        Debug.LogError($"[AdManager] {message}");
    }
}