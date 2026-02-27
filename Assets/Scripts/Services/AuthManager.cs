// using System;
// using System.Threading.Tasks;
// using UnityEngine;
// using Unity.Services.Core;
// using Unity.Services.Authentication;

// #if UNITY_ANDROID
// using GooglePlayGames;
// using GooglePlayGames.BasicApi;
// #endif

// namespace com.birdhunter.core.Services
// {
//     public interface IAuthManager
//     {
//         Task<bool> LoginWithGPGSAsync();
//         Task<bool> LoginWithEmailAsync(string email, string password);
//         Task<bool> LoginAnonymousAsync();
//     }

//     public class AuthManager : IAuthManager
//     {
//         // --- 1. GOOGLE PLAY GAMES LOGIN ---
//         public async Task<bool> LoginWithGPGSAsync()
//         {
//             await EnsureInitializedAsync();

// #if UNITY_ANDROID && !UNITY_EDITOR
//             try
//             {
//                 Debug.Log("[GPGS Auth] Initializing GPGS...");
//                 PlayGamesPlatform.Activate();

//                 // FIX 1: Was UniTaskCompletionSource<string> — UniTask is not imported here.
//                 //         Changed to the standard BCL TaskCompletionSource<string>.
//                 var tcs = new TaskCompletionSource<string>();

//                 PlayGamesPlatform.Instance.Authenticate((status) =>
//                 {
//                     if (status == SignInStatus.Success)
//                     {
//                         Debug.Log("[GPGS Auth] GPGS Authentication successful.");
//                         PlayGamesPlatform.Instance.RequestServerSideAccess(true, (code) =>
//                         {
//                             if (string.IsNullOrEmpty(code))
//                             {
//                                 tcs.TrySetException(new Exception("Failed to get GPGS Server Auth Code."));
//                             }
//                             else
//                             {
//                                 tcs.TrySetResult(code);
//                             }
//                         });
//                     }
//                     else
//                     {
//                         Debug.LogError($"[GPGS Auth] GPGS Authentication failed: {status}");
//                         tcs.TrySetException(new Exception($"GPGS Authentication failed: {status}"));
//                     }
//                 });

//                 string authCode = await tcs.Task;

//                 Debug.Log("[GPGS Auth] Signing into Unity Services with GPGS...");
//                 await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);

//                 // FIX 2: Was referencing undefined field `PlayerId`.
//                 //         Corrected to AuthenticationService.Instance.PlayerId.
//                 Debug.Log($"[GPGS Auth] Unity Authentication successful. Player ID: {AuthenticationService.Instance.PlayerId}");

//                 // FIX 3: The #if block had no return statement, causing a compiler error.
//                 //         Added explicit return true on success.
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[GPGS Auth] Sign in failed! {ex.Message}");
//                 return false;
//             }
// #else
//             Debug.LogWarning("[GPGS Auth] GPGS skipped (Editor/Non-Android).");
//             await Task.Delay(100);
//             return false;
// #endif
//         }

//         // --- 2. SMART EMAIL LOGIN (Login OR Register) ---
//         public async Task<bool> LoginWithEmailAsync(string email, string password)
//         {
//             Debug.Log($"[AuthManager] Attempting Smart Email Login for: {email}");
//             await EnsureInitializedAsync();

//             try
//             {
//                 await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(email, password);
//                 Debug.Log("[AuthManager] Login Successful.");
//                 return true;
//             }
//             catch (AuthenticationException ex)
//             {
//                 // 10003 is the specific error code for 'Account Not Found'
//                 if (ex.ErrorCode == 10003)
//                 {
//                     Debug.Log("[AuthManager] Account not found. Creating new account automatically...");
//                     return await RegisterInternalAsync(email, password);
//                 }

//                 Debug.LogError($"[AuthManager] Login Failed: {ex.Message} (Error Code: {ex.ErrorCode})");
//                 return false;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[AuthManager] Unexpected Error: {ex.Message}");
//                 return false;
//             }
//         }

//         private async Task<bool> RegisterInternalAsync(string email, string password)
//         {
//             try
//             {
//                 await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(email, password);
//                 Debug.Log("[AuthManager] Registration and Login Successful.");
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[AuthManager] Auto-Registration Failed: {ex.Message}");
//                 return false;
//             }
//         }

//         // --- 3. ANONYMOUS LOGIN ---
//         public async Task<bool> LoginAnonymousAsync()
//         {
//             await EnsureInitializedAsync();
//             try
//             {
//                 await AuthenticationService.Instance.SignInAnonymouslyAsync();
//                 Debug.Log("[AuthManager] Anonymous Login Successful.");
//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[AuthManager] Anonymous Login Failed: {ex.Message}");
//                 return false;
//             }
//         }

//         private async Task EnsureInitializedAsync()
//         {
//             if (UnityServices.State == ServicesInitializationState.Uninitialized)
//             {
//                 await UnityServices.InitializeAsync();
//             }
//         }
//     }
// }
