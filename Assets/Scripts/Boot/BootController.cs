using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class BootController : MonoBehaviour
{
    // ─── Inspector References ───

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject guestButton;
    [SerializeField] private UpdatePanelController updatePanel;

    [Header("Login UI (shown on auto sign-in failure)")]
    [Tooltip("Container of UpperPanel + LowerPanel. Hidden by default; shown when auto GPGS sign-in fails.")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject upperPanel;
    [SerializeField] private GameObject lowerPanel;

    private const string LastLaunchedVersionKey = "LastLaunchedAppVersion";

    [Header("Ad Manager (optional)")]
    [Tooltip("Assign if AdManager lives in this scene. Leave empty if handled elsewhere.")]
    [SerializeField] private AdManager adManager;

    [Header("Behavior")]
    [SerializeField] private bool autoGrantConsentInEditor = true;

    // ─── Services ───

    private AuthService authService;
    private CloudDatabase cloudDatabase;
    private SceneLoader sceneLoader;

    private CancellationTokenSource cts;

    // ─── Lifecycle ───

    private void Awake()
    {
        cts = new CancellationTokenSource();
        DetectVersionChange();
        InitializeServices();
        SubscribeToEvents();
        HideAllButtons();
    }

    // App update detection: PlayerPrefs survive reinstalls/updates but the
    // app process is killed by the OS, so this Awake always fires fresh from
    // BootStrapper (scene 0) on the first launch after an update.
    private void DetectVersionChange()
    {
        string previous = PlayerPrefs.GetString(LastLaunchedVersionKey, "");
        string current = Application.version;
        if (!string.IsNullOrEmpty(previous) && previous != current)
        {
            Debug.Log($"[BootController] App updated: {previous} -> {current}. Clear any stale state here if needed.");
            // Hook for future cleanup of version-sensitive cached state.
        }
        PlayerPrefs.SetString(LastLaunchedVersionKey, current);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        RunBootSequenceAsync(cts.Token).Forget();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        cts?.Cancel();
        cts?.Dispose();
    }

    // ─── Service Registration ───

    private void InitializeServices()
    {
        authService = new AuthService();
        cloudDatabase = new CloudDatabase();
        sceneLoader = new SceneLoader();

        ServiceLocator.Register<AuthService>(authService);
        ServiceLocator.Register<ICloudSaveManager>(CloudSaveManager.Instance);
        ServiceLocator.Register<CloudDatabase>(cloudDatabase);
        ServiceLocator.Register<SceneLoader>(sceneLoader);
        ServiceLocator.Register<CurrencyManager>(CurrencyManager.Instance);

        var chapterUnlockService = new ChapterUnlockService();
        ServiceLocator.Register<ChapterUnlockService>(chapterUnlockService);
        var userDataRepo = new UserDataRepository();
        ServiceLocator.Register<UserDataRepository>(userDataRepo);
    }

    // ─── Event Subscriptions ───

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<AuthProgressEvent>(OnAuthProgress);
        EventBus.Subscribe<DataLoadProgressEvent>(OnDataLoadProgress);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<AuthProgressEvent>(OnAuthProgress);
        EventBus.Unsubscribe<DataLoadProgressEvent>(OnDataLoadProgress);
    }

    private void OnAuthProgress(AuthProgressEvent evt) => SetStatus(evt.currentStep);
    private void OnDataLoadProgress(DataLoadProgressEvent evt) => SetStatus(evt.currentStep);

    // ─── Boot Sequence ───

    private async UniTaskVoid RunBootSequenceAsync(CancellationToken ct)
    {
        try
        {
            HideAllButtons();

            SetStatus("Starting up...");
            bool signedIn = await authService.SignInAsync(ct);

            if (!signedIn)
            {
                OnAuthFailed();
                return;
            }

            if (!await CheckRemoteGatesAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[BootController] Boot sequence cancelled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Boot failed: {ex.Message}\n{ex.StackTrace}");
            SetStatus("Something went wrong. Please retry.");
            ShowLoginPanels();
        }
    }

    // ─── Ad SDK Init (Parallel) ───

    private void InitializeAdsInParallel()
    {
        if (adManager == null) return;

#if UNITY_EDITOR
        if (autoGrantConsentInEditor && !adManager.HasConsentResolved)
            adManager.SetUserConsent(gdprConsent: true);
#endif
    }

    // ─── Failure Handlers ───

    private void OnAuthFailed()
    {
        SetStatus("Sign-in failed. Choose how to continue.");
        ShowLoginPanels();
    }

    // ─── UI Button Handlers ───

    public void OnGuestButtonClicked()
    {
        HideAllButtons();
        ContinueWithAnonymousAsync(cts.Token).Forget();
    }

    // Wire on the Google login button. Re-runs the full auto sign-in flow,
    // which on Android attempts GPGS again.
    public void OnGoogleLoginClicked()
    {
        HideAllButtons();
        SetStatus("Signing in with Google...");
        RetryAutoSignInAsync(cts.Token).Forget();
    }

    // Wire on the Apple login button. AuthService does not yet implement
    // Apple sign-in — log a warning and re-show the panels so the user can
    // pick another option. Replace with the real flow once available.
    public void OnAppleLoginClicked()
    {
        Debug.LogWarning("[BootController] Apple sign-in not implemented yet.");
        SetStatus("Apple sign-in is not available yet.");
        ShowLoginPanels();
    }

    private async UniTaskVoid RetryAutoSignInAsync(CancellationToken ct)
    {
        try
        {
            bool signedIn = await authService.SignInAsync(ct);
            if (!signedIn)
            {
                OnAuthFailed();
                return;
            }

            if (!await CheckRemoteGatesAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Retry sign-in failed: {ex.Message}");
            SetStatus("Sign-in failed. Try another option.");
            ShowLoginPanels();
        }
    }

    // Runs on BootStrapper before the loading scene loads, so the maintenance
    // text and update panel surface immediately on the boot UI.
    private async UniTask<bool> CheckRemoteGatesAsync(CancellationToken ct)
    {
        Debug.Log("[Boot] CheckRemoteGatesAsync: fetching config");
        SetStatus("Fetching config...");
        await RemoteConfigManager.Instance.FetchConfig();

        var rc = RemoteConfigManager.Instance;
        Debug.Log($"[Boot] Config fetched. appVer={Application.version} latest={rc.LatestVersion} min={rc.MinRequiredVersion} force={rc.ForceUpdate} maintenance={rc.MaintenanceMode}");

        if (rc.MaintenanceMode)
        {
            Debug.Log("[Boot] Maintenance mode active — halting boot");
            SetStatus(rc.MaintenanceMessage);
            return false;
        }

        bool updateAvailable = rc.IsUpdateAvailable();
        Debug.Log($"[Boot] IsUpdateAvailable={updateAvailable}");
        if (updateAvailable)
        {
            bool shouldContinue = await HandleUpdatePromptAsync(ct);
            Debug.Log($"[Boot] HandleUpdatePromptAsync returned {shouldContinue}");
            if (!shouldContinue)
                return false;
        }

        return true;
    }

    private async UniTask<bool> LoadGameDataAsync(CancellationToken ct)
    {
        await cloudDatabase.InitializeAsync(ct);

        var chapterService = ServiceLocator.Get<ChapterUnlockService>();
        int totalChapterCount = 10;
        int[] defaultUnlocked = new[] { 0 };
        chapterService.Initialize(cloudDatabase.ChapterUnlockStatusData, totalChapterCount, defaultUnlocked);

        var userDataRepo = ServiceLocator.Get<UserDataRepository>();
        userDataRepo.Initialize(cloudDatabase, CloudSaveManager.Instance);

        SetStatus("Loading currencies...");
        try
        {
            await CurrencyManager.Instance.LoadBalances(forceReload: true);
        }
        catch
        {
            await UniTask.Delay(2000, cancellationToken: ct);
            await CurrencyManager.Instance.LoadBalances(forceReload: true);
        }

        return true;
    }

    // Returns true if the boot sequence should continue (user dismissed an
    // optional update). Returns false if the update is mandatory and boot
    // must halt — the panel stays visible until the user updates and relaunches.
    private async UniTask<bool> HandleUpdatePromptAsync(CancellationToken ct)
    {
        var rc = RemoteConfigManager.Instance;
        bool isForce = rc.IsForceUpdate();
        string url = rc.GetPlatformUpdateURL();
        string message = rc.UpdatePromptMessage;

        Debug.Log($"[Boot] HandleUpdatePromptAsync isForce={isForce} url='{url}' panelWired={(updatePanel != null)}");

        if (updatePanel == null)
        {
            // No panel wired — fail safe: block on force update, allow otherwise.
            Debug.LogWarning("[Boot] updatePanel reference is null — falling back to status text only");
            SetStatus(message);
            return !isForce;
        }

        updatePanel.Configure(isForce, url, message);

        if (isForce)
        {
            Debug.Log("[Boot] Force update — halting boot, panel stays visible");
            return false;
        }

        Debug.Log("[Boot] Awaiting Not Now click...");
        await updatePanel.AwaitDismissAsync(ct);
        Debug.Log("[Boot] Update panel dismissed by user");
        return true;
    }

    private async UniTaskVoid ContinueWithAnonymousAsync(CancellationToken ct)
    {
        try
        {
            SetStatus("Signing in as guest...");
            bool signedIn = await authService.SignInAnonymouslyAsync(ct);

            if (!signedIn)
            {
                SetStatus("Sign-in failed. Check your connection.");
                ShowLoginPanels();
                return;
            }

            if (!await CheckRemoteGatesAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Anonymous flow failed: {ex.Message}");
            SetStatus("Something went wrong. Please retry.");
            ShowLoginPanels();
        }
    }

    // ─── UI Helpers ───

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void HideAllButtons()
    {
        if (guestButton != null) guestButton.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (upperPanel != null) upperPanel.SetActive(false);
        if (lowerPanel != null) lowerPanel.SetActive(false);
    }

    private void ShowLoginPanels()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (upperPanel != null) upperPanel.SetActive(true);
        if (lowerPanel != null) lowerPanel.SetActive(true);
        if (guestButton != null) guestButton.SetActive(true);
    }
}