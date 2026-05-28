using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Core.Environments;


#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if UNITY_IOS
using Apple.GameKit;
using Apple.GameKit.Players;
#endif

public class AuthService
{
    // ─── Public State ───
    public bool IsSignedIn => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => AuthenticationService.Instance?.PlayerId;
    public string AuthDisplayHint { get; private set; }
    public bool IsAnonymous { get; private set; }

    // ─── Config ───
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 1500;

    private bool gpgsActivated = false;

    // ─── Public API ───

    /// <summary>
    /// Full sign-in flow. Tries GPGS on Android, falls back to anonymous elsewhere.
    /// Publishes progress/completion events via EventBus.
    /// </summary>
    public async UniTask<bool> SignInAsync(CancellationToken ct = default)
    {
        try
        {
            EventBus.Publish(new AuthStartedEvent { method = "auto" });

            ReportProgress(0.0f, "Initializing services");
            if (!await InitializeUnityServicesAsync(ct))
                return PublishFailure("Services init failed", AuthFailureReason.ServicesInitFailed);

            // Already signed in from prior session — fast path
            if (IsSignedIn)
            {
                ReportProgress(1.0f, "Already signed in");
                PublishSuccess();
                return true;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            ReportProgress(0.3f, "Signing in with Google Play");
            if (await TryGpgsSignInAsync(ct))
            {
                PublishSuccess();
                return true;
            }

            // GPGS failed — do NOT auto-fallback. Let UI ask user.
            return PublishFailure("Google Play sign-in failed", AuthFailureReason.GpgsSignInFailed);
#elif UNITY_IOS && !UNITY_EDITOR
            ReportProgress(0.3f, "Signing in with Game Center");
            if (await TryAppleGameCenterSignInAsync(ct))
            {
                PublishSuccess();
                return true;
            }

            // Game Center failed — do NOT auto-fallback. Let UI ask user.
            return PublishFailure("Game Center sign-in failed", AuthFailureReason.AppleGameCenterSignInFailed);
#else
            ReportProgress(0.3f, "Signing in anonymously");
            if (await TrySignInAnonymouslyAsync(ct))
            {
                PublishSuccess();
                return true;
            }
            return PublishFailure("Anonymous sign-in failed", AuthFailureReason.AnonymousSignInFailed);
#endif
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[AuthService] Sign-in cancelled.");
            return PublishFailure("Cancelled", AuthFailureReason.Cancelled);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] Unexpected error: {ex.Message}\n{ex.StackTrace}");
            return PublishFailure(ex.Message, AuthFailureReason.Unknown);
        }
    }

    /// <summary>
    /// Explicit anonymous sign-in — call when user clicks "Continue as Guest" button.
    /// </summary>
    public async UniTask<bool> SignInAnonymouslyAsync(CancellationToken ct = default)
    {
        try
        {
            EventBus.Publish(new AuthStartedEvent { method = "anonymous" });

            if (!await InitializeUnityServicesAsync(ct))
                return PublishFailure("Services init failed", AuthFailureReason.ServicesInitFailed);

            if (IsSignedIn)
            {
                PublishSuccess();
                return true;
            }

            ReportProgress(0.5f, "Signing in anonymously");
            if (await TrySignInAnonymouslyAsync(ct))
            {
                PublishSuccess();
                return true;
            }

            return PublishFailure("Anonymous sign-in failed", AuthFailureReason.AnonymousSignInFailed);
        }
        catch (OperationCanceledException)
        {
            return PublishFailure("Cancelled", AuthFailureReason.Cancelled);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] Anonymous error: {ex.Message}");
            return PublishFailure(ex.Message, AuthFailureReason.Unknown);
        }
    }

    public void SignOut()
    {
        if (IsSignedIn)
            AuthenticationService.Instance.SignOut();
        AuthDisplayHint = null;
        IsAnonymous = false;
        Debug.Log("[AuthService] Signed out.");
    }

    // ─── Private: Unity Services Init ───

    private async UniTask<bool> InitializeUnityServicesAsync(CancellationToken ct)
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return true;

        if (UnityServices.State == ServicesInitializationState.Initializing)
        {
            await UniTask.WaitUntil(
                () => UnityServices.State != ServicesInitializationState.Initializing,
                cancellationToken: ct);
            return UnityServices.State == ServicesInitializationState.Initialized;
        }

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!gpgsActivated)
            {
                PlayGamesPlatform.Activate();
                gpgsActivated = true;
            }
#endif
            // Unity Services environment. This is the first InitializeAsync in the boot
            // sequence (auth runs first), so it sets the environment for all services.
            // Flip back to "production" for release builds.
            const string environmentName = "production";
            var options = new InitializationOptions().SetEnvironmentName(environmentName);
            await UnityServices.InitializeAsync(options).AsUniTask().AttachExternalCancellation(ct);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] UnityServices init failed: {ex.Message}");
            return false;
        }
    }

    // ─── Private: GPGS Flow ───

#if UNITY_ANDROID && !UNITY_EDITOR
    private async UniTask<bool> TryGpgsSignInAsync(CancellationToken ct)
    {
        try
        {
            // Step 1: Authenticate with Google Play Games
            var authTcs = new UniTaskCompletionSource<SignInStatus>();
            PlayGamesPlatform.Instance.Authenticate(status => authTcs.TrySetResult(status));
            var authStatus = await authTcs.Task.AttachExternalCancellation(ct);

            if (authStatus != SignInStatus.Success)
            {
                Debug.LogWarning($"[AuthService] GPGS auth returned: {authStatus}");
                return false;
            }

            // Step 2: Capture display hint (player's Google profile name)
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                AuthDisplayHint = displayName;
                Debug.Log($"[AuthService] Display hint captured: {AuthDisplayHint}");
            }

            ReportProgress(0.6f, "Requesting server access");

            // Step 3: Get server-side auth code for Unity Authentication handoff
            var codeTcs = new UniTaskCompletionSource<string>();
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code => codeTcs.TrySetResult(code));
            string authCode = await codeTcs.Task.AttachExternalCancellation(ct);

            if (string.IsNullOrEmpty(authCode))
            {
                Debug.LogError("[AuthService] GPGS auth code was empty.");
                return false;
            }

            ReportProgress(0.85f, "Connecting to server");

            // Step 4: Exchange auth code with Unity Authentication
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode)
                .AsUniTask().AttachExternalCancellation(ct);

            IsAnonymous = false;
            Debug.Log($"[AuthService] GPGS sign-in complete. Player ID: {PlayerId}");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[AuthService] GPGS Unity Auth failed: {ex.ErrorCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] GPGS error: {ex.Message}");
            return false;
        }
    }
#endif

    // ─── Private: Apple Game Center Flow ───

#if UNITY_IOS && !UNITY_EDITOR
    private async UniTask<bool> TryAppleGameCenterSignInAsync(CancellationToken ct)
    {
        try
        {
            // Step 1: Authenticate the local player with Game Center.
            var localPlayer = await GKLocalPlayer.Authenticate()
                .AsUniTask().AttachExternalCancellation(ct);

            if (localPlayer == null || !localPlayer.IsAuthenticated)
            {
                Debug.LogWarning("[AuthService] Game Center auth returned unauthenticated player.");
                return false;
            }

            // Step 2: Capture display hint (player's Game Center display name).
            string displayName = GKLocalPlayer.Local.DisplayName;
            if (!string.IsNullOrEmpty(displayName))
            {
                AuthDisplayHint = displayName;
                Debug.Log($"[AuthService] Display hint captured: {AuthDisplayHint}");
            }

            ReportProgress(0.6f, "Requesting server access");

            // Step 3: Get identity verification items for Unity Authentication handoff.
            var verification = await GKLocalPlayer.Local.FetchItemsForIdentityVerificationSignature()
                .AsUniTask().AttachExternalCancellation(ct);

            if (verification == null
                || string.IsNullOrEmpty(verification.PublicKeyUrl)
                || verification.Signature == null
                || verification.Salt == null)
            {
                Debug.LogError("[AuthService] Game Center verification response was empty.");
                return false;
            }

            string signatureB64 = Convert.ToBase64String(verification.GetSignature());
            string saltB64 = Convert.ToBase64String(verification.GetSalt());
            string teamPlayerId = GKLocalPlayer.Local.TeamPlayerId;

            ReportProgress(0.85f, "Connecting to server");

            Debug.Log("[AuthService] Game Center payload fetched");

            // Step 4: Exchange the signed payload with Unity Authentication.
            await AuthenticationService.Instance.SignInWithAppleGameCenterAsync(
                    signatureB64,
                    teamPlayerId,
                    verification.PublicKeyUrl,
                    saltB64,
                    verification.Timestamp)
                .AsUniTask().AttachExternalCancellation(ct);

            IsAnonymous = false;
            Debug.Log($"[AuthService] Apple Game Center sign-in complete. Player ID: {PlayerId}");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[AuthService] Apple GC Unity Auth failed: {ex.ErrorCode} - {ex.Message}");
            return false;
        }
        catch (GameKitException ex)
        {
            Debug.LogError($"[AuthService] GameKit error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] Apple Game Center error: {ex.Message}");
            return false;
        }
    }
#endif

    // ─── Private: Anonymous Flow ───

    private async UniTask<bool> TrySignInAnonymouslyAsync(CancellationToken ct)
    {
        for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync()
                    .AsUniTask().AttachExternalCancellation(ct);

                IsAnonymous = true;
                AuthDisplayHint = null;
                Debug.Log($"[AuthService] Anonymous sign-in success. Player ID: {PlayerId}");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AuthenticationException ex) when (
                ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked ||
                ex.ErrorCode == AuthenticationErrorCodes.AccountLinkLimitExceeded ||
                ex.ErrorCode == AuthenticationErrorCodes.InvalidParameters)
            {
                Debug.LogError($"[AuthService] Non-retriable auth error {ex.ErrorCode}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthService] Anonymous attempt {attempt}/{MAX_RETRY_ATTEMPTS} failed: {ex.Message}");
                if (attempt < MAX_RETRY_ATTEMPTS)
                    await UniTask.Delay(RETRY_DELAY_MS, cancellationToken: ct);
            }
        }
        return false;
    }

    // ─── Private: Event Helpers ───

    private void ReportProgress(float progress, string step)
    {
        EventBus.Publish(new AuthProgressEvent { progress = progress, currentStep = step });
    }

    private void PublishSuccess()
    {
        EventBus.Publish(new AuthCompletedEvent
        {
            playerId = PlayerId,
            displayHint = AuthDisplayHint,
            wasAnonymous = IsAnonymous
        });
    }

    private bool PublishFailure(string message, AuthFailureReason reason)
    {
        EventBus.Publish(new AuthFailedEvent { errorMessage = message, reason = reason });
        return false;
    }
}