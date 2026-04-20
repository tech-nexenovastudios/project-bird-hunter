using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayProgression : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private SpriteRenderer platformRenderer;

    [Header("Chapter Transition — References")]
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private Transform platformTransform;
    [SerializeField] private GameObject chapterUnlockPanel;
    [SerializeField] private TextMeshProUGUI chapterUnlockText;

    [Header("Chapter Transition — Timing")]
    [SerializeField] private TransitionSettings _transition = new()
    {
        platformDropDuration    = 0.4f,
        backgroundSlideDuration = 1.2f,
        platformRiseDuration    = 0.6f,
        cannonDropDuration      = 0.7f,
        panelHoldDuration       = 1.5f,
        platformDropDistance    = 5f,
        cannonStartAboveDistance = 10f,
    };

    [Header("Chapter Data Loading")]
    [SerializeField] private string chapterDataResourcesPath = "Chapters";

    [System.Serializable]
    private struct TransitionSettings
    {
        public float platformDropDuration;
        public float backgroundSlideDuration;
        public float platformRiseDuration;
        public float cannonDropDuration;
        public float panelHoldDuration;
        public float platformDropDistance;
        public float cannonStartAboveDistance;
    }

    private static ChapterData _pendingChapterData;
    public static void SetChapterData(ChapterData data) => _pendingChapterData = data;
    public static ChapterData GetCurrentChapterData() => _pendingChapterData;

    private ChapterData[] _allChapterData;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _allChapterData = Resources.LoadAll<ChapterData>(chapterDataResourcesPath);
    }

    private void OnEnable()  => GameEvents.OnChapterCompleted += PlayChapterTransition;
    private void OnDisable() => GameEvents.OnChapterCompleted -= PlayChapterTransition;

    private void Start()
    {
        if (chapterUnlockPanel != null)
            chapterUnlockPanel.SetActive(false);

        ApplyVisuals();
    }

    private void OnDestroy()
    {
        backgroundRect?.DOKill();
        platformTransform?.DOKill();
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void ApplyVisuals()
    {
        if (_pendingChapterData == null)
        {
            Debug.LogError("[ChapterEnv] No ChapterData set before loading gameplay scene.");
            return;
        }

        if (backgroundImage != null && _pendingChapterData.ChapterBackground != null)
            backgroundImage.sprite = _pendingChapterData.ChapterBackground;

        if (platformRenderer != null && _pendingChapterData.Platform != null)
            platformRenderer.sprite = _pendingChapterData.Platform;
    }

    public void SwitchToChapter(ChapterData newChapterData)
    {
        _pendingChapterData = newChapterData;
        ApplyVisuals();
    }

    // ── Chapter Transition ────────────────────────────────────────────────────

    private void PlayChapterTransition(int newChapterNumber)
    {
        ChapterData newData = FindChapterData(newChapterNumber);
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

        TransitionAsync(newData, newChapterNumber).Forget();
    }

    private async UniTaskVoid TransitionAsync(ChapterData newData, int chapterNum)
    {
        Vector2 bgStartPos          = backgroundRect.anchoredPosition;
        Vector3 platformStartPos    = platformTransform.position;
        float   platformDropY       = platformStartPos.y - _transition.platformDropDistance;

        GameObject cannon     = GameManager.Instance?.currentCannon;
        Vector3 cannonStart   = cannon != null ? cannon.transform.position : Vector3.zero;
        Vector3 cannonAbove   = cannonStart + new Vector3(0f, _transition.cannonStartAboveDistance, 0f);

        // Drop platform + cannon together
        platformTransform.DOMoveY(platformDropY, _transition.platformDropDuration).SetEase(Ease.InBack);
        cannon?.transform.DOMoveY(cannonStart.y - _transition.platformDropDistance, _transition.platformDropDuration).SetEase(Ease.InBack);
        await UniTask.Delay(Mathf.RoundToInt(_transition.platformDropDuration * 1000));

        if (cannon != null)
            cannon.transform.position = cannonAbove;

        // Show unlock panel
        if (chapterUnlockPanel != null)
        {
            chapterUnlockPanel.SetActive(true);
            if (chapterUnlockText != null)
                chapterUnlockText.text = $"Chapter {chapterNum} Unlocked";
        }

        // Cross-slide backgrounds
        float screenWidth   = backgroundRect.rect.width;
        GameObject newBgGO  = Instantiate(backgroundImage.gameObject, backgroundImage.transform.parent);
        var newBgImage      = newBgGO.GetComponent<Image>();
        var newBgRect       = newBgGO.GetComponent<RectTransform>();

        newBgImage.sprite               = newData.ChapterBackground;
        newBgRect.anchoredPosition      = bgStartPos + new Vector2(screenWidth, 0f);

        backgroundRect.DOAnchorPosX(bgStartPos.x - screenWidth, _transition.backgroundSlideDuration).SetEase(Ease.InOutQuad);
        newBgRect.DOAnchorPosX(bgStartPos.x, _transition.backgroundSlideDuration).SetEase(Ease.InOutQuad);
        await UniTask.Delay(Mathf.RoundToInt(_transition.backgroundSlideDuration * 1000));

        backgroundImage.sprite          = newData.ChapterBackground;
        backgroundRect.anchoredPosition = bgStartPos;
        Destroy(newBgGO);

        _pendingChapterData = newData;

        if (platformRenderer != null)
            platformRenderer.sprite = newData.Platform;

        // Rise platform
        platformTransform.DOMoveY(platformStartPos.y, _transition.platformRiseDuration).SetEase(Ease.OutQuad);
        await UniTask.Delay(Mathf.RoundToInt(_transition.platformRiseDuration * 1000));

        // Drop cannon
        if (cannon != null)
        {
            cannon.transform.DOMoveY(cannonStart.y, _transition.cannonDropDuration).SetEase(Ease.OutBounce);
            await UniTask.Delay(Mathf.RoundToInt(_transition.cannonDropDuration * 1000));
        }

        await UniTask.Delay(Mathf.RoundToInt(_transition.panelHoldDuration * 1000));

        if (chapterUnlockPanel != null)
            chapterUnlockPanel.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ChapterData FindChapterData(int chapterNum)
    {
        string prefix = $"{chapterNum}-";
        foreach (ChapterData data in _allChapterData)
            if (data.name.StartsWith(prefix))
                return data;

        Debug.LogError($"[ChapterEnv] No ChapterData starting with '{prefix}' in Resources/{chapterDataResourcesPath}/");
        return null;
    }

    [ContextMenu("Fire Chapter Completed Event")]
    private void FireTestEvent()
    {
        if (!Application.isPlaying) return;
        GameEvents.FireChapterCompleted(2);
    }
}
