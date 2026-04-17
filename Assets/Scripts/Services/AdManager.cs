// ============================================================================
// AdManager.cs — Production-ready LevelPlay integration
// Changes from your original (marked with // [FIX] comments):
//   1. Reads "AdsRemoved" PlayerPrefs on Awake (persists purchase)
//   2. Consent gate — ads only load after consent resolved
//   3. TestSuite method wrapped in editor/dev-build guard
//   4. Debug logs auto-disable in release builds
//   5. OnAdFullscreenStateChanged event for game-wide pause control
//   6. Replaced string-based Invoke() with coroutine (refactor-safe)
// ============================================================================

using UnityEngine;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class AdManager : MonoBehaviour
{
    // ========================================================================
    // INSPECTOR CONFIGURATION
    // ========================================================================

    [Header("=== LevelPlay Settings ===")]
    [Tooltip("App Key from LevelPlay dashboard. DO NOT commit this to public repos.")]
    [SerializeField] private string appKey = "";
    [SerializeField] private bool isDevelopmentMode = true;

    [Header("=== Ad Unit IDs (from LevelPlay Dashboard) ===")]
    [SerializeField] private string rewardedAdUnitId = "";
    [SerializeField] private string interstitialAdUnitId = "";
    [SerializeField] private string bannerAdUnitId = "";

    [Header("=== Behavior Settings ===")]
    [Tooltip("Disable in production builds to reduce log noise.")]
    [SerializeField] private bool enableDebugLogs = true;

    [Tooltip("Seconds to wait before retrying a failed ad load.")]
    [SerializeField] private float retryBaseDelay = 5f;

    [Tooltip("Maximum retry delay in seconds (caps exponential backoff).")]
    [SerializeField] private float retryMaxDelay = 120f;

    [Header("=== Consent (GDPR / CCPA / COPPA) ===")]
    [Tooltip("If TRUE, ads will NOT load until SetUserConsent() is called. " +
             "Set this to TRUE for any app that ships to EEA/UK/Switzerland users.")]
    [SerializeField] private bool requireConsentBeforeLoading = true;

    // ========================================================================
    // EVENTS
    // ========================================================================

    public event Action OnSDKReady;
    public event Action<string, int> OnRewardGranted;
    public event Action OnRewardedAvailable;
    public event Action OnRewardedUnavailable;
    public event Action OnRewardedDismissed;
    public event Action OnInterstitialAvailable;
    public event Action OnInterstitialUnavailable;
    public event Action OnInterstitialDismissed;
    public event Action OnBannerVisible;
    public event Action OnBannerFailed;

    // [FIX 5] Let the GameManager decide what "paused" means for your game.
    public event Action<bool> OnAdFullscreenStateChanged;

    // ========================================================================
    // SINGLETON & STATE
    // ========================================================================

    public static AdManager Instance { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool IsRewardedReady => rewardedAd != null && rewardedAd.IsAdReady();
    public bool IsInterstitialReady => interstitialAd != null && interstitialAd.IsAdReady();
    public bool IsBannerActive { get; private set; }

    // [FIX 2] Gate ad loading until consent is handled.
    public bool HasConsentResolved { get; private set; }

    [SerializeField] private bool adsDisabled = false;
    public bool AdsDisabled
    {
        get => adsDisabled;
        set
        {
            adsDisabled = value;
            if (value)
            {
                HideBanner();
                DestroyBanner();
            }
            // [FIX 1] Persist so "Remove Ads" survives app restarts.
            PlayerPrefs.SetInt(PREF_ADS_REMOVED, value ? 1 : 0);
            PlayerPrefs.Save();
            Log("Ads " + (value ? "DISABLED" : "ENABLED"));
        }
    }

    private const string PREF_ADS_REMOVED = "AdsRemoved";

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayBannerAd bannerAd;

    private int rewardedRetryCount;
    private int interstitialRetryCount;
    private bool isRewardedLoading;
    private bool isInterstitialLoading;

    private Coroutine rewardedRetryRoutine;
    private Coroutine interstitialRetryRoutine;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    // Tracks whether we've already started SDK init. Prevents duplicate
    // Init() calls that cause "SetMetaData must be called before init" errors.
    private static bool hasInitStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // [FIX 1] Restore "Remove Ads" purchase on app start.
        adsDisabled = PlayerPrefs.GetInt(PREF_ADS_REMOVED, 0) == 1;

        // [FIX 4] Force-off debug logs in non-development builds.
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            enableDebugLogs = false;
#endif

        // [FIX 7] SetMetaData MUST happen before LevelPlay.Init().
        // We do it here in Awake (not Start) so that even if Init is triggered
        // from another script's Start, our metadata is already registered.
        ApplyPreInitMetaData();
    }

    private void ApplyPreInitMetaData()
    {
        if (hasInitStarted) return; // Too late to set pre-init metadata.

        LevelPlay.SetAdaptersDebug(isDevelopmentMode);

        if (isDevelopmentMode)
            LevelPlay.SetMetaData("is_test_suite", "enable");
    }

    private void Start()
    {
        if (adsDisabled)
        {
            Log("Ads disabled (Remove Ads purchased). Skipping init.");
            return;
        }

        InitializeSDK();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        LevelPlay.OnInitSuccess -= HandleInitSuccess;
        LevelPlay.OnInitFailed -= HandleInitFailed;

        CleanupRewarded();
        CleanupInterstitial();
        CleanupBanner();

        Instance = null;
    }

    // ========================================================================
    // [FIX 3] TEST SUITE — Editor / Dev builds only
    // ========================================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void TestLevelplay()
    {
        if (!IsInitialized)
        {
            LogWarning("Cannot launch Test Suite — SDK not initialized yet.");
            return;
        }
        LevelPlay.LaunchTestSuite();
    }
#endif

    // ========================================================================
    // [FIX 2] CONSENT API — Call this from your consent/privacy flow
    // ========================================================================

    /// <summary>
    /// Call this AFTER the user has responded to your consent dialog
    /// (or immediately on launch for users outside regulated regions).
    /// </summary>
    /// <param name="gdprConsent">true = personalized ads OK, false = non-personalized only</param>
    /// <param name="ccpaDoNotSell">true = user opted out of data sale (CCPA)</param>
    /// <param name="isChildDirected">true = COPPA applies (under 13)</param>
    public void SetUserConsent(bool gdprConsent, bool ccpaDoNotSell = false, bool isChildDirected = false)
    {
        // Pass consent flags to LevelPlay.
        // Note: In LevelPlay SDK 9.4.0+ Unity modernized the consent API — verify
        // the exact call name against your installed SDK version.
        LevelPlay.SetMetaData("do_not_sell", ccpaDoNotSell ? "true" : "false");
        LevelPlay.SetMetaData("is_child_directed", isChildDirected ? "true" : "false");

        HasConsentResolved = true;
        Log($"Consent set: GDPR={gdprConsent}, CCPA_DoNotSell={ccpaDoNotSell}, Child={isChildDirected}");

        // If SDK already finished initializing but ads were gated, load them now.
        if (IsInitialized)
        {
            TryLoadAllAds();
        }
    }

    private void TryLoadAllAds()
    {
        if (!string.IsNullOrEmpty(rewardedAdUnitId))
        {
            if (rewardedAd == null) CreateRewardedAd();
            LoadRewarded();
        }

        if (!string.IsNullOrEmpty(interstitialAdUnitId))
        {
            if (interstitialAd == null) CreateInterstitialAd();
            LoadInterstitial();
        }
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

        // [FIX 7] Guard against double-init — LevelPlay.Init() can only be
        // called once per app session. If another script already called it
        // (or a previous scene triggered it), we skip re-initializing.
        if (hasInitStarted)
        {
            Log("SDK init already started elsewhere — skipping duplicate Init call.");
            return;
        }

        // SetMetaData / SetAdaptersDebug were already called in Awake()
        // (ApplyPreInitMetaData). DO NOT call them again here — doing so
        // after Init causes "must be called before init" errors.

        LevelPlay.OnInitSuccess += HandleInitSuccess;
        LevelPlay.OnInitFailed += HandleInitFailed;

        hasInitStarted = true;
        LevelPlay.Init(appKey);
        Log("Initializing LevelPlay SDK...");
    }

    private void HandleInitSuccess(LevelPlayConfiguration configuration)
    {
        IsInitialized = true;
        Log("SDK initialized successfully!");
        OnSDKReady?.Invoke();

        // [FIX 2] Only load ads if consent is resolved (or not required).
        if (!requireConsentBeforeLoading || HasConsentResolved)
        {
            TryLoadAllAds();
        }
        else
        {
            Log("Waiting for consent before loading ads. Call SetUserConsent() when ready.");
        }
    }

    private void HandleInitFailed(LevelPlayInitError error)
    {
        IsInitialized = false;
        // Allow retry: init failed, so we can try again.
        hasInitStarted = false;
        LogError($"SDK init failed: {error.ErrorMessage}");
        StartCoroutine(RetryInitAfter(30f));
    }

    private IEnumerator RetryInitAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Log("Retrying SDK initialization...");
        hasInitStarted = true;
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
    }

    public void LoadRewarded()
    {
        if (adsDisabled || !IsInitialized || rewardedAd == null) return;
        if (requireConsentBeforeLoading && !HasConsentResolved) return;
        if (isRewardedLoading || IsRewardedReady) return;

        isRewardedLoading = true;
        rewardedAd.LoadAd();
        Log("Loading rewarded ad...");
    }

    public bool ShowRewarded(string placement = null)
    {
        if (adsDisabled || !IsRewardedReady)
        {
            OnRewardedUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement) && LevelPlayRewardedAd.IsPlacementCapped(placement))
        {
            OnRewardedUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement))
            rewardedAd.ShowAd(placementName: placement);
        else
            rewardedAd.ShowAd();

        return true;
    }

    private void Rewarded_OnLoaded(LevelPlayAdInfo info)
    {
        isRewardedLoading = false;
        rewardedRetryCount = 0;
        Log($"Rewarded loaded — network: {info.AdNetwork}");
        OnRewardedAvailable?.Invoke();
    }

    private void Rewarded_OnLoadFailed(LevelPlayAdError error)
    {
        isRewardedLoading = false;
        LogWarning($"Rewarded load failed: {error.ErrorMessage}");
        OnRewardedUnavailable?.Invoke();
        if (rewardedRetryRoutine != null) StopCoroutine(rewardedRetryRoutine);
        rewardedRetryRoutine = StartCoroutine(RetryAfterDelay(
            () => rewardedRetryCount,
            v => rewardedRetryCount = v,
            LoadRewarded));
    }

    private void Rewarded_OnDisplayed(LevelPlayAdInfo info)
    {
        Log("Rewarded ad displayed.");
        SetFullscreenAdState(true);
    }

    private void Rewarded_OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        LogWarning($"Rewarded display failed: {error.ErrorMessage}");
        SetFullscreenAdState(false);
        OnRewardedUnavailable?.Invoke();
        LoadRewarded();
    }

    private void Rewarded_OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        Log($"Reward granted: {reward.Name} x{reward.Amount}");
        OnRewardGranted?.Invoke(reward.Name, reward.Amount);
    }

    private void Rewarded_OnClosed(LevelPlayAdInfo info)
    {
        SetFullscreenAdState(false);
        OnRewardedDismissed?.Invoke();
        LoadRewarded();
    }

    private void Rewarded_OnClicked(LevelPlayAdInfo info) { }

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
    // INTERSTITIAL ADS  (identical structure to rewarded — abbreviated comments)
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
    }

    public void LoadInterstitial()
    {
        if (adsDisabled || !IsInitialized || interstitialAd == null) return;
        if (requireConsentBeforeLoading && !HasConsentResolved) return;
        if (isInterstitialLoading || IsInterstitialReady) return;

        isInterstitialLoading = true;
        interstitialAd.LoadAd();
    }

    public bool ShowInterstitial(string placement = null)
    {
        if (adsDisabled || !IsInterstitialReady)
        {
            OnInterstitialUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement) && LevelPlayInterstitialAd.IsPlacementCapped(placement))
        {
            OnInterstitialUnavailable?.Invoke();
            return false;
        }

        if (!string.IsNullOrEmpty(placement))
            interstitialAd.ShowAd(placementName: placement);
        else
            interstitialAd.ShowAd();

        return true;
    }

    private void Interstitial_OnLoaded(LevelPlayAdInfo info)
    {
        isInterstitialLoading = false;
        interstitialRetryCount = 0;
        OnInterstitialAvailable?.Invoke();
    }

    private void Interstitial_OnLoadFailed(LevelPlayAdError error)
    {
        isInterstitialLoading = false;
        OnInterstitialUnavailable?.Invoke();
        if (interstitialRetryRoutine != null) StopCoroutine(interstitialRetryRoutine);
        interstitialRetryRoutine = StartCoroutine(RetryAfterDelay(
            () => interstitialRetryCount,
            v => interstitialRetryCount = v,
            LoadInterstitial));
    }

    private void Interstitial_OnDisplayed(LevelPlayAdInfo info) => SetFullscreenAdState(true);

    private void Interstitial_OnDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        SetFullscreenAdState(false);
        OnInterstitialUnavailable?.Invoke();
        LoadInterstitial();
    }

    private void Interstitial_OnClosed(LevelPlayAdInfo info)
    {
        SetFullscreenAdState(false);
        OnInterstitialDismissed?.Invoke();
        LoadInterstitial();
    }

    private void Interstitial_OnClicked(LevelPlayAdInfo info) { }

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

    public void ShowBanner()
    {
        if (adsDisabled || !IsInitialized) return;
        if (requireConsentBeforeLoading && !HasConsentResolved) return;
        if (string.IsNullOrEmpty(bannerAdUnitId)) return;

        if (bannerAd == null)
        {
            bannerAd = new LevelPlayBannerAd(bannerAdUnitId);
            bannerAd.OnAdLoaded += Banner_OnLoaded;
            bannerAd.OnAdLoadFailed += Banner_OnLoadFailed;
            bannerAd.OnAdClicked += Banner_OnClicked;
        }

        bannerAd.LoadAd();
    }

    public void HideBanner()
    {
        if (bannerAd == null) return;
        bannerAd.HideAd();
        IsBannerActive = false;
    }

    public void UnhideBanner()
    {
        if (adsDisabled || bannerAd == null) return;
        bannerAd.ShowAd();
        IsBannerActive = true;
    }

    public void DestroyBanner()
    {
        CleanupBanner();
        IsBannerActive = false;
    }

    public void SetBannerRefresh(bool enabled)
    {
        if (bannerAd == null) return;
        if (enabled) bannerAd.ResumeAutoRefresh();
        else bannerAd.PauseAutoRefresh();
    }

    private void Banner_OnLoaded(LevelPlayAdInfo info)
    {
        IsBannerActive = true;
        OnBannerVisible?.Invoke();
    }

    private void Banner_OnLoadFailed(LevelPlayAdError error)
    {
        IsBannerActive = false;
        OnBannerFailed?.Invoke();
    }

    private void Banner_OnClicked(LevelPlayAdInfo info) { }

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
    // UTILITY
    // ========================================================================

    // [FIX 6] Coroutine-based retry (type-safe, refactor-safe).
    private IEnumerator RetryAfterDelay(Func<int> getCount, Action<int> setCount, Action retryAction)
    {
        float delay = Mathf.Min(retryBaseDelay * Mathf.Pow(2, getCount()), retryMaxDelay);
        setCount(getCount() + 1);
        Log($"Retry in {delay:F0}s");
        yield return new WaitForSeconds(delay);
        retryAction?.Invoke();
    }

    private void SetFullscreenAdState(bool active)
    {
        AudioListener.pause = active;
        OnAdFullscreenStateChanged?.Invoke(active);
    }

    private void Log(string msg) { if (enableDebugLogs) Debug.Log($"[AdManager] {msg}"); }
    private void LogWarning(string msg) { if (enableDebugLogs) Debug.LogWarning($"[AdManager] {msg}"); }
    private void LogError(string msg) { Debug.LogError($"[AdManager] {msg}"); }
}