

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

//    public static Action<int> currentLevelChange; // ✅ Added back

//    int currentIndex;
//    Vector2 dragStartPos;
//    Vector2 contentStartPos;

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

//        float leftPadding =
//            (viewportWidth / 2f) - (items[0].rect.width / 2f);

//        float rightPadding =
//            (viewportWidth / 2f) - (items[items.Count - 1].rect.width / 2f);

//        layoutGroup.padding.left = Mathf.RoundToInt(leftPadding);
//        layoutGroup.padding.right = Mathf.RoundToInt(rightPadding);

//        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
//    }

//    public void OnPointerDown(PointerEventData eventData)
//    {
//        dragStartPos = eventData.position;
//        contentStartPos = contentRect.anchoredPosition;
//        contentRect.DOKill();
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        Vector2 delta = eventData.position - dragStartPos;

//        contentRect.anchoredPosition =
//            new Vector2(contentStartPos.x + delta.x,
//                        contentRect.anchoredPosition.y);

//        UpdateClosestIndex();
//        UpdateVisuals();
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        SnapToIndex(currentIndex, true);
//    }

//    void UpdateClosestIndex()
//    {
//        float minDistance = float.MaxValue;
//        int closest = 0;

//        for (int i = 0; i < items.Count; i++)
//        {
//            // Get item center relative to viewport
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
//        // Item center in viewport local space
//        Vector3 itemLocalPos =
//            viewport.InverseTransformPoint(items[index].position);

//        float offset = itemLocalPos.x;

//        float targetX = contentRect.anchoredPosition.x - offset;

//        if (animate)
//        {
//            contentRect.DOAnchorPosX(targetX, 0.3f)
//                .SetEase(Ease.OutCubic);
//        }
//        else
//        {
//            contentRect.anchoredPosition =
//                new Vector2(targetX, contentRect.anchoredPosition.y);
//        }

//        currentIndex = index;
//        UpdateVisuals();

//        currentLevelChange?.Invoke(currentIndex + 1);
//    }

//    void UpdateVisuals()
//    {
//        for (int i = 0; i < items.Count; i++)
//        {
//            CanvasGroup cg = items[i].GetComponent<CanvasGroup>();

//            if (i == currentIndex)
//            {
//                items[i].DOScale(Vector3.one * 1f, 0.15f);
//                if (cg) cg.DOFade(1f, 0.15f);
//            }
//            else
//            {
//                items[i].DOScale(Vector3.one * 0.7f, 0.15f);
//                if (cg) cg.DOFade(0.5f, 0.15f);
//            }
//        }

//        if (leftIndicator)
//            leftIndicator.gameObject.SetActive(currentIndex > 0);

//        if (rightIndicator)
//            rightIndicator.gameObject.SetActive(currentIndex < items.Count - 1);
//    }

//    public int CurrentIndex() => currentIndex;
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
    [Tooltip("Pixels per second of release velocity needed to skip 1 extra chapter beyond where finger landed")]
    [SerializeField] float velocityPerStep = 500f;

    [Tooltip("Maximum total chapters to skip in one swipe (including momentum)")]
    [SerializeField] int maxSkipSteps = 6;

    [Tooltip("Snap animation duration in seconds")]
    [SerializeField] float snapDuration = 0.35f;

    public static Action<int> currentLevelChange;

    int currentIndex;
    int dragStartIndex;           // index when finger first touched
    Vector2 dragStartPos;
    Vector2 contentStartPos;

    // Velocity tracking
    Vector2 lastDragPos;
    float lastDragTime;
    float dragVelocityX;

    void Start()
    {
        SetupDynamicPadding();
        UpdateVisuals();
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

    // ─────────────────────────────────────────────
    //  Pointer Events
    // ─────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
        contentStartPos = contentRect.anchoredPosition;
        dragStartIndex = currentIndex;

        lastDragPos = eventData.position;
        lastDragTime = Time.unscaledTime;
        dragVelocityX = 0f;

        contentRect.DOKill();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ── Track velocity (pixels per second) ──
        float dt = Time.unscaledTime - lastDragTime;
        if (dt > 0.001f)
        {
            float rawVelocity = (eventData.position.x - lastDragPos.x) / dt;
            dragVelocityX = Mathf.Lerp(dragVelocityX, rawVelocity, 0.4f);
        }

        lastDragPos = eventData.position;
        lastDragTime = Time.unscaledTime;

        // ── Move content 1:1 with finger ──
        float deltaX = eventData.position.x - dragStartPos.x;
        contentRect.anchoredPosition =
            new Vector2(contentStartPos.x + deltaX,
                        contentRect.anchoredPosition.y);

        // ── Update closest index live so visuals respond as finger moves ──
        UpdateClosestIndex();
        UpdateVisuals();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // currentIndex is already the chapter the finger physically landed on.
        // Now add momentum: release velocity pushes extra chapters forward.

        float speed = Mathf.Abs(dragVelocityX);
        int extraSteps = Mathf.FloorToInt(speed / velocityPerStep);

        if (extraSteps > 0)
        {
            int direction = dragVelocityX < 0 ? 1 : -1;
            int targetIndex = Mathf.Clamp(
                currentIndex + direction * extraSteps,
                0,
                items.Count - 1
            );

            // Never exceed maxSkipSteps total from where the drag started
            int totalFromStart = Mathf.Abs(targetIndex - dragStartIndex);
            if (totalFromStart > maxSkipSteps)
            {
                int clampedDirection = targetIndex > dragStartIndex ? 1 : -1;
                targetIndex = dragStartIndex + clampedDirection * maxSkipSteps;
                targetIndex = Mathf.Clamp(targetIndex, 0, items.Count - 1);
            }

            SnapToIndex(targetIndex, true);
        }
        else
        {
            // No meaningful velocity — snap to wherever finger naturally landed
            SnapToIndex(currentIndex, true);
        }
    }

    // ─────────────────────────────────────────────
    //  Index & Snapping
    // ─────────────────────────────────────────────

    void UpdateClosestIndex()
    {
        float minDistance = float.MaxValue;
        int closest = 0;

        for (int i = 0; i < items.Count; i++)
        {
            Vector3 itemLocalPos =
                viewport.InverseTransformPoint(items[i].position);

            float distance = Mathf.Abs(itemLocalPos.x);

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = i;
            }
        }

        currentIndex = closest;
    }

    void SnapToIndex(int index, bool animate)
    {
        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);

        Vector3 itemLocalPos =
            viewport.InverseTransformPoint(items[currentIndex].position);

        float targetX = contentRect.anchoredPosition.x - itemLocalPos.x;

        if (animate)
        {
            contentRect.DOAnchorPosX(targetX, snapDuration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            contentRect.anchoredPosition =
                new Vector2(targetX, contentRect.anchoredPosition.y);
        }

        UpdateVisuals();
        currentLevelChange?.Invoke(currentIndex + 1);
    }

    // ─────────────────────────────────────────────
    //  Visuals
    // ─────────────────────────────────────────────

    void UpdateVisuals()
    {
        for (int i = 0; i < items.Count; i++)
        {
            CanvasGroup cg = items[i].GetComponent<CanvasGroup>();
            bool isSelected = (i == currentIndex);

            items[i].DOScale(isSelected ? Vector3.one : Vector3.one * 0.7f, 0.15f);
            if (cg) cg.DOFade(isSelected ? 1f : 0.5f, 0.15f);
        }

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
