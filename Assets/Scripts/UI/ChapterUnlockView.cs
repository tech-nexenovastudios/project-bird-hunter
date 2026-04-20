using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChapterUnlockView : MonoBehaviour
{
    [Header("Shared UI")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private Image powerImage;
    [SerializeField] private Material greyScaleMaterial;

    [Header("Chapter Items (0 = Chapter 1)")]
    [SerializeField] private GameObject[] chapterItems;
    private const string LEVEL_IMAGE_NAME = "LevelImage";

    [Header("Main Menu Background")]
    [SerializeField] private ChaptersConfig chaptersConfig;
    [SerializeField] private Image mainMenuBackgroundImage;

    // ─── Runtime ───
    private readonly Dictionary<int, Material> savedMaterials = new();
    private int currentCarouselIndex;
    private ChapterUnlockService unlockService;

    // ─── Lifecycle ───

    private void Start()
    {
        CacheLevelImageMaterials();
        unlockService = ServiceLocator.Get<ChapterUnlockService>();

        if (unlockService == null || !unlockService.IsReady)
        {
            Debug.LogError("[ChapterUnlockView] ChapterUnlockService not ready.");
            return;
        }

        ApplyAllLevelImageStates();
        RefreshPlayButton(currentCarouselIndex);
        ApplyMainMenuBackground();
    }

    private void OnEnable()
    {
        ScrollCarouselEffect.currentLevelChange += OnCarouselChanged;
        EventBus.Subscribe<ChapterUnlockedEvent>(OnChapterUnlocked);
        EventBus.Subscribe<HighestChapterChangedEvent>(OnHighestChanged);
    }

    private void OnDisable()
    {
        ScrollCarouselEffect.currentLevelChange -= OnCarouselChanged;
        EventBus.Unsubscribe<ChapterUnlockedEvent>(OnChapterUnlocked);
        EventBus.Unsubscribe<HighestChapterChangedEvent>(OnHighestChanged);
    }

    // ─── Event Handlers ───

    private void OnCarouselChanged(int oneBasedIndex)
    {
        currentCarouselIndex = oneBasedIndex - 1;
        RefreshPlayButton(currentCarouselIndex);
    }

    private void OnChapterUnlocked(ChapterUnlockedEvent evt)
    {
        ApplyLevelImageState(evt.chapterIndex, true);
        if (evt.chapterIndex == currentCarouselIndex)
            RefreshPlayButton(currentCarouselIndex);
    }

    private void OnHighestChanged(HighestChapterChangedEvent evt)
    {
        ApplyMainMenuBackground();
    }

    // ─── UI Updates ───

    private void RefreshPlayButton(int chapterIndex)
    {
        if (unlockService == null || playButton == null) return;

        bool unlocked = unlockService.IsUnlocked(chapterIndex);
        playButton.GetComponent<Button>().enabled = unlocked;

        Material mat = unlocked ? null : greyScaleMaterial;
        playButton.GetComponent<Image>().material = mat;
        if (powerImage != null) powerImage.material = mat;
    }

    private void CacheLevelImageMaterials()
    {
        if (chapterItems == null) return;
        for (int i = 0; i < chapterItems.Length; i++)
        {
            var img = GetLevelImage(i);
            if (img != null) savedMaterials[i] = img.material;
        }
    }

    private void ApplyAllLevelImageStates()
    {
        if (chapterItems == null || unlockService == null) return;
        for (int i = 0; i < chapterItems.Length; i++)
            ApplyLevelImageState(i, unlockService.IsUnlocked(i));
    }

    private void ApplyLevelImageState(int chapterIndex, bool unlocked)
    {
        var img = GetLevelImage(chapterIndex);
        if (img == null) return;

        img.material = unlocked
            ? null
            : (savedMaterials.TryGetValue(chapterIndex, out var mat) ? mat : null);
    }

    private Image GetLevelImage(int chapterIndex)
    {
        if (chapterItems == null || chapterIndex >= chapterItems.Length) return null;
        var item = chapterItems[chapterIndex];
        if (item == null) return null;
        var child = item.transform.Find(LEVEL_IMAGE_NAME);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private void ApplyMainMenuBackground()
    {
        if (mainMenuBackgroundImage == null || chaptersConfig == null || unlockService == null) return;

        ChapterData data = chaptersConfig.GetWorldData(unlockService.HighestUnlockedIndex);
        if (data != null && data.ChapterBackground != null)
            mainMenuBackgroundImage.sprite = data.ChapterBackground;
    }
}