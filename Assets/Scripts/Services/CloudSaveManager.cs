using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

public interface ICloudSaveManager
{
    // Core Operations
    UniTask SaveAsync(Dictionary<string, object> data);
    UniTask<Dictionary<string, Item>> LoadAsync(HashSet<string> keys);
    UniTask DeleteAsync(string key);
    UniTask ListKeys();

    // Typed Helpers
    UniTask SaveValueAsync(string key, object value);
    UniTask<T> LoadValueAsync<T>(string key, T defaultValue = default);
    UniTask<bool> HasKeyAsync(string key);

    // Cache
    void ClearCache();
}

public class CloudSaveManager : ICloudSaveManager
{
    private static CloudSaveManager _instance;
    public static CloudSaveManager Instance => _instance ??= new CloudSaveManager();

    // Local cache to reduce API calls
    private readonly Dictionary<string, object> _cache = new();
    private bool _isInitialized;

    private const int MAX_RETRIES = 3;
    private const float RETRY_DELAY = 1f;

    // ==================== Initialization ====================

    private async UniTask EnsureInitialized()
    {
        if (_isInitialized) return;

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("[CloudSave] Unity Services Initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSave] Unity Services Initialization failed: {ex.Message}");
                throw;
            }
        }

        _isInitialized = true;
    }

    // ==================== Core Operations ====================

    public async UniTask SaveAsync(Dictionary<string, object> data)
    {
        await EnsureInitialized();

        await RetryAsync(async () =>
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            // Update cache
            foreach (var kvp in data)
                _cache[kvp.Key] = kvp.Value;

            Debug.Log($"[CloudSave] Saved {data.Count} key(s) successfully.");
        });
    }

    public async UniTask<Dictionary<string, Item>> LoadAsync(HashSet<string> keys)
    {
        await EnsureInitialized();

        try
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            // Update cache with loaded data
            foreach (var kvp in result)
                _cache[kvp.Key] = kvp.Value.Value;

            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] Load failed: {ex.Message}");
            return new Dictionary<string, Item>();
        }
    }

    public async UniTask DeleteAsync(string key)
    {
        await EnsureInitialized();

        await RetryAsync(async () =>
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
            _cache.Remove(key);
            Debug.Log($"[CloudSave] Deleted key: {key}");
        });
    }

    public async UniTask ListKeys()
    {
        await EnsureInitialized();

        try
        {
            var keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();
            foreach (var keyItem in keys)
                Debug.Log($"[CloudSave Key]: {keyItem.Key}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSave] List Keys failed: {ex.Message}");
        }
    }

    // ==================== Typed Helpers ====================

    /// <summary>Save a single key-value pair.</summary>
    public async UniTask SaveValueAsync(string key, object value)
    {
        var data = new Dictionary<string, object> { { key, value } };
        await SaveAsync(data);
    }

    /// <summary>Load a single value by key. Returns defaultValue if not found.</summary>
    public async UniTask<T> LoadValueAsync<T>(string key, T defaultValue = default)
    {
        // Check cache first
        if (_cache.TryGetValue(key, out var cached))
        {
            try
            {
                return ConvertValue<T>(cached);
            }
            catch
            {
                // Cache had bad data, fall through to cloud
            }
        }

        var result = await LoadAsync(new HashSet<string> { key });

        if (result.TryGetValue(key, out var item))
        {
            try
            {
                return ConvertValue<T>(item.Value.GetAs<T>());
            }
            catch
            {
                // If direct GetAs fails, try JSON deserialization
                try
                {
                    string json = JsonConvert.SerializeObject(item.Value.GetAsString());
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CloudSave] Failed to deserialize key '{key}': {ex.Message}");
                    return defaultValue;
                }
            }
        }

        return defaultValue;
    }

    /// <summary>Check if a key exists in cloud save.</summary>
    public async UniTask<bool> HasKeyAsync(string key)
    {
        if (_cache.ContainsKey(key)) return true;

        var result = await LoadAsync(new HashSet<string> { key });
        return result.ContainsKey(key);
    }

    // ==================== Batch Helpers ====================

    /// <summary>Save multiple key-value pairs at once.</summary>
    public async UniTask SaveBatchAsync(params (string key, object value)[] pairs)
    {
        var data = new Dictionary<string, object>();
        foreach (var (key, value) in pairs)
            data[key] = value;

        await SaveAsync(data);
    }

    /// <summary>Delete multiple keys at once.</summary>
    public async UniTask DeleteBatchAsync(params string[] keys)
    {
        await EnsureInitialized();

        foreach (var key in keys)
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
                _cache.Remove(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSave] Delete failed for key '{key}': {ex.Message}");
            }
        }
    }

    // ==================== Cache ====================

    public void ClearCache()
    {
        _cache.Clear();
        Debug.Log("[CloudSave] Cache cleared.");
    }

    // ==================== Internal Utilities ====================

    private T ConvertValue<T>(object value)
    {
        if (value is T typed)
            return typed;

        // Handle numeric conversions
        if (typeof(T) == typeof(int) && value is long l)
            return (T)(object)(int)l;

        if (typeof(T) == typeof(float) && value is double d)
            return (T)(object)(float)d;

        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }

    private async UniTask RetryAsync(Func<UniTask> action)
    {
        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt == MAX_RETRIES)
                {
                    Debug.LogError($"[CloudSave] Operation failed after {MAX_RETRIES} attempts: {ex.Message}");
                    throw;
                }

                Debug.LogWarning($"[CloudSave] Attempt {attempt} failed, retrying in {RETRY_DELAY}s...");
                await UniTask.Delay(TimeSpan.FromSeconds(RETRY_DELAY));
            }
        }
    }
}