
using System;
using UnityEngine;
using Unity.Services.RemoteConfig;
using Cysharp.Threading.Tasks;

public class RemoteConfigManager : MonoBehaviour
{
    // Your CurrencyManager uses a static property pattern — we match it.
    public static RemoteConfigManager Instance { get; private set; }

    // ========================================================================
    // EVENTS
    // ========================================================================
    // Subscribe to these from UI scripts, gameplay scripts, etc.
    // Pattern matches your CurrencyManager's static event style.

    /// <summary>Fired when all remote config values are fetched and ready.</summary>
    public static event Action OnConfigReady;

    /// <summary>Fired when a specific key changes (useful for reactive UI).</summary>
    public static event Action<string> OnConfigKeyChanged;

    // ========================================================================
    // STATE
    // ========================================================================

    public bool IsReady { get; private set; }
    private bool _isFetching;

    // ========================================================================
    // REQUIRED STRUCTS — Remote Config needs these even if empty
    // ========================================================================
    // You can add fields here to enable conditional delivery.
    // Example: send playerLevel so dashboard can deliver different
    // values to beginners vs veterans.

    public struct UserAttributes
    {
        public int playerLevel;
        public string platform;
    }

    public struct AppAttributes
    {
        public string appVersion;
    }

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Call this AFTER Unity Services + Auth are initialized.
    /// </summary>
    public async UniTask FetchConfig()
    {
        if (_isFetching) return;
        _isFetching = true;

        try
        {
            RemoteConfigService.Instance.FetchCompleted += HandleFetchCompleted;

            var userAttr = new UserAttributes
            {
                playerLevel = PlayerPrefs.GetInt("playerLevel", 1),
                platform = Application.platform.ToString()
            };

            var appAttr = new AppAttributes
            {
                appVersion = Application.version
            };

            await RemoteConfigService.Instance.FetchConfigsAsync(userAttr, appAttr);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfig] Fetch failed: {ex.Message}");
            // Still mark as ready — scripts will use default values
            IsReady = true;
            OnConfigReady?.Invoke();
        }
        finally
        {
            _isFetching = false;
        }
    }

    private void HandleFetchCompleted(ConfigResponse response)
    {
        RemoteConfigService.Instance.FetchCompleted -= HandleFetchCompleted;

        string origin = response.requestOrigin switch
        {
            ConfigOrigin.Remote => "server",
            ConfigOrigin.Cached => "cache",
            _ => "defaults"
        };

        Debug.Log($"[RemoteConfig] Config loaded from {origin}");
        IsReady = true;
        OnConfigReady?.Invoke();
    }

    // ========================================================================
    // VALUE GETTERS — Type-safe access with sensible defaults
    // ========================================================================
    // Every getter has a hardcoded default. If Remote Config is unreachable
    // (no internet, first launch, server down), these defaults keep your
    // game running perfectly. The game should NEVER break because of
    // missing Remote Config values.

    public int GetInt(string key, int defaultValue)
    {
        return RemoteConfigService.Instance.appConfig.GetInt(key, defaultValue);
    }

    public float GetFloat(string key, float defaultValue)
    {
        return RemoteConfigService.Instance.appConfig.GetFloat(key, defaultValue);
    }

    public string GetString(string key, string defaultValue)
    {
        return RemoteConfigService.Instance.appConfig.GetString(key, defaultValue);
    }

    public bool GetBool(string key, bool defaultValue)
    {
        return RemoteConfigService.Instance.appConfig.GetBool(key, defaultValue);
    }

    public T GetJson<T>(string key, T defaultValue)
    {
        try
        {
            string json = GetString(key, null);
            if (string.IsNullOrEmpty(json)) return defaultValue;
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RemoteConfig] JSON parse failed for '{key}': {ex.Message}");
            return defaultValue;
        }
    }

    // ========================================================================
    // ========================================================================
    //
    //  CATEGORY 1: GAME BALANCE
    //
    //  These are numbers your game designer wants to tweak after launch.
    //  "The boss is too easy" → change bossHealth from dashboard → done.
    //
    // ========================================================================
    // ========================================================================

    // --- Enemy Stats ---
    public int BossHealth => GetInt("bossHealth", 5000);
    public int BossAttackDamage => GetInt("bossAttackDamage", 50);
    public float EnemySpeedMultiplier => GetFloat("enemySpeedMult", 1.0f);
    public int MaxEnemiesPerWave => GetInt("maxEnemiesPerWave", 10);

    // --- Player Stats ---
    public int PlayerStartHealth => GetInt("playerStartHealth", 100);
    public float PlayerDamageMultiplier => GetFloat("playerDamageMult", 1.0f);

    // --- Level Tuning ---
    public float DifficultyScaleFactor => GetFloat("difficultyScale", 1.0f);
    public int XPPerKill => GetInt("xpPerKill", 10);
    public float CooldownMultiplier => GetFloat("cooldownMult", 1.0f);

    // ========================================================================
    //  CATEGORY 2: REWARD TUNING (non-purchase rewards)
    // ========================================================================
    //
    // IMPORTANT DISTINCTION:
    // The AMOUNTS here are what the player earns for actions like watching
    // an ad or logging in daily. The actual GRANTING of currency still goes
    // through your CurrencyManager.Instance.AddGold(amount).
    //
    // Remote Config controls HOW MUCH. CurrencyManager handles the transaction.
    //
    // Example flow:
    //   int reward = GameRemoteConfig.Instance.AdWatchGoldReward; // from RC: 30
    //   await CurrencyManager.Instance.AddGold(reward);           // via Economy

    public int DailyLoginGold => GetInt("dailyLoginGold", 50);
    public int DailyLoginGems => GetInt("dailyLoginGems", 5);
    public int DailyLoginPower => GetInt("dailyLoginPower", 10);
    public int AdWatchGoldReward => GetInt("adWatchGoldReward", 30);
    public int AdWatchGemReward => GetInt("adWatchGemReward", 2);
    public int LevelCompleteBaseGold => GetInt("levelCompleteBaseGold", 100);
    public int LevelCompleteBonusPerStar => GetInt("levelCompleteBonusPerStar", 25);

    // ========================================================================
    //  CATEGORY 3: FEATURE FLAGS
    // ========================================================================
    //
    // The most powerful safety net. Something crashing on Samsung phones?
    // Set pvpEnabled = false from dashboard → crash stops instantly.
    //
    // Usage in your game code:
    //   if (GameRemoteConfig.Instance.PvPEnabled) { ShowPvPButton(); }

    public bool PvPEnabled => GetBool("pvpEnabled", false);
    public bool ChatEnabled => GetBool("chatEnabled", false);
    public bool LeaderboardEnabled => GetBool("leaderboardEnabled", true);
    public bool NewUIEnabled => GetBool("newUIEnabled", false);
    public bool SocialSharingEnabled => GetBool("socialShareEnabled", true);
    public bool DebugModeEnabled => GetBool("debugMode", false);

    // ========================================================================
    //  CATEGORY 4: AD CONFIGURATION
    // ========================================================================
    //
    // Ad fatigue kills retention. These let you tune frequency live.
    //
    // Usage:
    //   if (GameRemoteConfig.Instance.AdsEnabled &&
    //       levelsPlayed % GameRemoteConfig.Instance.InterstitialEveryNLevels == 0)
    //   { ShowInterstitial(); }

    public bool AdsEnabled => GetBool("adsEnabled", true);
    public int InterstitialEveryNLevels => GetInt("interstitialEveryN", 3);
    public int RewardedAdCooldownSeconds => GetInt("rewardedAdCooldown", 60);
    public int MaxAdsPerSession => GetInt("maxAdsPerSession", 10);
    public int NoAdsBeforeLevel => GetInt("noAdsBeforeLevel", 3);

    // ========================================================================
    //  CATEGORY 5: SALE / EVENT BANNERS
    // ========================================================================
    //
    // Your PURCHASE PRICES live in Economy Service (GoldPurchaseManager etc).
    // These control the VISUAL sale experience — banners, messages, timers.
    //
    // Why not put prices here? Because a price change requires server
    // validation (Economy Service). But showing a banner that says
    // "Diwali Sale! Best time to buy!" is just UI — perfect for Remote Config.
    //
    // Usage:
    //   if (GameRemoteConfig.Instance.SaleBannerActive)
    //   {
    //       saleBanner.SetActive(true);
    //       saleBannerText.text = GameRemoteConfig.Instance.SaleBannerMessage;
    //   }

    public bool SaleBannerActive => GetBool("saleBannerActive", false);
    public string SaleBannerMessage => GetString("saleBannerMsg", "");
    public string SaleBannerColorHex => GetString("saleBannerColor", "#FF6B00");
    public string SaleEndDateUTC => GetString("saleEndDateUTC", "");
    public string EventId => GetString("eventId", "");
    public string EventThemeColor => GetString("eventThemeColor", "");

    /// <summary>
    /// Check if a sale is currently within its time window.
    /// The sale must be both active AND before the end date.
    /// </summary>
    public bool IsSaleCurrentlyRunning()
    {
        if (!SaleBannerActive) return false;
        if (string.IsNullOrEmpty(SaleEndDateUTC)) return true; // No end date = always active

        if (DateTime.TryParse(SaleEndDateUTC, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime end))
        {
            return DateTime.UtcNow <= end;
        }
        return false;
    }

    /// <summary>Seconds until sale ends. For countdown timer UI.</summary>
    public double SaleSecondsRemaining()
    {
        if (string.IsNullOrEmpty(SaleEndDateUTC)) return 0;
        if (DateTime.TryParse(SaleEndDateUTC, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime end))
        {
            double remaining = (end - DateTime.UtcNow).TotalSeconds;
            return remaining > 0 ? remaining : 0;
        }
        return 0;
    }

    // ========================================================================
    //  CATEGORY 6: UI TEXT & MESSAGES
    // ========================================================================
    //
    // Found a typo in your welcome message? Fix it from dashboard.
    // Want to A/B test "Buy Now" vs "Get it Now"? Change it live.

    public string WelcomeMessage => GetString("welcomeMsg", "Welcome back!");
    public string MaintenanceMessage => GetString("maintenanceMsg", "We're updating the game. Back soon!");
    public string UpdatePromptMessage => GetString("updatePromptMsg", "A new version is available. Please update!");

    // ========================================================================
    //  CATEGORY 7: APP LIFECYCLE
    // ========================================================================
    //
    // MAINTENANCE MODE:
    //   Set maintenanceMode = true → every player sees a "maintenance" screen.
    //   Your servers are down for database migration? No one can play and
    //   get errors — they see a friendly message instead.
    //
    // FORCE UPDATE:
    //   Set minRequiredVersion = "2.0.0". Players on 1.x see "Please update".
    //   Critical for when you push a breaking server change.
    //
    // SERVER ENDPOINT:
    //   Your game talks to your backend server. If you need to migrate
    //   to a new server URL, change it here — no app update needed.
    //
    // Usage in your app startup:
    //   if (GameRemoteConfig.Instance.MaintenanceMode)
    //   { ShowMaintenanceScreen(); return; }
    //   if (GameRemoteConfig.Instance.NeedsForceUpdate())
    //   { ShowUpdateScreen(); return; }

    public bool MaintenanceMode => GetBool("maintenanceMode", false);
    public string MinRequiredVersion => GetString("minRequiredVersion", "1.0.0");
    public string UpdateURL => GetString("updateURL", "");
    public string ServerEndpointURL => GetString("serverURL", "");

    /// <summary>
    /// Compares current app version against minRequiredVersion.
    /// Returns true if the player MUST update.
    /// </summary>
    public bool NeedsForceUpdate()
    {
        string minVersion = MinRequiredVersion;
        if (string.IsNullOrEmpty(minVersion)) return false;

        try
        {
            var current = new Version(Application.version);
            var minimum = new Version(minVersion);
            return current < minimum;
        }
        catch
        {
            return false; // If version parsing fails, don't block the player
        }
    }

    // ========================================================================
    //  CATEGORY 8: MATCHMAKING (if your game has multiplayer)
    // ========================================================================

    public int MatchmakingTimeoutSeconds => GetInt("mmTimeout", 30);
    public int MaxPlayersPerMatch => GetInt("mmMaxPlayers", 10);
    public int MinPlayersToStart => GetInt("mmMinPlayers", 2);
    public int SkillRangeTolerance => GetInt("mmSkillRange", 200);
    public bool BotFillEnabled => GetBool("mmBotFill", true);

    // ========================================================================
    //  CATEGORY 9: TUTORIAL / ONBOARDING
    // ========================================================================

    public bool TutorialEnabled => GetBool("tutorialEnabled", true);
    public bool SkipTutorialAllowed => GetBool("skipTutorialAllowed", false);
    public int TutorialRewardGold => GetInt("tutorialRewardGold", 100);

    // ========================================================================
    //  CATEGORY 10: NOTIFICATION SETTINGS
    // ========================================================================

    public bool PushNotificationsEnabled => GetBool("pushNotifsEnabled", true);
    public int MaxNotificationsPerDay => GetInt("maxNotifsPerDay", 3);
    public int InactiveReminderDays => GetInt("inactiveReminderDays", 3);

    // ========================================================================
    // CLEANUP
    // ========================================================================

    private void OnDestroy()
    {
        // Safety unsubscribe in case fetch is still pending
        try { RemoteConfigService.Instance.FetchCompleted -= HandleFetchCompleted; }
        catch { /* RemoteConfigService may not exist during app quit */ }
    }
}


// ============================================================================
// ============================================================================
//
// INTEGRATION EXAMPLES — How to use alongside your existing scripts
//
// These are NOT new scripts you need to create. They show how your
// EXISTING code can read from RemoteConfig where appropriate.
//
// ============================================================================
// ============================================================================

/*
// ─────────────────────────────────────────────────────────────────
// EXAMPLE 1: Your reward ad system reads amounts from Remote Config
//            but uses CurrencyManager to actually grant them
// ─────────────────────────────────────────────────────────────────

public class RewardAdHandler : MonoBehaviour
{
    public async void OnAdWatchComplete()
    {
        // Amount comes from Remote Config (changeable from dashboard)
        int goldReward = GameRemoteConfig.Instance.AdWatchGoldReward;
        int gemReward = GameRemoteConfig.Instance.AdWatchGemReward;
        
        // Actual granting goes through your existing CurrencyManager
        // which talks to Unity Economy Service (server-validated)
        await CurrencyManager.Instance.AddGold(goldReward);
        await CurrencyManager.Instance.AddGems(gemReward);
    }
}


// ─────────────────────────────────────────────────────────────────
// EXAMPLE 2: Your enemy spawner reads balance values from RC
// ─────────────────────────────────────────────────────────────────

public class EnemySpawner : MonoBehaviour
{
    public void SpawnBoss(Transform spawnPoint)
    {
        GameObject boss = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);
        
        // These values are from Remote Config — tweak from dashboard
        // without pushing an app update
        var health = boss.GetComponent<HealthComponent>();
        health.SetMaxHealth(GameRemoteConfig.Instance.BossHealth);
        
        var combat = boss.GetComponent<CombatComponent>();
        combat.SetDamage(GameRemoteConfig.Instance.BossAttackDamage);
    }
    
    public void SpawnWave()
    {
        int count = GameRemoteConfig.Instance.MaxEnemiesPerWave;
        float speedMult = GameRemoteConfig.Instance.EnemySpeedMultiplier;
        // ... spawn enemies with these RC-driven values
    }
}


// ─────────────────────────────────────────────────────────────────
// EXAMPLE 3: Your app startup checks maintenance + force update
// ─────────────────────────────────────────────────────────────────

public class AppStartup : MonoBehaviour
{
    public async void Start()
    {
        // Step 1: Initialize Unity Services (you already do this)
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        // Step 2: Load currency balances (your existing flow)
        await CurrencyManager.Instance.LoadBalances();
        
        // Step 3: Fetch remote config (NEW — add this line)
        await GameRemoteConfig.Instance.FetchConfig();
        
        // Step 4: Check app lifecycle gates
        if (GameRemoteConfig.Instance.MaintenanceMode)
        {
            ShowMaintenanceScreen(GameRemoteConfig.Instance.MaintenanceMessage);
            return; // Don't load the game
        }
        
        if (GameRemoteConfig.Instance.NeedsForceUpdate())
        {
            ShowUpdateScreen(GameRemoteConfig.Instance.UpdateURL);
            return; // Don't load the game
        }
        
        // Step 5: Everything clear — load the game
        LoadMainMenu();
    }
}


// ─────────────────────────────────────────────────────────────────
// EXAMPLE 4: Sale banner reads from RC (prices stay in Economy)
// ─────────────────────────────────────────────────────────────────

public class SaleBannerUI : MonoBehaviour
{
    [SerializeField] private GameObject banner;
    [SerializeField] private TextMeshProUGUI bannerText;
    
    private void OnEnable()
    {
        GameRemoteConfig.OnConfigReady += UpdateBanner;
        if (GameRemoteConfig.Instance?.IsReady == true) UpdateBanner();
    }
    
    private void OnDisable()
    {
        GameRemoteConfig.OnConfigReady -= UpdateBanner;
    }
    
    private void UpdateBanner()
    {
        if (GameRemoteConfig.Instance.IsSaleCurrentlyRunning())
        {
            banner.SetActive(true);
            bannerText.text = GameRemoteConfig.Instance.SaleBannerMessage;
        }
        else
        {
            banner.SetActive(false);
        }
    }
}


// ─────────────────────────────────────────────────────────────────
// EXAMPLE 5: Feature flag gates a new mode
// ─────────────────────────────────────────────────────────────────

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button pvpButton;
    [SerializeField] private Button leaderboardButton;
    
    private void Start()
    {
        // These buttons appear/disappear based on Remote Config
        pvpButton.gameObject.SetActive(GameRemoteConfig.Instance.PvPEnabled);
        leaderboardButton.gameObject.SetActive(GameRemoteConfig.Instance.LeaderboardEnabled);
    }
}
*/