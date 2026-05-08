using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.PushNotifications;
using UnityEngine;

namespace Services
{
    /// <summary>
    /// Wraps Unity Gaming Services' Push Notifications package. Registers the
    /// device with APNs (iOS) / FCM (Android), gated by the in-app NotificationEnabled
    /// toggle. Idempotent — calling Register multiple times just re-confirms targeting.
    ///
    /// UGS targeting is keyed off the player's Unity Authentication ID, so the user
    /// must be signed in before registration. BootController calls RegisterIfEnabledAsync
    /// after auth completes.
    /// </summary>
    public class PushNotificationService
    {
        public static PushNotificationService Instance { get; private set; }

        private const string PrefKey = "NotificationEnabled";

        public string DeviceToken { get; private set; }
        public bool IsRegistered { get; private set; }
        public static bool UserOptedIn => PlayerPrefs.GetInt(PrefKey, 1) == 1;

        public static void Initialize()
        {
            Instance ??= new PushNotificationService();
        }

        /// <summary>
        /// Register only if the user hasn't opted out. Safe to call from boot flow.
        /// Returns the device token (or null if user opted out / denied OS permission).
        /// </summary>
        public async UniTask<string> RegisterIfEnabledAsync(CancellationToken ct = default)
        {
            if (!UserOptedIn)
            {
                Debug.Log("[Push] User opted out — skipping registration.");
                return null;
            }
            return await RegisterAsync(ct);
        }

        /// <summary>
        /// Force-register regardless of opt-in flag. Use when the user explicitly
        /// flips the settings toggle on.
        /// </summary>
        public async UniTask<string> RegisterAsync(CancellationToken ct = default)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.LogWarning("[Push] UnityServices not initialized — registration skipped.");
                return null;
            }

            if (IsRegistered && !string.IsNullOrEmpty(DeviceToken))
                return DeviceToken;

            try
            {
                Debug.Log("[Push] Registering for push notifications…");
                string token = await PushNotificationsService.Instance
                    .RegisterForPushNotificationsAsync()
                    .AsUniTask().AttachExternalCancellation(ct);

                DeviceToken = token;
                IsRegistered = !string.IsNullOrEmpty(token);

                if (IsRegistered)
                {
                    int prefixLen = Math.Min(16, token.Length);
                    Debug.Log($"[Push] Registered. Token prefix: {token.Substring(0, prefixLen)}…");
                }
                else
                {
                    Debug.LogWarning("[Push] Registration returned empty token. User likely denied OS permission.");
                }

                return token;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Push] Registration failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Persist the in-app opt-in flag. If the user newly opts in and we haven't
        /// registered yet, kick off registration (which triggers the OS prompt on iOS
        /// the first time).
        ///
        /// NOTE: UGS does not expose a programmatic "unsubscribe" — once a token is
        /// uploaded, the dashboard can still target it. Real opt-out requires the
        /// player to disable notifications at the OS level (Settings → app).
        /// </summary>
        public void SetUserOptedIn(bool value)
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Push] User opt-in set to {value}.");

            if (value && !IsRegistered)
                RegisterAsync().Forget();
        }
    }
}
