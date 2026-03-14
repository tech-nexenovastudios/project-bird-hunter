using System;
using Constants;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Services.CloudSave;
using Unity.Services.RemoteConfig;

//#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class AuthBootstrapper : MonoBehaviour
{
    public static AuthBootstrapper Instance { get; private set; }
    
    [Header("Config")]
    public string environmentId = "development"; 

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject anonymousLoginButton;

    public bool IsLoggedIn => AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => AuthenticationService.Instance.PlayerId;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
#endif

        HideAllButtons();
    }

    private void OnEnable()
    {
        PlayerDataLoader.OnDataLoaded += OnDataLoaded;
        PlayerDataLoader.OnNewPlayerReady += OnNewPlayerJoined;
        PlayerDataLoader.OnReturningPlayerReady += OnReturningPlayer;
    }

    private void OnReturningPlayer()
    {
        
    }

    private void OnNewPlayerJoined()
    {
        
    }

    private void OnDataLoaded(PlayerDataLoader.LoadResult obj)
    {
        
    }

    private void Start()
    {
        BootAsync().Forget();
    }

    // ==================== Boot Sequence ====================

    private async UniTaskVoid BootAsync()
    {
        HideAllButtons();

        // Step 1: Initialize Unity Services
        if (!await InitializeServices())
            return;

        // Already signed in
        if (IsLoggedIn)
        {
            Debug.Log("[Auth] Already signed in.");
            OnSignInSuccess().Forget();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: Try GPGS first
        bool signedIn = await TryGPGSLogin();

        if (signedIn)
        {
            OnSignInSuccess().Forget();
        }
        else
        {
            // GPGS failed — show Retry and Anonymous buttons
            SetStatus("Google Play sign-in failed.");
            ShowFallbackButtons();
        }
#else
        // Editor / Non-Android: Go straight to anonymous
        bool signedIn = await TryAnonymousLogin();

        if (signedIn)
            OnSignInSuccess().Forget();
        else
            OnSignInFailed();
#endif
    }

    // ==================== Initialize ====================

    private async UniTask<bool> InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Uninitialized)
            return true;

        try
        {
            SetStatus("Initializing...");
            await UnityServices.InitializeAsync();
            Debug.Log("[Auth] Unity Services initialized.");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Auth] Unity Services init failed: {ex.Message}");
            SetStatus("Initialization failed.");
            ShowRetryOnly();
            return false;
        }
    }

    // ==================== GPGS Login ====================

    private async UniTask<bool> TryGPGSLogin()
    {
//#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_ANDROID
        try
        {
            SetStatus("Signing in with Google Play...");

            var authTcs = new UniTaskCompletionSource<SignInStatus>();

            PlayGamesPlatform.Instance.Authenticate((status) =>
            {
                authTcs.TrySetResult(status);
            });

            var authStatus = await authTcs.Task;

            if (authStatus != SignInStatus.Success)
            {
                Debug.LogWarning($"[Auth] GPGS auth failed: {authStatus}");
                return false;
            }

            Debug.Log($"[Auth] GPGS authenticated: {PlayGamesPlatform.Instance.GetUserId()}");

            var codeTcs = new UniTaskCompletionSource<string>();

            PlayGamesPlatform.Instance.RequestServerSideAccess(true, (code) =>
            {
                codeTcs.TrySetResult(code);
            });

            

            string authCode = await codeTcs.Task;

            if (string.IsNullOrEmpty(authCode))
            {
                Debug.LogError("[Auth] GPGS auth code was empty.");
                return false;
            }

            SetStatus("Connecting to server...");
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);

            Debug.Log($"[Auth] GPGS sign-in complete. Player ID: {PlayerId}");
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[Auth] GPGS Unity Auth failed: {ex.ErrorCode} - {ex.Message}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Auth] GPGS error: {ex.Message}");
            return false;
        }
#else
        await UniTask.CompletedTask;
        return false;
#endif
    }

    // ==================== Anonymous Login ====================

    private async UniTask<bool> TryAnonymousLogin()
    {
        try
        {
            SetStatus("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Auth] Anonymous sign-in successful. Player ID: {PlayerId}");
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[Auth] Anonymous auth failed: {ex.ErrorCode} - {ex.Message}");
            return false;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[Auth] Request failed: {ex.ErrorCode} - {ex.Message}");
            return false;
        }
    }

    // ==================== Starter Currency ====================

    private const string STARTER_GRANTED_KEY = "starter_currency_granted";

    private const int STARTER_GOLD = 1000000;
    private const int STARTER_GEMS = 10000;
    private const int STARTER_POWER = 5;

    // ==================== Result Handlers ====================

    private async UniTaskVoid OnSignInSuccess()
    {
        HideAllButtons();
        SetStatus("Loading player data...");

        try
        {
            if (!string.IsNullOrEmpty(environmentId))
            {
                RemoteConfigService.Instance.SetEnvironmentID(environmentId);
            }

            await PlayerDataLoader.Instance.LoadAllPlayerDataAsync();
            
            
            bool alreadyGranted = await CloudSaveManager.Instance.LoadValueAsync<bool>(STARTER_GRANTED_KEY, false);

            if (!alreadyGranted)
            {
                SetStatus("Setting up your account...");

                await CurrencyManager.Instance.LoadBalances();
                await CurrencyManager.Instance.AddGold(STARTER_GOLD);
                await CurrencyManager.Instance.AddGems(STARTER_GEMS);
                await CurrencyManager.Instance.AddPower(STARTER_POWER);

                await CloudSaveManager.Instance.SaveValueAsync(STARTER_GRANTED_KEY, true);

                Debug.Log($"[Auth] Starter currency granted — Gold: {STARTER_GOLD}, Gems: {STARTER_GEMS}, Power: {STARTER_POWER}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Auth] Starter currency error: {ex.Message}");
        }

        SetStatus("Signed in! Loading...");
        Debug.Log($"[Auth] Loading {mainMenuSceneName}. Player ID: {PlayerId}");
        //IAnalyticsService.TrackNewUserFirstLogin(PlayerId);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnSignInFailed()
    {
        SetStatus("Sign-in failed. Check your connection.");
        Debug.LogError("[Auth] All sign-in methods failed.");
        ShowRetryOnly();
    }

    // ==================== Button Visibility ====================

    private void HideAllButtons()
    {
        if (retryButton != null) retryButton.SetActive(false);
        if (anonymousLoginButton != null) anonymousLoginButton.SetActive(false);
    }

    private void ShowFallbackButtons()
    {
        if (retryButton != null) retryButton.SetActive(true);
        if (anonymousLoginButton != null) anonymousLoginButton.SetActive(true);
    }

    private void ShowRetryOnly()
    {
        if (retryButton != null) retryButton.SetActive(true);
        if (anonymousLoginButton != null) anonymousLoginButton.SetActive(false);
    }

    // ==================== UI Helpers ====================

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ==================== Public API (Wire to UI Buttons) ====================

    /// <summary>Wire to Retry button. Restarts GPGS login attempt.</summary>
    public void RetrySignIn()
    {
        BootAsync().Forget();
    }

    /// <summary>Wire to Anonymous Login button. Signs in without Google Play.</summary>
    public void LoginAnonymously()
    {
        LoginAnonymouslyAsync().Forget();
    }

    private async UniTaskVoid LoginAnonymouslyAsync()
    {
        HideAllButtons();

        bool signedIn = await TryAnonymousLogin();

        if (signedIn)
            OnSignInSuccess().Forget();
        else
            OnSignInFailed();
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        Debug.Log("[Auth] Signed out.");
    }
}