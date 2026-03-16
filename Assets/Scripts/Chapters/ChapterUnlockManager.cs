using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages chapter lock/unlock status.
/// - All data loaded exclusively from Unity Cloud Save (no local reads/writes).
/// - One shared Play button + one shared Locked overlay, toggled per visible chapter.
/// - Removes the material from the "LevelImage" child Image on unlocked chapters.
/// - Uses UniTask throughout.
/// - UI is hidden entirely until cloud data is loaded to prevent flash/glitch.
/// </summary>
public class ChapterUnlockManager : MonoBehaviour
{
    public static ChapterUnlockManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Shared Chapter UI (one object serves all chapters)")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject lockedStatusObject;

    [Header("Chapter Item GameObjects (assign in order, 0 = Chapter 1)")]
    [Tooltip("Drag each chapter carousel item here in order. " +
             "The script will find the child named 'LevelImage' on each.")]
    [SerializeField] private GameObject[] chapterItems;

    [Header("Default Unlocks (used only when cloud has no data yet)")]
    [SerializeField] private int[] defaultUnlockedChapters = { 0 };

    // ── Cloud ────────────────────────────────────────────────────────────────
    private const string CLOUD_KEY = "chapter_unlock_data";
    private const string LEVEL_IMAGE_NAME = "LevelImage";

    // ── Runtime State ────────────────────────────────────────────────────────
    private readonly Dictionary<int, bool> _unlockMap = new();
    private readonly Dictionary<int, Material> _savedMaterial = new(); // original materials per chapter
    private int _currentIndex;
    private bool _servicesReady;
    private bool _dataReady;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Raised after any unlock state changes. Args: (chapterIndex, isUnlocked)</summary>
    public static event Action<int, bool> OnChapterUnlockChanged;

    // ═════════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => ScrollCarouselEffect.currentLevelChange += OnCarouselChanged;
    private void OnDisable() => ScrollCarouselEffect.currentLevelChange -= OnCarouselChanged;

    private void Start()
    {
        CacheLevelImageMaterials();
        HideAllUI();
        InitAsync().Forget();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Material Cache
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Walks every chapter item, finds the child named "LevelImage", and caches
    /// its current material so we can restore it if a chapter gets re-locked.
    /// </summary>
    private void CacheLevelImageMaterials()
    {
        if (chapterItems == null) return;

        for (int i = 0; i < chapterItems.Length; i++)
        {
            if (chapterItems[i] == null) continue;

            var img = GetLevelImage(i);
            if (img != null)
                _savedMaterial[i] = img.material;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Initialisation
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTaskVoid InitAsync()
    {
        await InitServicesAsync();
        await LoadFromCloudAsync();

        _dataReady = true;

        // Apply material state to ALL chapters up front
        ApplyAllLevelImageStates();

        // Then refresh the shared UI for whichever chapter is currently visible
        RefreshUI(_currentIndex);
    }

    private async UniTask InitServicesAsync()
    {
        try
        {
            await UnityServices.InitializeAsync().AsUniTask();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync().AsUniTask();

            _servicesReady = true;
            Debug.Log($"[ChapterUnlockManager] Signed in: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            _servicesReady = false;
            Debug.LogError($"[ChapterUnlockManager] Service init failed: {e.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Load
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTask LoadFromCloudAsync()
    {
        if (!_servicesReady)
        {
            Debug.LogWarning("[ChapterUnlockManager] Services unavailable – unlock map is empty.");
            return;
        }

        try
        {
            var result = await CloudSaveService.Instance.Data.Player
                .LoadAsync(new HashSet<string> { CLOUD_KEY }).AsUniTask();

            if (result.TryGetValue(CLOUD_KEY, out var item))
            {
                var saveData = JsonUtility.FromJson<ChapterSaveData>(item.Value.GetAs<string>());
                _unlockMap.Clear();
                if (saveData?.entries != null)
                    foreach (var entry in saveData.entries)
                        _unlockMap[entry.chapterIndex] = entry.isUnlocked;

                Debug.Log($"[ChapterUnlockManager] Loaded {_unlockMap.Count} entries from cloud.");
            }
            else
            {
                ApplyDefaults();
                await SaveToCloudAsync();
                Debug.Log("[ChapterUnlockManager] No cloud data found – defaults applied and saved.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChapterUnlockManager] Cloud load error: {e.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Cloud – Save
    // ═════════════════════════════════════════════════════════════════════════

    private async UniTask SaveToCloudAsync()
    {
        if (!_servicesReady) return;

        try
        {
            var saveData = new ChapterSaveData();
            foreach (var kvp in _unlockMap)
                saveData.entries.Add(new ChapterEntry { chapterIndex = kvp.Key, isUnlocked = kvp.Value });

            var payload = new Dictionary<string, object>
            {
                { CLOUD_KEY, JsonUtility.ToJson(saveData) }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(payload).AsUniTask();
            Debug.Log("[ChapterUnlockManager] Saved to cloud.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChapterUnlockManager] Cloud save error: {e.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Returns whether <paramref name="chapterIndex"/> is unlocked.</summary>
    public bool IsUnlocked(int chapterIndex) =>
        _unlockMap.TryGetValue(chapterIndex, out bool v) && v;

    /// <summary>
    /// Set unlock state for a chapter. Persists to cloud automatically.
    /// Call this from gameplay (e.g. level complete, IAP unlock).
    ///     await ChapterUnlockManager.Instance.SetUnlock(chapterIndex: 2, unlocked: true);
    /// </summary>
    public async UniTask SetUnlock(int chapterIndex, bool unlocked)
    {
        if (_unlockMap.TryGetValue(chapterIndex, out bool current) && current == unlocked)
            return;

        _unlockMap[chapterIndex] = unlocked;

        // Update the LevelImage material for this specific chapter item
        if (_dataReady)
        {
            ApplyLevelImageState(chapterIndex, unlocked);

            if (chapterIndex == _currentIndex)
                RefreshUI(_currentIndex);
        }

        OnChapterUnlockChanged?.Invoke(chapterIndex, unlocked);
        await SaveToCloudAsync();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Carousel Callback
    // ═════════════════════════════════════════════════════════════════════════

    private void OnCarouselChanged(int oneBasedIndex)
    {
        _currentIndex = oneBasedIndex - 1;
        if (!_dataReady) return;
        RefreshUI(_currentIndex);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UI
    // ═════════════════════════════════════════════════════════════════════════

    private void HideAllUI()
    {
        if (playButton) playButton.SetActive(false);
        if (lockedStatusObject) lockedStatusObject.SetActive(false);
    }

    private void RefreshUI(int chapterIndex)
    {
        bool unlocked = IsUnlocked(chapterIndex);
        if (playButton) playButton.SetActive(unlocked);
        if (lockedStatusObject) lockedStatusObject.SetActive(!unlocked);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LevelImage Material
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Applies correct material state to every chapter item at once.</summary>
    private void ApplyAllLevelImageStates()
    {
        if (chapterItems == null) return;
        for (int i = 0; i < chapterItems.Length; i++)
            ApplyLevelImageState(i, IsUnlocked(i));
    }

    /// <summary>
    /// Unlocked  → sets LevelImage.material to null (uses default UI material, shows sprite clearly).
    /// Locked    → restores the cached material (e.g. greyscale / darkened shader).
    /// </summary>
    private void ApplyLevelImageState(int chapterIndex, bool unlocked)
    {
        var img = GetLevelImage(chapterIndex);
        if (img == null) return;

        if (unlocked)
        {
            img.material = null; // null = Unity's default UI material, removes any lock overlay shader
        }
        else
        {
            // Restore the original material that was on the image at startup
            if (_savedMaterial.TryGetValue(chapterIndex, out var mat))
                img.material = mat;
        }
    }

    /// <summary>Finds the Image component on the child named "LevelImage" for a given chapter index.</summary>
    private Image GetLevelImage(int chapterIndex)
    {
        if (chapterItems == null || chapterIndex >= chapterItems.Length) return null;
        var item = chapterItems[chapterIndex];
        if (item == null) return null;

        var child = item.transform.Find(LEVEL_IMAGE_NAME);
        if (child == null)
        {
            Debug.LogWarning($"[ChapterUnlockManager] '{LEVEL_IMAGE_NAME}' not found on chapter item {chapterIndex}.");
            return null;
        }

        return child.GetComponent<Image>();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private void ApplyDefaults()
    {
        _unlockMap.Clear();
        if (defaultUnlockedChapters != null)
            foreach (int i in defaultUnlockedChapters)
                _unlockMap[i] = true;  
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Serialization
    // ═════════════════════════════════════════════════════════════════════════

    [Serializable]
    private class ChapterSaveData { public List<ChapterEntry> entries = new(); }

    [Serializable]
    private class ChapterEntry { public int chapterIndex; public bool isUnlocked; }
}