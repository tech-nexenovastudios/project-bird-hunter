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

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAndCheckGatesAsync(ct))
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
            ShowGuestButton();
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
        SetStatus("Sign-in failed. Continue as guest?");
        ShowGuestButton();
    }

    // ─── UI Button Handlers ───

    public void OnGuestButtonClicked()
    {
        HideAllButtons();
        ContinueWithAnonymousAsync(cts.Token).Forget();
    }

    private async UniTask<bool> LoadGameDataAndCheckGatesAsync(CancellationToken ct)
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

        SetStatus("Fetching config...");
        await RemoteConfigManager.Instance.FetchConfig();

        if (RemoteConfigManager.Instance.MaintenanceMode)
        {
            SetStatus(RemoteConfigManager.Instance.MaintenanceMessage);
            return false;
        }

        if (RemoteConfigManager.Instance.NeedsForceUpdate())
        {
            SetStatus(RemoteConfigManager.Instance.UpdatePromptMessage);
            return false;
        }

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
                ShowGuestButton();
                return;
            }

            await sceneLoader.LoadSceneAdditiveAsync(SceneNames.LOADING, setActive: false, ct: ct);
            InitializeAdsInParallel();

            if (!await LoadGameDataAndCheckGatesAsync(ct))
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
            ShowGuestButton();
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
    }

    private void ShowGuestButton()
    {
        if (guestButton != null) guestButton.SetActive(true);
    }
}