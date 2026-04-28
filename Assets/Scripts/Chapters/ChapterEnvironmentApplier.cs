using System.Collections;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterEnvironmentApplier : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Scene References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private SpriteRenderer platformRenderer;

    [Header("Chapter Transition — References")]
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private Transform platformTransform;
    [SerializeField] private GameObject chapterUnlockPanel;
    [SerializeField] private TextMeshProUGUI chapterUnlockText;

    [Header("Chapter Transition — Timing")]
    [SerializeField] private float platformDropDuration = 0.4f;
    [SerializeField] private float backgroundSlideDuration = 1.2f;
    [SerializeField] private float platformRiseDuration = 0.6f;
    [SerializeField] private float cannonDropDuration = 0.7f;
    [SerializeField] private float panelHoldDuration = 1.5f;

    [Tooltip("How far below start position the platform drops before coming back.")]
    [SerializeField] private float platformDropDistance = 5f;

    [Tooltip("How far above the cannon's spawn Y it starts before dropping in.")]
    [SerializeField] private float cannonStartAboveDistance = 10f;

    [Header("Chapter Data Loading")]
    [SerializeField] private string chapterDataResourcesPath = "Chapters";

    // ── Static Chapter Reference ─────────────────────────────────────────────

    private static ChapterData _pendingChapterData;
    public static void SetChapterData(ChapterData data) => _pendingChapterData = data;
    public static ChapterData GetCurrentChapterData() => _pendingChapterData;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable() => GameEvents.OnChapterCompleted += PlayChapterTransition;
    private void OnDisable() => GameEvents.OnChapterCompleted -= PlayChapterTransition;

    private void Start()
    {
        if (chapterUnlockPanel != null)
            chapterUnlockPanel.SetActive(false);

        ApplyVisuals();
    }

    // ── Core Logic ───────────────────────────────────────────────────────────

    private void ApplyVisuals()
    {
        if (_pendingChapterData == null)
        {
            Debug.LogError("[ChapterEnv] No ChapterData was set before loading the gameplay scene!");
            return;
        }

        if (backgroundImage != null && _pendingChapterData.ChapterBackground != null)
            backgroundImage.sprite = _pendingChapterData.ChapterBackground;

        if (platformRenderer != null && _pendingChapterData.Platform != null)
            platformRenderer.sprite = _pendingChapterData.Platform;

        Debug.Log($"[ChapterEnv] Applied visuals for '{_pendingChapterData.worldName}'");
    }

    public void SwitchToChapter(ChapterData newChapterData)
    {
        _pendingChapterData = newChapterData;
        ApplyVisuals();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CHAPTER TRANSITION (animated)
    // ─────────────────────────────────────────────────────────────────────────

    private void PlayChapterTransition(int newChapterNumber)
    {
        ChapterData newData = LoadChapterDataForChapter(newChapterNumber);
        if (newData == null)
        {
            Debug.LogError($"[ChapterEnv] No ChapterData found for Chapter {newChapterNumber}.");
            return;
        }

        if (backgroundRect == null || platformTransform == null)
        {
            Debug.LogError("[ChapterEnv] Transition references missing in Inspector.");
            return;
        }

        StartCoroutine(TransitionRoutine(newData, newChapterNumber));
    }

    private IEnumerator TransitionRoutine(ChapterData newData, int chapterNum)
    {
        Debug.Log($"[ChapterEnv] ▶ Starting transition to Chapter {chapterNum}");

        // ── Cache references ────────────────────────────────────────────
        Vector2 bgStartPos = backgroundRect.anchoredPosition;
        Vector3 platformStartPos = platformTransform.position;
        float platformDropY = platformStartPos.y - platformDropDistance;

        // Cache cannon reference (if one exists)
        GameObject cannonGO = GameManager.Instance != null ? GameManager.Instance.currentCannon : null;
        Vector3 cannonStartPos = Vector3.zero;
        Vector3 cannonAbovePos = Vector3.zero;
        if (cannonGO != null)
        {
            cannonStartPos = cannonGO.transform.position;
            cannonAbovePos = cannonStartPos + new Vector3(0f, cannonStartAboveDistance, 0f);
        }

        // ── STEP 1: Drop platform down AND move cannon off-screen together ──
        platformTransform.DOMoveY(platformDropY, platformDropDuration)
                         .SetEase(Ease.InBack);

        if (cannonGO != null)
        {
            // Cannon rides the platform down too (since it stands on it)
            cannonGO.transform.DOMoveY(cannonStartPos.y - platformDropDistance, platformDropDuration)
                              .SetEase(Ease.InBack);
        }

        yield return new WaitForSeconds(platformDropDuration);

        // Move cannon off-screen completely (up top, ready to drop)
        if (cannonGO != null)
            cannonGO.transform.position = cannonAbovePos;

        // ── STEP 2: Show unlock panel ───────────────────────────────────
        if (chapterUnlockPanel != null)
        {
            chapterUnlockPanel.SetActive(true);
            if (chapterUnlockText != null)
                chapterUnlockText.text = $"Chapter : {chapterNum} Unlocked";
        }

        // ── STEP 3: Cross-slide backgrounds ─────────────────────────────
        float screenWidth = backgroundRect.rect.width;

        GameObject newBgGO = Instantiate(backgroundImage.gameObject, backgroundImage.transform.parent);
        Image newBgImage = newBgGO.GetComponent<Image>();
        RectTransform newBgRect = newBgGO.GetComponent<RectTransform>();

        newBgImage.sprite = newData.ChapterBackground;
        newBgRect.anchoredPosition = bgStartPos + new Vector2(screenWidth, 0f);

        backgroundRect.DOAnchorPosX(bgStartPos.x - screenWidth, backgroundSlideDuration)
                      .SetEase(Ease.InOutQuad);
        newBgRect.DOAnchorPosX(bgStartPos.x, backgroundSlideDuration)
                 .SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(backgroundSlideDuration);

        // ── STEP 4: Swap BG back to original Image ──────────────────────
        backgroundImage.sprite = newData.ChapterBackground;
        backgroundRect.anchoredPosition = bgStartPos;
        Destroy(newBgGO);

        _pendingChapterData = newData;

        // ── STEP 5: Swap platform sprite while off-screen ──────────────
        if (platformRenderer != null)
            platformRenderer.sprite = newData.Platform;

        // ── STEP 6: Slide platform back to EXACT original Y ─────────────
        // Ease.OutQuad — smooth settle, NO overshoot (was Ease.OutBack which overshot)
        platformTransform.DOMoveY(platformStartPos.y, platformRiseDuration)
                         .SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(platformRiseDuration);

        // ── STEP 7: Drop cannon from top to its spawn Y ─────────────────
        if (cannonGO != null)
        {
            cannonGO.transform.DOMoveY(cannonStartPos.y, cannonDropDuration)
                              .SetEase(Ease.OutBounce);  // bouncy landing
            yield return new WaitForSeconds(cannonDropDuration);
        }

        // ── STEP 8: Hold panel, then hide ───────────────────────────────
        yield return new WaitForSeconds(panelHoldDuration);

        if (chapterUnlockPanel != null)
            chapterUnlockPanel.SetActive(false);

        Debug.Log($"[ChapterEnv] ✅ Transition to Chapter {chapterNum} complete.");
    }

    // ─────────────────────────────────────────────────────────────────────────

    private ChapterData LoadChapterDataForChapter(int chapterNum)
    {
        ChapterData[] all = Resources.LoadAll<ChapterData>(chapterDataResourcesPath);
        string prefix = $"{chapterNum}-";

        foreach (ChapterData data in all)
            if (data.name.StartsWith(prefix))
                return data;

        Debug.LogError($"[ChapterEnv] No ChapterData starting with '{prefix}' in Resources/{chapterDataResourcesPath}/");
        return null;
    }

    private void OnDestroy()
    {
        if (backgroundRect != null) backgroundRect.DOKill();
        if (platformTransform != null) platformTransform.DOKill();

        GameObject cannonGO = GameManager.Instance != null ? GameManager.Instance.currentCannon : null;
        if (cannonGO != null) cannonGO.transform.DOKill();
    }

    [ContextMenu("Fire Chapter Completed Event")]
    private void FireTestEvent()
    {
        if (!Application.isPlaying) return;
        GameEvents.FireChapterCompleted(2);
    }
}