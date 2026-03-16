using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Newtonsoft.Json;  // Your requirement
using Constants;
using Cysharp.Threading.Tasks;
using UnityUtils;

public class PlayerDataLoader : Singleton<PlayerDataLoader>
{
    public static PlayerDataLoader Instance { get; private set; }
    
    public static bool IsNewPlayer { get; private set; }
    public static Dictionary<string, string> PlayerData { get; private set; } = new();  // FIXED: string values
    public static DateTime FirstLoginDate { get; private set; }
    public bool IsReturningPlayer { get; set; }
    public static int TotalLogins { get; private set; }
    
    public static Action<LoadResult> OnDataLoaded;
    public static Action OnNewPlayerReady;
    public static Action OnReturningPlayerReady;
    
    public static ChapterData ChapterData { get; private set; }
    
    [System.Serializable]
    public class LoadResult
    {
        public bool success;
        public bool isNewPlayer;
        public string message;
    }
    
    protected override void Awake()
    {
        DontDestroyOnLoad(this);
        base.Awake();
    }
    
    public async UniTask LoadAllPlayerDataAsync()
    {
        LoadResult result = new() { success = false };
        
        try
        {
            if (UnityServices.State < ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }
            
            // 2. Auth
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            
            var keys = new HashSet<string>
            {
                GameConstants.PLAYER_LOGIN_COUNT_KEY,
                GameConstants.PLAYER_USERNAME_KEY,
                GameConstants.PLAYER_EMAIL_KEY,
                GameConstants.PLAYER_GOLD_COIN_KEY,
                GameConstants.PLAYER_GEM_COIN_KEY,
                GameConstants.PLAYER_POWER_COIN_KEY,
                GameConstants.PLAYER_CHAPTER_COMPLETED_KEY,
                GameConstants.PLAYER_TOTAL_TIME_PLAYED_KEY,
                GameConstants.PLAYER_CURRENT_CHAPTER_KEY,
                GameConstants.PLAYER_CURRENT_LEVEL_KEY,
                GameConstants.PLAYER_EGGS_DESTROYED_KEY,
                GameConstants.PLAYER_HIGH_SCORE_KEY,
                GameConstants.PLAYER_TOTAL_SCORE_KEY,
                GameConstants.PLAYER_SLOT_POWERUP_IDS_KEY,
                GameConstants.PLAYER_GLOBAL_UNLOCKED_KEY,
                GameConstants.PLAYER_XP_KEY,
                GameConstants.PLAYER_LEVEL_KEY,
                GameConstants.PLAYER_LAST_LEVEL_UP_XP_KEY,
                GameConstants.PLAYER_SPINS_KEY,
                GameConstants.PLAYER_DAILY_QUEST_KEY,
                GameConstants.PLAYER_QUEST_DATA_KEY,
                GameConstants.PLAYER_QUEST_COMPLETIONS_KEY
            };
            
            var loadedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            
            PlayerData.Clear();
            
            foreach (var kvp in loadedData)
            {
                PlayerData[kvp.Key] = kvp.Value.Value.GetAs<string>() ?? "";
            }
            
            result.success = true;
            
            if (!PlayerData.ContainsKey(GameConstants.PLAYER_LOGIN_COUNT_KEY) || 
                string.IsNullOrEmpty(PlayerData[GameConstants.PLAYER_LOGIN_COUNT_KEY]))
            {
                IsNewPlayer = true;
                await InitializeNewPlayer();
                result.isNewPlayer = true;
                result.message = "New player initialized";
                OnNewPlayerReady?.Invoke();
            }
            else
            {
                IsReturningPlayer = true;
                ParseReturningPlayerData();
                result.isNewPlayer = false;
                result.message = $"Returning player loaded (Login #{TotalLogins})";
                OnReturningPlayerReady?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Player data load failed: {e.Message}");
            result.message = e.Message;
            IsNewPlayer = true;
        }
        finally
        {
            OnDataLoaded?.Invoke(result);
        }
    }
    
    private async Task InitializeNewPlayer()
    {
        TotalLogins = 1;
        
        var defaultData = new Dictionary<string, object>
        {
            { GameConstants.PLAYER_LOGIN_COUNT_KEY, 1 },
            { GameConstants.PLAYER_USERNAME_KEY, "Player" },
            { GameConstants.PLAYER_EMAIL_KEY, "" },
            { GameConstants.PLAYER_GOLD_COIN_KEY, "1000" },
            { GameConstants.PLAYER_GEM_COIN_KEY, "10" },
            { GameConstants.PLAYER_POWER_COIN_KEY, "20" },
            { GameConstants.PLAYER_CHAPTER_COMPLETED_KEY, "0" },
            { GameConstants.PLAYER_TOTAL_TIME_PLAYED_KEY, "0" },
            { GameConstants.PLAYER_CURRENT_CHAPTER_KEY, "1" },
            { GameConstants.PLAYER_CURRENT_LEVEL_KEY, "1" },
            { GameConstants.PLAYER_EGGS_DESTROYED_KEY, "0" },
            { GameConstants.PLAYER_HIGH_SCORE_KEY, "0" },
            { GameConstants.PLAYER_TOTAL_SCORE_KEY, "0" },
            { GameConstants.PLAYER_SLOT_POWERUP_IDS_KEY, "[]" },
            { GameConstants.PLAYER_GLOBAL_UNLOCKED_KEY, "[]" },
            { GameConstants.PLAYER_XP_KEY, "0" },
            { GameConstants.PLAYER_LEVEL_KEY, "1" },
            { GameConstants.PLAYER_LAST_LEVEL_UP_XP_KEY, "0" },
            { GameConstants.PLAYER_SPINS_KEY, "1" },
            { GameConstants.PLAYER_DAILY_QUEST_KEY, "[]" },
            { GameConstants.PLAYER_QUEST_DATA_KEY, "{}" },
            { GameConstants.PLAYER_QUEST_COMPLETIONS_KEY, "[]" }
        };
        
        // FIXED: SaveAsync accepts Dictionary<string, object> [web:252]
        await CloudSaveService.Instance.Data.Player.SaveAsync(defaultData);
        
        // Populate local cache
        foreach (var kvp in defaultData)
        {
            PlayerData[kvp.Key] = kvp.Value.ToString();
        }
    }
    
    private void ParseReturningPlayerData()
    {
        TotalLogins = int.Parse(PlayerData[GameConstants.PLAYER_LOGIN_COUNT_KEY]) + 1;
    }
    
    // FIXED: Type-safe getters using GetAs<T>() [web:262]
    public static int GetInt(string key, int defaultValue = 0)
    {
        if (PlayerData.TryGetValue(key, out var valueStr) && int.TryParse(valueStr, out int result))
        {
            return result;
        }
        return defaultValue;
    }
    
    public static long GetLong(string key, long defaultValue = 0L)
    {
        if (PlayerData.TryGetValue(key, out var valueStr) && long.TryParse(valueStr, out long result))
        {
            return result;
        }
        return defaultValue;
    }
    
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (PlayerData.TryGetValue(key, out var valueStr) && float.TryParse(valueStr, out float result))
        {
            return result;
        }
        return defaultValue;
    }
    
    public static string GetString(string key, string defaultValue = "")
    {
        return PlayerData.TryGetValue(key, out var value) ? value : defaultValue;
    }
    
    // NewtonSoft JSON helpers
    public static T GetJsonObject<T>(string key) where T : class
    {
        var json = GetString(key, "{}");
        return JsonConvert.DeserializeObject<T>(json);
    }
    
    // Save single key (string only)
    public static async Task SaveKeyAsync(string key, object value)
    {
        var data = new Dictionary<string, object> { { key, value.ToString() } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        PlayerData[key] = value.ToString();
    }
    
    [ContextMenu("Reload All Data")]
    public async void ReloadData()
    {
        await LoadAllPlayerDataAsync();
    }
}
