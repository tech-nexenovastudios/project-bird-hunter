using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.Managers;
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
/// - Supports unlocking ALL chapters via Inspector toggle or public API.
/// - Tracks the highest unlocked chapter and updates the main menu background
///   to match that chapter's visual theme.
/// </summary>
public class ChapterUnlockManager : MonoBehaviour
{
    public static ChapterUnlockManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Shared Chapter UI (one object serves all chapters)")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private Image powerImage;
    //[SerializeField] private GameObject lockedStatusObject;
    [SerializeField] private Material greyScaleMaterial;

    [Header("Chapter Item GameObjects (assign in order, 0 = Chapter 1)")]
    [Tooltip("Drag each chapter carousel item here in order. " +
             "The script will find the child named 'LevelImage' on each.")]
    [SerializeField] private GameObject[] chapterItems;

    [Header("Default Unlocks (used only when cloud has no data yet)")]
    [SerializeField] private int[] defaultUnlockedChapters = { 0 };

    [Header("Dev / QA Override")]
    [Tooltip("Tick this to unlock ALL chapters instantly (overrides cloud data). " +
             "Untick to restore normal cloud-save behaviour. " +
             "NEVER ship to production with this enabled.")]
    [SerializeField] private bool devUnlockAll = true;

    [Header("Main Menu Background")]
    [Tooltip("The ChaptersConfig asset that holds all ChapterData references. " +
             "Used to pull the background sprite for the latest unlocked chapter.")]
    [SerializeField] private ChaptersConfig chaptersConfig;

    [Tooltip("The Image component on the main menu background GameObject. " +
             "Its sprite will change to match the highest unlocked chapter.")]
    [SerializeField] private Image mainMenuBackgroundImage;

    // ── Cloud ────────────────────────────────────────────────────────────────
    private const string CLOUD_KEY = "chapter_unlock_data";
    private const string LAST_UNLOCKED_KEY = "last_unlocked_chapter";
    private const string LEVEL_IMAGE_NAME = "LevelImage";

    // ── Runtime State ────────────────────────────────────────────────────────
    private readonly Dictionary<int, bool> _unlockMap = new();
    private readonly Dictionary<int, Material> _savedMaterial = new();
    private int _currentIndex;
    private int _lastUnlockedChapter = 0;
    private bool _servicesReady;
    private bool _dataReady;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Raised after any unlock state changes. Args: (chapterIndex, isUnlocked)</summary>
    public static event Action<int, bool> OnChapterUnlockChanged;

    /// <summary>
    /// Raised when the highest unlocked chapter changes.
    /// Args: (newHighestChapterIndex)
    /// Subscribe from any script that needs to react to progression changes.
    /// </summary>
    public static event Action<int> OnLastUnlockedChapterChanged;

    // ── Public Read-Only ─────────────────────────────────────────────────────

    /// <summary>
    /// The index of the highest chapter the player has unlocked (0-based).
    /// Updated automatically whenever a new chapter is unlocked.
    /// </summary>
    public int LastUnlockedChapter => _lastUnlockedChapter;

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
        BindPlayButtonChapterCommit();
        InitAsync().Forget();
    }

    // Play button's scene-load is wired via UnityEvent in the inspector, but
    // nothing tells GameProgressManager which chapter the carousel is on. Without
    // this, gameplay always reads whatever _progress.currentChapter was loaded from
    // cloud — so picking a different chapter from the carousel silently runs the
    // previous one. We add a runtime listener to commit the carousel index before
    // the scene change fires.
    private void BindPlayButtonChapterCommit()
    {
        if (playButton == null) return;
        var btn = playButton.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.SelectChapter(_currentIndex + 1);
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Material Cache
    // ═════════════════════════════════════════════════════════════════════════

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

        // ── Dev override: skip cloud entirely ─────────────────────────────
        if (devUnlockAll)
        {
            ApplyUnlockAll();
            Debug.LogWarning("[ChapterUnlockManager] DEV MODE: All chapters unlocked. " +
                             "Cloud data was NOT loaded or written.");
        }
        else
        {
            await LoadFromCloudAsync();
        }

        _dataReady = true;

        // ── Recalculate highest unlocked chapter from the loaded data ─────
        RecalculateLastUnlocked();

        ApplyAllLevelImageStates();
        RefreshUI(_currentIndex);
        ApplyMainMenuBackground();
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
            // Load both keys in a single batch call for efficiency
            var keys = new HashSet<string> { CLOUD_KEY, LAST_UNLOCKED_KEY };
            var result = await CloudSaveService.Instance.Data.Player
                .LoadAsync(keys).AsUniTask();

            // ── Unlock map ────────────────────────────────────────────────
            if (result.TryGetValue(CLOUD_KEY, out var unlockItem))
            {
                var saveData = JsonUtility.FromJson<ChapterSaveData>(
                    unlockItem.Value.GetAs<string>());
                _unlockMap.Clear();
                if (saveData?.entries != null)
                    foreach (var entry in saveData.entries)
                        _unlockMap[entry.chapterIndex] = entry.isUnlocked;

                Debug.Log($"[ChapterUnlockManager] Loaded {_unlockMap.Count} entries from cloud.");
            }
            else
            {
                ApplyDefaults();
                Debug.Log("[ChapterUnlockManager] No cloud data found – defaults applied.");
            }

            // ── Last unlocked chapter ─────────────────────────────────────
            if (result.TryGetValue(LAST_UNLOCKED_KEY, out var lastItem))
            {
                _lastUnlockedChapter = lastItem.Value.GetAs<int>();
                Debug.Log($"[ChapterUnlockManager] Last unlocked chapter from cloud: {_lastUnlockedChapter}");
            }
            else
            {
                // First time — will be recalculated from unlock map
                _lastUnlockedChapter = 0;
            }

            // Save defaults if this was the first load
            if (!result.ContainsKey(CLOUD_KEY))
                await SaveToCloudAsync();
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
                saveData.entries.Add(new ChapterEntry
                {
                    chapterIndex = kvp.Key,
                    isUnlocked = kvp.Value
                });

            // Save both unlock map and last unlocked chapter in one batch
            var payload = new Dictionary<string, object>
            {
                { CLOUD_KEY, JsonUtility.ToJson(saveData) },
                { LAST_UNLOCKED_KEY, _lastUnlockedChapter }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(payload).AsUniTask();
            Debug.Log($"[ChapterUnlockManager] Saved to cloud. Last unlocked: {_lastUnlockedChapter}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChapterUnlockManager] Cloud save error: {e.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Last Unlocked Chapter — Tracking & Background
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scans the entire unlock map and finds the highest unlocked index.
    /// Called once after loading cloud data, and after every unlock change.
    /// </summary>
    private void RecalculateLastUnlocked()
    {
        int highest = 0;
        foreach (var kvp in _unlockMap)
        {
            if (kvp.Value && kvp.Key > highest)
                highest = kvp.Key;
        }

        bool changed = highest != _lastUnlockedChapter;
        _lastUnlockedChapter = highest;

        if (changed)
        {
            OnLastUnlockedChapterChanged?.Invoke(_lastUnlockedChapter);
            Debug.Log($"[ChapterUnlockManager] Highest unlocked chapter updated: {_lastUnlockedChapter}");
        }
    }

    /// <summary>
    /// Applies the background sprite from the highest unlocked chapter's
    /// ChapterData to the main menu background Image.
    /// Safe to call at any time — silently does nothing if references are missing.
    /// </summary>
    private void ApplyMainMenuBackground()
    {
        if (mainMenuBackgroundImage == null)
        {
            // Not assigned — might be in a scene without a menu background.
            // This is normal when the manager persists into the gameplay scene.
            return;
        }

        if (chaptersConfig == null)
        {
            Debug.LogWarning("[ChapterUnlockManager] ChaptersConfig is not assigned. " +
                             "Cannot update main menu background.");
            return;
        }

        ChapterData data = chaptersConfig.GetWorldData(_lastUnlockedChapter);
        if (data == null)
        {
            Debug.LogWarning($"[ChapterUnlockManager] No ChapterData found for " +
                             $"chapter index {_lastUnlockedChapter}.");
            return;
        }

        Sprite bg = data.ChapterBackground;
        if (bg != null)
        {
            mainMenuBackgroundImage.sprite = bg;
            Debug.Log($"[ChapterUnlockManager] Main menu BG set to " +
                      $"'{data.worldName}' (chapter {_lastUnlockedChapter + 1})");
        }
        else
        {
            Debug.LogWarning($"[ChapterUnlockManager] ChapterData '{data.worldName}' " +
                             "has no background sprite assigned.");
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
    /// If this chapter is higher than the previous highest, updates the
    /// main menu background and saves the new value to cloud.
    /// </summary>
    public async UniTask SetUnlock(int chapterIndex, bool unlocked)
    {
        if (_unlockMap.TryGetValue(chapterIndex, out bool current) && current == unlocked)
            return;

        _unlockMap[chapterIndex] = unlocked;

        if (_dataReady)
        {
            ApplyLevelImageState(chapterIndex, unlocked);
            if (chapterIndex == _currentIndex)
                RefreshUI(_currentIndex);
        }

        OnChapterUnlockChanged?.Invoke(chapterIndex, unlocked);

        // ── Check if this is a new highest unlock ─────────────────────────
        if (unlocked)
        {
            int previousHighest = _lastUnlockedChapter;
            RecalculateLastUnlocked();

            if (_lastUnlockedChapter != previousHighest)
                ApplyMainMenuBackground();
        }

        // Skip cloud write when running in dev-unlock-all mode
        if (!devUnlockAll)
            await SaveToCloudAsync();
    }

    /// <summary>
    /// Unlocks every chapter registered in <see cref="chapterItems"/> at runtime.
    /// Saves the result to cloud (skipped in devUnlockAll mode).
    /// Safe to call from a UI button, cheat menu, or game event.
    /// </summary>
    public async UniTask UnlockAllChapters()
    {
        if (chapterItems == null) return;

        for (int i = 0; i < chapterItems.Length; i++)
            _unlockMap[i] = true;

        ApplyAllLevelImageStates();
        RefreshUI(_currentIndex);

        // Raise events for every chapter so other systems stay in sync
        for (int i = 0; i < chapterItems.Length; i++)
            OnChapterUnlockChanged?.Invoke(i, true);

        // Update last unlocked to the final chapter
        int previousHighest = _lastUnlockedChapter;
        RecalculateLastUnlocked();

        if (_lastUnlockedChapter != previousHighest)
            ApplyMainMenuBackground();

        if (!devUnlockAll)
            await SaveToCloudAsync();

        Debug.Log("[ChapterUnlockManager] All chapters unlocked.");
    }

    /// <summary>
    /// Re-applies the main menu background based on current data.
    /// Call this when returning to the main menu scene if the background
    /// Image reference needs to be reassigned (e.g. after scene reload).
    /// </summary>
    public void RefreshMainMenuBackground(Image newBackgroundImage)
    {
        mainMenuBackgroundImage = newBackgroundImage;
        ApplyMainMenuBackground();
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
        if (playButton)
        {
            playButton.GetComponent<Button>().enabled = false;
            playButton.GetComponent<Image>().material = greyScaleMaterial;
            powerImage.GetComponent<Image>().material = greyScaleMaterial;
        }
        
        //if (lockedStatusObject) lockedStatusObject.SetActive(false);
    }

    private void RefreshUI(int chapterIndex)
    {
        bool unlocked = IsUnlocked(chapterIndex);
        if (playButton)
        {
                playButton.GetComponent<Button>().enabled = unlocked;
            if (!unlocked)
            {
                playButton.GetComponent<Image>().material = greyScaleMaterial;
                powerImage.GetComponent<Image>().material = greyScaleMaterial;
            }
            else // if chapter is unlocked
            {
                playButton.GetComponent<Image>().material = null;
                powerImage.GetComponent<Image>().material = null;
            }
            
        }
        //if (lockedStatusObject) lockedStatusObject.SetActive(!unlocked);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LevelImage Material
    // ═════════════════════════════════════════════════════════════════════════

    private void ApplyAllLevelImageStates()
    {
        if (chapterItems == null) return;
        for (int i = 0; i < chapterItems.Length; i++)
            ApplyLevelImageState(i, IsUnlocked(i));
    }

    private void ApplyLevelImageState(int chapterIndex, bool unlocked)
    {
        var img = GetLevelImage(chapterIndex);
        if (img == null) return;

        img.material = unlocked
            ? null
            : (_savedMaterial.TryGetValue(chapterIndex, out var mat) ? mat : null);
    }

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

    /// <summary>Fills _unlockMap with true for every chapter slot without touching cloud.</summary>
    private void ApplyUnlockAll()
    {
        _unlockMap.Clear();
        if (chapterItems == null) return;
        for (int i = 0; i < chapterItems.Length; i++)
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