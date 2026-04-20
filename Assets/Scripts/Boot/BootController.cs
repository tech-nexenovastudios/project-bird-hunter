using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BootController : MonoBehaviour
{
    // ─── Inspector References ───

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    //[SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject anonymousLoginButton;

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
        InitializeServices();
        SubscribeToEvents();
        HideAllButtons();
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
        // In InitializeServices(), add:
        var chapterUnlockService = new ChapterUnlockService();
        ServiceLocator.Register<ChapterUnlockService>(chapterUnlockService);
        var userDataRepo = new UserDataRepository();
        ServiceLocator.Register<UserDataRepository>(userDataRepo);

        var powerupUnlockService = new PowerupUnlockService();
        ServiceLocator.Register<PowerupUnlockService>(powerupUnlockService);


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

            // Step 1: Authenticate
            SetStatus("Starting up...");
            bool signedIn = await authService.SignInAsync(ct);

            if (!signedIn)
            {
                OnAuthFailed();
                return;
            }

            // Step 2: Load LoadingScene additively (progress bar visible from here on)
            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);

            // Step 3: Start ad SDK init in parallel (don't await — it runs alongside data fetch)
            InitializeAdsInParallel();

            if (!await LoadGameDataAndCheckGatesAsync(ct))
                return;
            // Step 5: Transition to MainMenu, unload Bootstrapper + LoadingScene
            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);

            // Bootstrapper unloads itself last — use a detached token so our own
            // OnDestroy cancellation doesn't abort the unload mid-flight.
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
            ShowRetryOnly();
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
        // AdManager's own Start() handles init. Nothing to await here.
    }

    // ─── Failure Handlers ───

    private void OnAuthFailed()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SetStatus("Google Play sign-in failed.");
        ShowFallbackButtons();
#else
        SetStatus("Sign-in failed. Check your connection.");
        ShowRetryOnly();
#endif
    }

    // ─── UI Button Handlers (wire in Inspector) ───

    public void OnRetryClicked()
    {
        HideAllButtons();
        RunBootSequenceAsync(cts.Token).Forget();
    }

    public void OnAnonymousLoginClicked()
    {
        HideAllButtons();
        ContinueWithAnonymousAsync(cts.Token).Forget();
    }
    // Add this helper method to BootController
    private async UniTask<bool> LoadGameDataAndCheckGatesAsync(CancellationToken ct)
    {
        await cloudDatabase.InitializeAsync(ct);
        // Initialize chapter unlock service with loaded data
        var chapterService = ServiceLocator.Get<ChapterUnlockService>();
        int totalChapterCount = 10; // Or pull from your ChaptersConfig via a shared access pattern
        int[] defaultUnlocked = new[] { 0 };
        chapterService.Initialize(
            cloudDatabase.ChapterUnlockStatusData,
            totalChapterCount,
            defaultUnlocked
        );
        // Initialize PowerupUnlockService with loaded data + database from Resources
        var powerupService = ServiceLocator.Get<PowerupUnlockService>();
        var powerupDatabase = Resources.Load<Gameplay.PowerUps.PowerupDatabase>("PowerupDatabase");

        if (powerupDatabase == null)
        {
            Debug.LogError("[BootController] PowerupDatabase not found in Resources folder.");
        }

        powerupService.Initialize(
            cloudDatabase.PowerupUnlockStatusData,
            powerupDatabase
        );
        // Initialize UserDataRepository with the loaded user data
        var userDataRepo = ServiceLocator.Get<UserDataRepository>();
        userDataRepo.Initialize(cloudDatabase, CloudSaveManager.Instance);
        //currency
        SetStatus("Loading currencies...");
        await CurrencyManager.Instance.LoadBalances();
        //remote-config
        SetStatus("Fetching config...");
        await RemoteConfigManager.Instance.FetchConfig();

        if (RemoteConfigManager.Instance.MaintenanceMode)
        {
            SetStatus(RemoteConfigManager.Instance.MaintenanceMessage);
            ShowRetryOnly();
            return false;
        }

        if (RemoteConfigManager.Instance.NeedsForceUpdate())
        {
            SetStatus(RemoteConfigManager.Instance.UpdatePromptMessage);
            ShowRetryOnly();
            return false;
        }

        return true;
    }
    private async UniTaskVoid ContinueWithAnonymousAsync(CancellationToken ct)
    {
        try
        {
            bool signedIn = await authService.SignInAnonymouslyAsync(ct);

            if (!signedIn)
            {
                SetStatus("Sign-in failed. Check your connection.");
                ShowRetryOnly();
                return;
            }

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();
            if (!await LoadGameDataAndCheckGatesAsync(ct))
                return;

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.MAIN_MENU, setActive: true, ct: ct);
            await sceneLoader.UnloadSceneAsync(SceneNames.LOADING, ct);

            // Bootstrapper unloads itself last — use a detached token so our own
            // OnDestroy cancellation doesn't abort the unload mid-flight.
            sceneLoader.UnloadSceneAsync(SceneNames.BOOTSTRAPPER, CancellationToken.None).Forget();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.LogError($"[BootController] Anonymous flow failed: {ex.Message}");
            SetStatus("Something went wrong. Please retry.");
            ShowRetryOnly();
        }
    }

    // ─── UI Helpers ───

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void HideAllButtons()
    {
        //if (retryButton != null) retryButton.SetActive(false);
        if (anonymousLoginButton != null) anonymousLoginButton.SetActive(false);
    }

    private void ShowFallbackButtons()
    {
        //if (retryButton != null) retryButton.SetActive(true);
        if (anonymousLoginButton != null) anonymousLoginButton.SetActive(true);
    }

    private void ShowRetryOnly()
    {
        //if (retryButton != null) retryButton.SetActive(true);
        if (anonymousLoginButton != null) anonymousLoginButton.SetActive(false);
    }
}