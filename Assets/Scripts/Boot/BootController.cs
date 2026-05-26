using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay.Managers;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BootController : MonoBehaviour
{
    // ─── Inspector References ───

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private UpdatePanelController updatePanel;

    [Header("Auth Buttons")]
    [Tooltip("All start non-interactable; enabled only after auto sign-in fails.")]
    [SerializeField] private Button guestButton;
    [SerializeField] private Button googleBtn;
    [SerializeField] private Button appleBtn;

    private const string LastLaunchedVersionKey = "LastLaunchedAppVersion";

    [Header("Ad Manager (optional)")]
    [Tooltip("Assign if AdManager lives in this scene. Leave empty if handled elsewhere.")]
    [SerializeField] private AdManager adManager;

    // ─── Services ───

    private AuthService authService;
    private CloudDatabase cloudDatabase;
    private SceneLoader sceneLoader;

    private CancellationTokenSource timeoutCts;

    // ─── Lifecycle ───

    private void Awake()
    {
        timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfterSlim(TimeSpan.FromSeconds(90));

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token,
                this.GetCancellationTokenOnDestroy(),
                AppLifetime.Token);

        DetectVersionChange();
        InitializeServices();
        SubscribeToEvents();

        SetButtonsInteractable(false);
        HidePlatformIrrelevantButton();
    }

    // Hide the platform-irrelevant login button at startup — the object itself
    // is disabled so it takes no space and can't be clicked even by accident.
    private void HidePlatformIrrelevantButton()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (appleBtn  != null) appleBtn.gameObject.SetActive(false);
#elif UNITY_IOS && !UNITY_EDITOR
        if (googleBtn != null) googleBtn.gameObject.SetActive(false);
#endif
    }

    private void DetectVersionChange()
    {
        string previous = PlayerPrefs.GetString(LastLaunchedVersionKey, "");
        string current  = Application.version;
        if (!string.IsNullOrEmpty(previous) && previous != current)
            GameLog.Log($"[BootController] App updated: {previous} -> {current}.");
        PlayerPrefs.SetString(LastLaunchedVersionKey, current);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        RunBootSequenceAsync(timeoutCts.Token).Forget();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        timeoutCts?.Cancel();
        timeoutCts?.Dispose();
    }

    // ─── Service Registration ───

    private void InitializeServices()
    {
        authService   = new AuthService();
        cloudDatabase = new CloudDatabase();
        sceneLoader   = new SceneLoader();

        ServiceLocator.Register<AuthService>(authService);
        ServiceLocator.Register<ICloudSaveManager>(CloudSaveManager.Instance);
        ServiceLocator.Register<CloudDatabase>(cloudDatabase);
        ServiceLocator.Register<SceneLoader>(sceneLoader);
        ServiceLocator.Register<CurrencyManager>(CurrencyManager.Instance);

        var chapterUnlockService = new ChapterUnlockService();
        ServiceLocator.Register<ChapterUnlockService>(chapterUnlockService);
        var userDataRepo = new UserDataRepository();
        ServiceLocator.Register<UserDataRepository>(userDataRepo);

        PushNotificationService.Initialize();
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

    private void OnAuthProgress(AuthProgressEvent evt)         => SetStatus(evt.currentStep);
    private void OnDataLoadProgress(DataLoadProgressEvent evt) => SetStatus(evt.currentStep);

    // ─── Boot Sequence ───

    private async UniTaskVoid RunBootSequenceAsync(CancellationToken ct)
    {
        try
        {
            SetStatus("Starting up...");
            bool signedIn = await authService.SignInAsync(ct);

            if (!signedIn) { OnAuthFailed(); return; }

            if (!await CheckRemoteGatesAsync(ct)) return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAsync(ct)) return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException)
        {
            GameLog.Log("[BootController] Boot sequence cancelled.");
            if (this != null)
            {
                SetStatus("Connection timed out. Please retry.");
                SetButtonsInteractable(true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Boot failed: {ex.Message}\n{ex.StackTrace}");
            SetStatus("Something went wrong. Please retry.");
            SetButtonsInteractable(true);
        }
    }

    // ─── Ad SDK Init (Parallel) ───

    private void InitializeAdsInParallel()
    {
        if (adManager == null) return;
        if (authService.IsSignedIn)
            adManager.SetUserId(authService.PlayerId);
        if (!adManager.HasConsentResolved)
            adManager.SetUserConsent(gdprConsent: true);
    }

    // ─── Failure Handler ───

    private void OnAuthFailed()
    {
        SetStatus("Sign-in failed. Choose how to continue.");
        SetButtonsInteractable(true);
    }

    // ─── UI Button Handlers ───

    public void OnGuestButtonClicked()
    {
        SetButtonsInteractable(false);
        ResetTimeoutCts();
        ContinueWithAnonymousAsync(timeoutCts.Token).Forget();
    }

    public void OnGoogleLoginClicked()
    {
        SetButtonsInteractable(false);
        SetStatus("Signing in with Google...");
        ResetTimeoutCts();
        RetryAutoSignInAsync(timeoutCts.Token).Forget();
    }

    public void OnAppleLoginClicked()
    {
        SetButtonsInteractable(false);
        SetStatus("Signing in with Game Center...");
        ResetTimeoutCts();
        RetryAutoSignInAsync(timeoutCts.Token).Forget();
    }

    private void ResetTimeoutCts()
    {
        timeoutCts?.Cancel();
        timeoutCts?.Dispose();
        timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfterSlim(TimeSpan.FromSeconds(90));
    }

    private async UniTaskVoid RetryAutoSignInAsync(CancellationToken ct)
    {
        try
        {
            bool signedIn = await authService.SignInAsync(ct);
            if (!signedIn) { OnAuthFailed(); return; }

            if (!await CheckRemoteGatesAsync(ct)) return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAsync(ct)) return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Retry sign-in failed: {ex.Message}");
            SetStatus("Sign-in failed. Try another option.");
            SetButtonsInteractable(true);
        }
    }

    private async UniTask<bool> CheckRemoteGatesAsync(CancellationToken ct)
    {
        SetStatus("Fetching config...");
        await RemoteConfigManager.Instance.FetchConfig();

        var rc = RemoteConfigManager.Instance;
        if (rc.MaintenanceMode) { SetStatus(rc.MaintenanceMessage); return false; }

        if (rc.IsUpdateAvailable())
            if (!await HandleUpdatePromptAsync(ct)) return false;

        return true;
    }

    private async UniTask<bool> LoadGameDataAsync(CancellationToken ct)
    {
        await cloudDatabase.InitializeAsync(ct);

        var chapterService = ServiceLocator.Get<ChapterUnlockService>();
        chapterService.Initialize(cloudDatabase.ChapterUnlockStatusData, 10, new[] { 0 });

        var userDataRepo = ServiceLocator.Get<UserDataRepository>();
        userDataRepo.Initialize(cloudDatabase, CloudSaveManager.Instance);

        TryRegisterPushNotifications(ct);

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

        SetStatus("Loading progress...");
        await GameProgressManager.Instance.LoadProgress();
        return true;
    }

    private void TryRegisterPushNotifications(CancellationToken ct)
    {
        if (!RemoteConfigManager.Instance.PushNotificationsEnabled) return;
        if (PushNotificationService.Instance == null) return;
        PushNotificationService.Instance.RegisterIfEnabledAsync(ct).Forget();
    }

    private async UniTask<bool> HandleUpdatePromptAsync(CancellationToken ct)
    {
        var rc       = RemoteConfigManager.Instance;
        bool isForce = rc.IsForceUpdate();
        string url   = rc.GetPlatformUpdateURL();
        string msg   = rc.UpdatePromptMessage;

        if (updatePanel == null) { SetStatus(msg); return !isForce; }

        updatePanel.Configure(isForce, url, msg);
        if (isForce) return false;

        await updatePanel.AwaitDismissAsync(ct);
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
                SetButtonsInteractable(true);
                return;
            }

            if (!await CheckRemoteGatesAsync(ct)) return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAsync(ct)) return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Anonymous flow failed: {ex.Message}");
            SetStatus("Something went wrong. Please retry.");
            SetButtonsInteractable(true);
        }
    }

    // ─── UI Helpers ───

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // Locks or unlocks all three buttons. HidePlatformIrrelevantButton() already
    // disabled the wrong-platform button's GameObject, so interactable changes
    // on it are harmless but we guard anyway for clarity.
    private void SetButtonsInteractable(bool interactable)
    {
        if (guestButton != null) guestButton.interactable = interactable;
        if (googleBtn   != null) googleBtn.interactable   = interactable;
        if (appleBtn    != null) appleBtn.interactable     = interactable;
    }
}