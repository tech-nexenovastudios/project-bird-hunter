using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using Unity.Services.Analytics;
using Unity.Services.Core;


// ═══════════════════════════════════════════════════════════════════════════════
//  ENUMS — Define all game-specific types here.
//  ✅ Future-Ready: Add new cannons or powers here and the whole system adapts.
//  ✅ Type-Safe:    No magic strings. Typos become compile errors, not silent bugs.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// All 7 cannon types in the game inventory.
/// HOW TO USE: Replace Cannon_01..07 with your actual cannon names.
/// EXAMPLE: FireCannon, IceCannon, LaserCannon, etc.
/// </summary>
public enum CannonType
{
    Cannon_01 = 0,
    Cannon_02 = 1,
    Cannon_03 = 2,
    Cannon_04 = 3,
    Cannon_05 = 4,
    Cannon_06 = 5,
    Cannon_07 = 6
}

/// <summary>
/// All 29 slot power types in the game.
/// HOW TO USE: Replace Power_01..29 with your actual power names.
/// EXAMPLE: ShieldPower, SpeedBoost, DoubleDamage, etc.
/// </summary>
public enum PowerType
{
    Power_01 = 0, Power_02 = 1, Power_03 = 2, Power_04 = 3, Power_05 = 4,
    Power_06 = 5, Power_07 = 6, Power_08 = 7, Power_09 = 8, Power_10 = 9,
    Power_11 = 10, Power_12 = 11, Power_13 = 12, Power_14 = 13, Power_15 = 14,
    Power_16 = 15, Power_17 = 16, Power_18 = 17, Power_19 = 18, Power_20 = 19,
    Power_21 = 20, Power_22 = 21, Power_23 = 22, Power_24 = 23, Power_25 = 24,
    Power_26 = 25, Power_27 = 26, Power_28 = 27, Power_29 = 28
}


// ═══════════════════════════════════════════════════════════════════════════════
//  INTERFACE — The contract every analytics provider MUST follow.
//  ✅ Future-Ready: Swap Unity Analytics for Firebase or any other service
//                  by creating a new class that implements this interface.
//                  Zero changes needed in the rest of your game code.
// ═══════════════════════════════════════════════════════════════════════════════

public interface IAnalyticsService
{
    // ── Core Lifecycle ──────────────────────────────────────────────────────
    bool IsInitialized { get; }
    UniTask InitializeAsync();
    UniTask ShutdownAsync();

    // ── User ────────────────────────────────────────────────────────────────
    void SetUserProperties(string playerId, Dictionary<string, object> properties = null);

    // ── Generic Tracking (used internally and for one-off events) ───────────
    void TrackEvent(string eventName, Dictionary<string, object> parameters = null);
    void ReportException(Exception exception, Dictionary<string, object> context = null);
    void ReportError(string message, Dictionary<string, object> context = null);

    // ── Screen / Navigation ─────────────────────────────────────────────────
    void TrackScreenView(string screenName, Dictionary<string, object> parameters = null);

    // ── Monetization ────────────────────────────────────────────────────────
    void TrackPurchase(string productId, decimal amount, string currency,
                       Dictionary<string, object> parameters = null);

    // ══════════════════════════════════════════════════════════════════════
    //  GAME-SPECIFIC EVENTS  (your 6 requested events live here)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Fires once in a player's lifetime — first time they log in.</summary>
    void TrackNewUserFirstLogin(string playerId);

    /// <summary>Fires every time the player taps the Play button.</summary>
    void TrackPlayButtonClicked(string fromScreen = "main_menu");

    /// <summary>Fires when a chapter is fully completed.</summary>
    void TrackChapterCompleted(string chapterName, int chapterIndex,
                               float completionTimeSeconds,
                               Dictionary<string, object> extraParams = null);

    /// <summary>Fires when a cannon is unlocked in the inventory.</summary>
    void TrackCannonUnlocked(CannonType cannonType, string unlockMethod = "default");

    /// <summary>Fires when the session ends (app background/quit).</summary>
    void TrackSessionEnded();

    /// <summary>Fires when a slot power is unlocked.</summary>
    void TrackPowerUnlocked(PowerType powerType, string unlockMethod = "default");
}


// ═══════════════════════════════════════════════════════════════════════════════
//  IMPLEMENTATION
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Production-ready Unity Analytics implementation.
///
/// DESIGN DECISIONS (explained for learning):
///   • PlayerPrefs key for first-login:  persists across sessions on the device.
///   • Session timer uses DateTime.UtcNow: immune to device clock changes mid-session.
///   • All methods are silent-fail with logging: analytics should NEVER crash the game.
///   • Enums are converted to strings before sending: analytics dashboards
///     receive human-readable names, not integer codes.
/// </summary>
public class AnalyticsServices : IAnalyticsService
{
    // ── State ────────────────────────────────────────────────────────────────
    private bool _isInitialized = false;
    private string _playerId;
    private DateTime _sessionStartTime;

    private readonly Dictionary<string, object> _userProperties =
        new Dictionary<string, object>();

    // PlayerPrefs key — stored permanently on device to detect first login.
    // "Analytics_" prefix avoids collisions with other PlayerPrefs keys.
    private const string FIRST_LOGIN_KEY = "Analytics_HasLaunchedBefore";

    public bool IsInitialized => _isInitialized;


    // ── Initialization ───────────────────────────────────────────────────────

    public async UniTask InitializeAsync()
    {
        if (_isInitialized)
        {
            Debug.Log("[Analytics] Already initialized.");
            return;
        }

        try
        {
            Debug.Log("[Analytics] Initializing...");

            if (Application.isEditor && !Application.isPlaying)
                Debug.LogWarning("[Analytics] Analytics may not work in Edit Mode.");

            SetInitialUserProperties();

            // Record session start time ONCE, here, reliably.
            _sessionStartTime = DateTime.UtcNow;

            _isInitialized = true;

            Debug.Log("[Analytics] Initialized successfully.");

            // Baseline event so every session appears in the dashboard.
            TrackEvent("session_started", new Dictionary<string, object>
            {
                { "environment",    GetEnvironment() },
                { "unity_version",  Application.unityVersion },
                { "platform",       Application.platform.ToString() },
                { "app_version",    Application.version }
            });
        }
        catch (Exception e)
        {
            // ✅ Silent-fail: analytics failure must NOT stop the game.
            Debug.LogError($"[Analytics] Init failed: {e.Message}");
        }

        await UniTask.CompletedTask;
    }

    public async UniTask ShutdownAsync()
    {
        if (!_isInitialized) return;

        try
        {
            Debug.Log("[Analytics] Shutting down...");

            // Always record session length on graceful shutdown.
            TrackSessionEnded();

            _playerId = null;
            _userProperties.Clear();
            _isInitialized = false;

            Debug.Log("[Analytics] Shut down.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] Shutdown error: {e.Message}");
        }

        await UniTask.CompletedTask;
    }


    // ══════════════════════════════════════════════════════════════════════════
    //  GAME-SPECIFIC EVENTS
    // ══════════════════════════════════════════════════════════════════════════

    // ── 1. New User First Login ──────────────────────────────────────────────
    /// <summary>
    /// Call this right after a user successfully logs in / creates an account.
    /// Internally checks if this is truly the first time using PlayerPrefs.
    ///
    /// HOW TO CALL (example):
    ///   _analytics.TrackNewUserFirstLogin(userId);
    /// </summary>
    public void TrackNewUserFirstLogin(string playerId)
    {
        if (!_isInitialized) return;

        try
        {
            bool isFirstTime = PlayerPrefs.GetInt(FIRST_LOGIN_KEY, 0) == 0;

            if (isFirstTime)
            {
                // Mark as seen — will never fire again on this device.
                PlayerPrefs.SetInt(FIRST_LOGIN_KEY, 1);
                PlayerPrefs.Save(); // Force-write to disk immediately.

                TrackEvent("new_user_first_login", new Dictionary<string, object>
                {
                    { "player_id",   playerId ?? "unknown" },
                    { "platform",    Application.platform.ToString() },
                    { "app_version", Application.version },
                    { "login_time",  DateTime.UtcNow.ToString("O") }
                });

                Debug.Log($"[Analytics] First login tracked for: {playerId}");
            }
            else
            {
                // Returning user — fire a regular login event instead.
                TrackEvent("returning_user_login", new Dictionary<string, object>
                {
                    { "player_id", playerId ?? "unknown" }
                });

                Debug.Log($"[Analytics] Returning user login tracked for: {playerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackNewUserFirstLogin error: {e.Message}");
        }
    }


    // ── 2. Play Button Click ─────────────────────────────────────────────────
    /// <summary>
    /// Call this in your Play Button's onClick handler.
    ///
    /// HOW TO CALL (example):
    ///   _analytics.TrackPlayButtonClicked("main_menu");
    ///   _analytics.TrackPlayButtonClicked("chapter_select");
    /// </summary>
    public void TrackPlayButtonClicked(string fromScreen = "main_menu")
    {
        if (!_isInitialized) return;

        try
        {
            TrackEvent("play_button_clicked", new Dictionary<string, object>
            {
                { "from_screen",  fromScreen },
                { "click_time",   DateTime.UtcNow.ToString("O") }
            });

            Debug.Log($"[Analytics] Play button clicked from: {fromScreen}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackPlayButtonClicked error: {e.Message}");
        }
    }


    // ── 3. Chapter Completed ─────────────────────────────────────────────────
    /// <summary>
    /// Call this when the chapter's end condition is met (not when the UI appears).
    ///
    /// HOW TO CALL (example):
    ///   float timeTaken = Time.timeSinceLevelLoad; // seconds spent in scene
    ///   _analytics.TrackChapterCompleted("Forest", 1, timeTaken);
    /// </summary>
    public void TrackChapterCompleted(string chapterName, int chapterIndex,
                                      float completionTimeSeconds,
                                      Dictionary<string, object> extraParams = null)
    {
        if (!_isInitialized || string.IsNullOrEmpty(chapterName)) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "chapter_name",             chapterName },
                { "chapter_index",            chapterIndex },
                { "completion_time_seconds",  Mathf.RoundToInt(completionTimeSeconds) },
                // Convert to minutes for dashboard readability.
                { "completion_time_minutes",  MathF.Round(completionTimeSeconds / 60f, 2) }
            };

            // Merge any extra params the caller wants to include.
            if (extraParams != null)
                foreach (var kvp in extraParams)
                    parameters[kvp.Key] = kvp.Value;

            TrackEvent("chapter_completed", parameters);

            Debug.Log($"[Analytics] Chapter completed: {chapterName} (#{chapterIndex})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackChapterCompleted error: {e.Message}");
        }
    }


    // ── 4. Cannon Unlocked ───────────────────────────────────────────────────
    /// <summary>
    /// Call this when a cannon is unlocked in the inventory.
    ///
    /// HOW TO CALL (example):
    ///   _analytics.TrackCannonUnlocked(CannonType.Cannon_03, "level_reward");
    ///   _analytics.TrackCannonUnlocked(CannonType.Cannon_07, "iap_purchase");
    ///
    /// unlockMethod examples: "level_reward", "iap_purchase", "daily_reward", "ad_reward"
    /// </summary>
    public void TrackCannonUnlocked(CannonType cannonType, string unlockMethod = "default")
    {
        if (!_isInitialized) return;

        try
        {
            TrackEvent("cannon_unlocked", new Dictionary<string, object>
            {
                { "cannon_name",    cannonType.ToString() },  // Human-readable in dashboard
                { "cannon_index",   (int)cannonType },        // Numeric for sorting/filtering
                { "unlock_method",  unlockMethod },
                { "total_cannons",  Enum.GetValues(typeof(CannonType)).Length }
            });

            Debug.Log($"[Analytics] Cannon unlocked: {cannonType} via {unlockMethod}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackCannonUnlocked error: {e.Message}");
        }
    }


    // ── 5. Session Length ────────────────────────────────────────────────────
    /// <summary>
    /// Called automatically by ShutdownAsync(). You can also call it manually
    /// when the app goes to background (OnApplicationPause).
    ///
    /// HOW TO CALL (from a MonoBehaviour — see note at bottom of file):
    ///   void OnApplicationPause(bool isPaused)
    ///   {
    ///       if (isPaused) _analytics.TrackSessionEnded();
    ///   }
    /// </summary>
    public void TrackSessionEnded()
    {
        if (!_isInitialized) return;

        try
        {
            var sessionDuration = DateTime.UtcNow - _sessionStartTime;

            TrackEvent("session_ended", new Dictionary<string, object>
            {
                { "session_length_seconds", (int)sessionDuration.TotalSeconds },
                { "session_length_minutes", Math.Round(sessionDuration.TotalMinutes, 2) },
                { "session_start_utc",      _sessionStartTime.ToString("O") },
                { "session_end_utc",        DateTime.UtcNow.ToString("O") }
            });

            Debug.Log($"[Analytics] Session ended. Duration: {sessionDuration.TotalMinutes:F1} min");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackSessionEnded error: {e.Message}");
        }
    }


    // ── 6. Power Unlocked ────────────────────────────────────────────────────
    /// <summary>
    /// Call this when any of the 29 slot powers is unlocked.
    ///
    /// HOW TO CALL (example):
    ///   _analytics.TrackPowerUnlocked(PowerType.Power_05, "chapter_reward");
    ///   _analytics.TrackPowerUnlocked(PowerType.Power_29, "iap_purchase");
    ///
    /// unlockMethod examples: "chapter_reward", "iap_purchase", "daily_login", "achievement"
    /// </summary>
    public void TrackPowerUnlocked(PowerType powerType, string unlockMethod = "default")
    {
        if (!_isInitialized) return;

        try
        {
            TrackEvent("power_unlocked", new Dictionary<string, object>
            {
                { "power_name",    powerType.ToString() },  // Human-readable in dashboard
                { "power_index",   (int)powerType },        // Numeric for sorting/filtering
                { "unlock_method", unlockMethod },
                { "total_powers",  Enum.GetValues(typeof(PowerType)).Length }
            });

            Debug.Log($"[Analytics] Power unlocked: {powerType} via {unlockMethod}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackPowerUnlocked error: {e.Message}");
        }
    }


    // ══════════════════════════════════════════════════════════════════════════
    //  GENERIC / CORE METHODS
    // ══════════════════════════════════════════════════════════════════════════

    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!_isInitialized || string.IsNullOrEmpty(eventName))
        {
            if (!string.IsNullOrEmpty(eventName))
                Debug.LogWarning($"[Analytics] Not initialized. Dropped event: {eventName}");
            return;
        }

        try
        {
            var eventData = MergeWithDefaults(parameters);

            // Stamp user properties on every event (do NOT send device specs every time —
            // only send the user_id so the dashboard can group events per player).
            if (!string.IsNullOrEmpty(_playerId))
                eventData.TryAdd("user_id", _playerId);

            var result = Analytics.CustomEvent(eventName, eventData);

            if (result == AnalyticsResult.Ok)
                Debug.Log($"[Analytics] ✓ {eventName}");
            else
                Debug.LogWarning($"[Analytics] ✗ {eventName} — result: {result}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackEvent '{eventName}' error: {e.Message}");
        }
    }

    public void SetUserProperties(string playerId, Dictionary<string, object> properties = null)
    {
        if (!_isInitialized) return;

        try
        {
            _playerId = playerId;

            if (!string.IsNullOrEmpty(playerId))
            {
                UnityServices.ExternalUserId = playerId;
                _userProperties["user_id"] = playerId;
                _userProperties["user_set_at"] = DateTime.UtcNow.ToString("O");
            }

            if (properties != null)
                foreach (var prop in properties)
                    _userProperties[prop.Key] = prop.Value;

            Debug.Log($"[Analytics] User properties set: {playerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] SetUserProperties error: {e.Message}");
        }
    }

    public void ReportException(Exception exception, Dictionary<string, object> context = null)
    {
        if (!_isInitialized || exception == null) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "exception_type",        exception.GetType().Name },
                { "exception_message",     exception.Message },
                { "exception_stack_trace", exception.StackTrace ?? "" }
            };

            if (context != null)
                foreach (var kvp in context)
                    parameters[kvp.Key] = kvp.Value;

            TrackEvent("exception_occurred", parameters);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] ReportException error: {e.Message}");
        }
    }

    public void ReportError(string message, Dictionary<string, object> context = null)
    {
        if (!_isInitialized || string.IsNullOrEmpty(message)) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "error_message", message },
                { "error_type",    "CustomError" }
            };

            if (context != null)
                foreach (var kvp in context)
                    parameters[kvp.Key] = kvp.Value;

            TrackEvent("error_occurred", parameters);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] ReportError error: {e.Message}");
        }
    }

    public void TrackScreenView(string screenName, Dictionary<string, object> parameters = null)
    {
        if (!_isInitialized || string.IsNullOrEmpty(screenName)) return;

        try
        {
            var eventParams = new Dictionary<string, object> { { "screen_name", screenName } };

            if (parameters != null)
                foreach (var kvp in parameters)
                    eventParams[kvp.Key] = kvp.Value;

            TrackEvent("screen_view", eventParams);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackScreenView error: {e.Message}");
        }
    }

    public void TrackPurchase(string productId, decimal amount, string currency,
                              Dictionary<string, object> parameters = null)
    {
        if (!_isInitialized || string.IsNullOrEmpty(productId)) return;

        try
        {
            var eventParams = new Dictionary<string, object>
            {
                { "product_id", productId },
                { "amount",     amount },
                { "currency",   currency ?? "USD" }
            };

            if (parameters != null)
                foreach (var kvp in parameters)
                    eventParams[kvp.Key] = kvp.Value;

            TrackEvent("purchase", eventParams);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] TrackPurchase error: {e.Message}");
        }
    }


    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Stamps every event with a UTC timestamp. Nothing else — keep events lean.
    /// </summary>
    private Dictionary<string, object> MergeWithDefaults(Dictionary<string, object> additional)
    {
        var merged = new Dictionary<string, object>
        {
            { "timestamp_utc", DateTime.UtcNow.ToString("O") }
        };

        if (additional != null)
            foreach (var param in additional)
                merged[param.Key] = param.Value;

        return merged;
    }

    private void SetInitialUserProperties()
    {
        try
        {
            _userProperties.Clear();
            _userProperties["device_model"] = SystemInfo.deviceModel;
            _userProperties["device_type"] = SystemInfo.deviceType.ToString();
            _userProperties["os"] = SystemInfo.operatingSystem;
            _userProperties["processor"] = SystemInfo.processorType;
            _userProperties["cpu_cores"] = SystemInfo.processorCount;
            _userProperties["ram_mb"] = SystemInfo.systemMemorySize;
            _userProperties["gpu"] = SystemInfo.graphicsDeviceName;
            _userProperties["vram_mb"] = SystemInfo.graphicsMemorySize;
            _userProperties["app_version"] = Application.version;
            _userProperties["unity_version"] = Application.unityVersion;
            _userProperties["platform"] = Application.platform.ToString();
            _userProperties["environment"] = GetEnvironment();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] SetInitialUserProperties error: {e.Message}");
        }
    }

    private string GetEnvironment()
    {
#if DEVELOPMENT_BUILD
            return "development";
#elif UNITY_EDITOR
        return "editor";
#else
            return "production";
#endif
    }
}


// ═══════════════════════════════════════════════════════════════════════════════
//  HOW TO WIRE SESSION TRACKING IN A MONOBEHAVIOUR
//  (Add this to your GameManager or AppManager — the persistent root object)
// ═══════════════════════════════════════════════════════════════════════════════

/*
public class AppLifecycleHandler : MonoBehaviour
{
    private IAnalyticsService _analytics; // Injected via ServiceLocator / DI

    void OnApplicationPause(bool isPaused)
    {
        // iOS/Android: fires when player goes to home screen.
        if (isPaused)
            _analytics.TrackSessionEnded();
    }

    void OnApplicationQuit()
    {
        // PC/Editor: fires on window close.
        _analytics.TrackSessionEnded();
    }
}
*/