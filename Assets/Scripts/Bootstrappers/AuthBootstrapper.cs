using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Threading;

//#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// Handles the full authentication boot sequence:
///   1. Initialize Unity Services
///   2. Attempt Google Play Games sign-in (Android only)
///   3. Fallback to anonymous sign-in
///   4. Grant starter currency on first login
///   5. Load main menu scene
///
/// DISPLAY HINT:
///   After successful GPGS auth, <see cref="AuthDisplayHint"/> is populated
///   with the player's Google display name (or email).
///   <see cref="UserProfileDataManager"/> reads this to generate the initial username.
/// </summary>
public class AuthBootstrapper : MonoBehaviour
{
    public static AuthBootstrapper Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject anonymousLoginButton;

    // ───────────────────────── Public Read-Only State ─────────────────────────

    public bool IsLoggedIn => AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => AuthenticationService.Instance.PlayerId;

    /// <summary>
    /// Display name or email retrieved from the auth provider (e.g. Google Play Games).
    /// Null for anonymous logins.  Read by UserProfileDataManager.
    /// </summary>
    public string AuthDisplayHint { get; private set; }

    // ───────────────────────── Internals ─────────────────────────

    private CancellationTokenSource cts;

    // ═══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Debug.unityLogger.logEnabled = false;

        cts = new CancellationTokenSource();

#if UNITY_ANDROID && !UNITY_EDITOR
        //PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
#endif

        HideAllButtons();
    }

    private void Start()
    {
        BootAsync(cts.Token).Forget();
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════
    //  BOOT SEQUENCE
    // ═══════════════════════════════════════════════════════════════

    private async UniTaskVoid BootAsync(CancellationToken token)
    {
        HideAllButtons();

        // Step 1 — Initialize Unity Gaming Services
        if (!await InitializeServices())
            return;

        // Already signed in from a previous session
        if (IsLoggedIn)
        {
            Debug.Log("[Auth] Already signed in.");
            OnSignInSuccess(token).Forget();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: try Google Play Games first
        bool signedIn = await TryGPGSLogin(token);

        if (signedIn)
        {
            OnSignInSuccess(token).Forget();
        }
        else
        {
            SetStatus("Google Play sign-in failed.");
            ShowFallbackButtons();
        }
#else
        // Editor / Non-Android: straight to anonymous
        bool signedIn = await TryAnonymousLogin();

        if (signedIn)
            OnSignInSuccess(token).Forget();
        else
            OnSignInFailed();
#endif
    }

    // ═══════════════════════════════════════════════════════════════
    //  INITIALIZE UNITY SERVICES
    // ═══════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════
    //  GOOGLE PLAY GAMES LOGIN
    // ═══════════════════════════════════════════════════════════════

    private async UniTask<bool> TryGPGSLogin(CancellationToken token)
    {
        //#if UNITY_ANDROID && !UNITY_EDITOR
#if UNITY_ANDROID
        try
        {
            SetStatus("Signing in with Google Play...");

            // ── Authenticate with GPGS ──
            var authTcs = new UniTaskCompletionSource<SignInStatus>();
            PlayGamesPlatform.Instance.Authenticate(status => authTcs.TrySetResult(status));
            var authStatus = await authTcs.Task;

            if (authStatus != SignInStatus.Success)
            {
                Debug.LogWarning($"[Auth] GPGS auth failed: {authStatus}");
                return false;
            }

            Debug.Log($"[Auth] GPGS authenticated: {PlayGamesPlatform.Instance.GetUserId()}");

            // ── Capture display hint for username generation ──
            // GetUserDisplayName() returns the player's Google profile name.
            // If your GPGS console has email scope enabled you could also
            // try Social.localUser.userName as an alternative source.
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                AuthDisplayHint = displayName;
                Debug.Log($"[Auth] Display hint captured: {AuthDisplayHint}");
            }

            // ── Request server-side auth code for Unity Authentication ──
            var codeTcs = new UniTaskCompletionSource<string>();
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => codeTcs.TrySetResult(code));
            string authCode = await codeTcs.Task;

            if (string.IsNullOrEmpty(authCode))
            {
                Debug.LogError("[Auth] GPGS auth code was empty.");
                return false;
            }

            token.ThrowIfCancellationRequested();

            SetStatus("Connecting to server...");
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);

            Debug.Log($"[Auth] GPGS sign-in complete. Player ID: {PlayerId}");
            return true;
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[Auth] GPGS login cancelled.");
            return false;
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

    // ═══════════════════════════════════════════════════════════════
    //  ANONYMOUS LOGIN
    // ═══════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════
    //  STARTER CURRENCY
    // ═══════════════════════════════════════════════════════════════

    private const string STARTER_GRANTED_KEY = "starter_currency_granted";
    private const int STARTER_GOLD = 1000000;
    private const int STARTER_GEMS = 10000;
    private const int STARTER_POWER = 5;

    // ═══════════════════════════════════════════════════════════════
    //  RESULT HANDLERS
    // ═══════════════════════════════════════════════════════════════

    private async UniTaskVoid OnSignInSuccess(CancellationToken token)
    {
        HideAllButtons();
        SetStatus("Loading player data...");

        try
        {
            bool alreadyGranted = await CloudSaveManager.Instance
                .LoadValueAsync<bool>(STARTER_GRANTED_KEY, false);

            token.ThrowIfCancellationRequested();

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
            await RemoteConfigManager.Instance.FetchConfig();
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[Auth] Sign-in success handler cancelled.");
            return;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Auth] Starter currency error: {ex.Message}");
        }

        SetStatus("Signed in! Loading...");
        Debug.Log($"[Auth] Loading {mainMenuSceneName}. Player ID: {PlayerId}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnSignInFailed()
    {
        SetStatus("Sign-in failed. Check your connection.");
        Debug.LogError("[Auth] All sign-in methods failed.");
        ShowRetryOnly();
    }

    // ═══════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ═══════════════════════════════════════════════════════════════

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

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API  (Wire to UI Buttons via Inspector)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Wire to Retry button. Restarts the full boot sequence.</summary>
    public void RetrySignIn()
    {
        BootAsync(cts.Token).Forget();
    }

    /// <summary>Wire to Anonymous Login button.</summary>
    public void LoginAnonymously()
    {
        LoginAnonymouslyAsync(cts.Token).Forget();
    }

    private async UniTaskVoid LoginAnonymouslyAsync(CancellationToken token)
    {
        HideAllButtons();

        bool signedIn = await TryAnonymousLogin();

        if (signedIn)
            OnSignInSuccess(token).Forget();
        else
            OnSignInFailed();
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        AuthDisplayHint = null;
        Debug.Log("[Auth] Signed out.");
    }
}