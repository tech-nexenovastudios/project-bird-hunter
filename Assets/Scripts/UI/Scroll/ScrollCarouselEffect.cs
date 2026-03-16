//using DG.Tweening;
//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class ScrollCarouselEffect : MonoBehaviour,
//    IPointerDownHandler, IDragHandler, IEndDragHandler
//{
//    [Header("References")]
//    [SerializeField] RectTransform contentRect;
//    [SerializeField] RectTransform viewport;
//    [SerializeField] HorizontalLayoutGroup layoutGroup;
//    [SerializeField] List<RectTransform> items = new List<RectTransform>();

//    [Header("Indicators")]
//    [SerializeField] Image leftIndicator;
//    [SerializeField] Image rightIndicator;

//    [Header("Swipe Settings")]
//    [Tooltip("Pixels per second of release velocity needed to skip 1 extra chapter beyond where finger landed")]
//    [SerializeField] float velocityPerStep = 500f;

//    [Tooltip("Maximum total chapters to skip in one swipe (including momentum)")]
//    [SerializeField] int maxSkipSteps = 6;

//    [Tooltip("Snap animation duration in seconds")]
//    [SerializeField] float snapDuration = 0.35f;

//    public static Action<int> currentLevelChange;

//    int currentIndex;
//    int dragStartIndex;           // index when finger first touched
//    Vector2 dragStartPos;
//    Vector2 contentStartPos;

//    // Velocity tracking
//    Vector2 lastDragPos;
//    float lastDragTime;
//    float dragVelocityX;

//    void Start()
//    {
//        SetupDynamicPadding();
//        UpdateVisuals();
//        SnapToIndex(0, false);
//    }

//    void SetupDynamicPadding()
//    {
//        if (items.Count == 0) return;

//        float viewportWidth = viewport.rect.width;
//        float leftPadding = (viewportWidth / 2f) - (items[0].rect.width / 2f);
//        float rightPadding = (viewportWidth / 2f) - (items[items.Count - 1].rect.width / 2f);

//        layoutGroup.padding.left = Mathf.RoundToInt(leftPadding);
//        layoutGroup.padding.right = Mathf.RoundToInt(rightPadding);

//        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
//    }

//    // ─────────────────────────────────────────────
//    //  Pointer Events
//    // ─────────────────────────────────────────────

//    public void OnPointerDown(PointerEventData eventData)
//    {
//        dragStartPos = eventData.position;
//        contentStartPos = contentRect.anchoredPosition;
//        dragStartIndex = currentIndex;

//        lastDragPos = eventData.position;
//        lastDragTime = Time.unscaledTime;
//        dragVelocityX = 0f;

//        contentRect.DOKill();
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        // ── Track velocity (pixels per second) ──
//        float dt = Time.unscaledTime - lastDragTime;
//        if (dt > 0.001f)
//        {
//            float rawVelocity = (eventData.position.x - lastDragPos.x) / dt;
//            dragVelocityX = Mathf.Lerp(dragVelocityX, rawVelocity, 0.4f);
//        }

//        lastDragPos = eventData.position;
//        lastDragTime = Time.unscaledTime;

//        // ── Move content 1:1 with finger ──
//        float deltaX = eventData.position.x - dragStartPos.x;
//        contentRect.anchoredPosition =
//            new Vector2(contentStartPos.x + deltaX,
//                        contentRect.anchoredPosition.y);

//        // ── Update closest index live so visuals respond as finger moves ──
//        UpdateClosestIndex();
//        UpdateVisuals();
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        // currentIndex is already the chapter the finger physically landed on.
//        // Now add momentum: release velocity pushes extra chapters forward.

//        float speed = Mathf.Abs(dragVelocityX);
//        int extraSteps = Mathf.FloorToInt(speed / velocityPerStep);

//        if (extraSteps > 0)
//        {
//            int direction = dragVelocityX < 0 ? 1 : -1;
//            int targetIndex = Mathf.Clamp(
//                currentIndex + direction * extraSteps,
//                0,
//                items.Count - 1
//            );

//            // Never exceed maxSkipSteps total from where the drag started
//            int totalFromStart = Mathf.Abs(targetIndex - dragStartIndex);
//            if (totalFromStart > maxSkipSteps)
//            {
//                int clampedDirection = targetIndex > dragStartIndex ? 1 : -1;
//                targetIndex = dragStartIndex + clampedDirection * maxSkipSteps;
//                targetIndex = Mathf.Clamp(targetIndex, 0, items.Count - 1);
//            }

//            SnapToIndex(targetIndex, true);
//        }
//        else
//        {
//            // No meaningful velocity — snap to wherever finger naturally landed
//            SnapToIndex(currentIndex, true);
//        }
//    }

//    // ─────────────────────────────────────────────
//    //  Index & Snapping
//    // ─────────────────────────────────────────────

//    void UpdateClosestIndex()
//    {
//        float minDistance = float.MaxValue;
//        int closest = 0;

//        for (int i = 0; i < items.Count; i++)
//        {
//            Vector3 itemLocalPos =
//                viewport.InverseTransformPoint(items[i].position);

//            float distance = Mathf.Abs(itemLocalPos.x);

//            if (distance < minDistance)
//            {
//                minDistance = distance;
//                closest = i;
//            }
//        }

//        currentIndex = closest;
//    }

//    void SnapToIndex(int index, bool animate)
//    {
//        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);

//        Vector3 itemLocalPos =
//            viewport.InverseTransformPoint(items[currentIndex].position);

//        float targetX = contentRect.anchoredPosition.x - itemLocalPos.x;

//        if (animate)
//        {
//            contentRect.DOAnchorPosX(targetX, snapDuration)
//                .SetEase(Ease.OutCubic);
//        }
//        else
//        {
//            contentRect.anchoredPosition =
//                new Vector2(targetX, contentRect.anchoredPosition.y);
//        }

//        UpdateVisuals();
//        currentLevelChange?.Invoke(currentIndex + 1);
//    }

//    // ─────────────────────────────────────────────
//    //  Visuals
//    // ─────────────────────────────────────────────

//    void UpdateVisuals()
//    {
//        for (int i = 0; i < items.Count; i++)
//        {
//            CanvasGroup cg = items[i].GetComponent<CanvasGroup>();
//            bool isSelected = (i == currentIndex);

//            items[i].DOScale(isSelected ? Vector3.one : Vector3.one * 0.7f, 0.15f);
//            if (cg) cg.DOFade(isSelected ? 1f : 0.5f, 0.15f);
//        }

//        if (leftIndicator)
//            leftIndicator.gameObject.SetActive(currentIndex > 0);

//        if (rightIndicator)
//            rightIndicator.gameObject.SetActive(currentIndex < items.Count - 1);
//    }

//    // ─────────────────────────────────────────────
//    //  Public API
//    // ─────────────────────────────────────────────

//    public int CurrentIndex() => currentIndex;

//    /// <summary>Jump directly to a chapter index (e.g. from a button).</summary>
//    public void GoToIndex(int index) => SnapToIndex(index, true);
//}
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollCarouselEffect : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] RectTransform contentRect;
    [SerializeField] RectTransform viewport;
    [SerializeField] HorizontalLayoutGroup layoutGroup;
    [SerializeField] List<RectTransform> items = new List<RectTransform>();

    [Header("Indicators")]
    [SerializeField] Image leftIndicator;
    [SerializeField] Image rightIndicator;

    [Header("Swipe Settings")]
    [Tooltip("How far release velocity carries the content forward after finger lifts (seconds). Higher = more items skipped on fast swipe.")]
    [SerializeField] float momentumTime = 0.25f;

    [Tooltip("Snap animation duration in seconds")]
    [SerializeField] float snapDuration = 0.35f;

    [Tooltip("Scale of non-selected items")]
    [SerializeField] float inactiveScale = 0.7f;

    [Tooltip("Fade of non-selected items")]
    [SerializeField] float inactiveFade = 0.5f;

    public static Action<int> currentLevelChange;

    int currentIndex;

    // Drag state
    Vector2 dragStartPos;
    Vector2 contentStartPos;

    // Velocity tracking
    Vector2 lastDragPos;
    float lastDragTime;
    float dragVelocityX;

    // Per-item cached snap positions (content anchoredPosition.x when item i is centred)
    float[] snapPositions;

    // ─────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────

    void Start()
    {
        SetupDynamicPadding();
        CacheSnapPositions();
        SnapToIndex(0, false);
    }

    void SetupDynamicPadding()
    {
        if (items.Count == 0) return;

        float viewportWidth = viewport.rect.width;
        float leftPadding = (viewportWidth / 2f) - (items[0].rect.width / 2f);
        float rightPadding = (viewportWidth / 2f) - (items[items.Count - 1].rect.width / 2f);

        layoutGroup.padding.left = Mathf.RoundToInt(leftPadding);
        layoutGroup.padding.right = Mathf.RoundToInt(rightPadding);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    /// <summary>
    /// Cache where contentRect.anchoredPosition.x must be for each item to sit at viewport centre.
    /// Called once after layout is built. If items are added/removed at runtime, call this again.
    /// </summary>
    void CacheSnapPositions()
    {
        snapPositions = new float[items.Count];

        // Temporarily zero the content so InverseTransformPoint gives local positions
        Vector2 savedPos = contentRect.anchoredPosition;
        contentRect.anchoredPosition = Vector2.zero;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        for (int i = 0; i < items.Count; i++)
        {
            // Local x of item centre in viewport space when content is at 0
            Vector3 localPos = viewport.InverseTransformPoint(items[i].position);
            // To move item to x=0 we shift content by -localPos.x
            snapPositions[i] = -localPos.x;
        }

        contentRect.anchoredPosition = savedPos;
    }

    // ─────────────────────────────────────────────
    //  Pointer Events
    // ─────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        contentRect.DOKill();

        dragStartPos = eventData.position;
        contentStartPos = contentRect.anchoredPosition;
        dragVelocityX = 0f;
        lastDragPos = eventData.position;
        lastDragTime = Time.unscaledTime;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ── Velocity tracking ──
        float dt = Time.unscaledTime - lastDragTime;
        if (dt > 0.001f)
        {
            float raw = (eventData.position.x - lastDragPos.x) / dt;
            dragVelocityX = Mathf.Lerp(dragVelocityX, raw, 0.35f);
        }
        lastDragPos = eventData.position;
        lastDragTime = Time.unscaledTime;

        // ── Move content 1-to-1 with finger ──
        float deltaX = eventData.position.x - dragStartPos.x;
        contentRect.anchoredPosition =
            new Vector2(contentStartPos.x + deltaX, contentRect.anchoredPosition.y);

        // ── Continuous proportional scale/fade based on proximity to centre ──
        UpdateVisualsContinuous();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Project where the content would travel if momentum carried it forward.
        // dragVelocityX (px/s) × momentumTime (s) = extra pixels of travel.
        float projectedX = contentRect.anchoredPosition.x + dragVelocityX * momentumTime;

        // Snap to whichever item is closest to that projected position.
        int targetIndex = ClosestIndexToContentX(projectedX);
        SnapToIndex(targetIndex, true);
    }

    // ─────────────────────────────────────────────
    //  Index helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns the item index whose snap position is nearest to the given contentX.
    /// </summary>
    int ClosestIndexToContentX(float contentX)
    {
        int best = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < snapPositions.Length; i++)
        {
            float dist = Mathf.Abs(snapPositions[i] - contentX);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best;
    }

    // ─────────────────────────────────────────────
    //  Snapping
    // ─────────────────────────────────────────────

    void SnapToIndex(int index, bool animate)
    {
        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);
        float targetX = snapPositions[currentIndex];

        if (animate)
        {
            contentRect
                .DOAnchorPosX(targetX, snapDuration)
                .SetEase(Ease.OutCubic)
                .OnUpdate(UpdateVisualsContinuous);
        }
        else
        {
            contentRect.anchoredPosition =
                new Vector2(targetX, contentRect.anchoredPosition.y);
            UpdateVisualsContinuous();
        }

        UpdateIndicators();
        currentLevelChange?.Invoke(currentIndex + 1);
    }

    // ─────────────────────────────────────────────
    //  Visuals — continuous proportional scale/fade
    // ─────────────────────────────────────────────

    /// <summary>
    /// Scale and fade each item proportionally to how close its centre is to the viewport centre.
    /// Runs both during drag AND during the snap tween (via OnUpdate).
    /// </summary>
    void UpdateVisualsContinuous()
    {
        for (int i = 0; i < items.Count; i++)
        {
            // Distance of this item's centre from viewport centre (in viewport-local space)
            Vector3 localPos = viewport.InverseTransformPoint(items[i].position);
            float dist = Mathf.Abs(localPos.x);

            // Normalise: 0 = perfectly centred, 1 = one full item-width away
            float itemWidth = items[i].rect.width;
            float t = Mathf.Clamp01(dist / (itemWidth * 1.2f)); // 1.2 = falloff radius

            float scale = Mathf.Lerp(1f, inactiveScale, t);
            float fade = Mathf.Lerp(1f, inactiveFade, t);

            items[i].localScale = Vector3.one * scale;

            CanvasGroup cg = items[i].GetComponent<CanvasGroup>();
            if (cg) cg.alpha = fade;
        }
    }

    void UpdateIndicators()
    {
        if (leftIndicator)
            leftIndicator.gameObject.SetActive(currentIndex > 0);

        if (rightIndicator)
            rightIndicator.gameObject.SetActive(currentIndex < items.Count - 1);
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    public int CurrentIndex() => currentIndex;

    /// <summary>Jump directly to a chapter index (e.g. from a button).</summary>
    public void GoToIndex(int index) => SnapToIndex(index, true);
}
