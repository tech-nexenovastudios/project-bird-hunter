

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

    public static Action<int> currentLevelChange; // ✅ Added back

    int currentIndex;
    Vector2 dragStartPos;
    Vector2 contentStartPos;

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

        float leftPadding =
            (viewportWidth / 2f) - (items[0].rect.width / 2f);

        float rightPadding =
            (viewportWidth / 2f) - (items[items.Count - 1].rect.width / 2f);

        layoutGroup.padding.left = Mathf.RoundToInt(leftPadding);
        layoutGroup.padding.right = Mathf.RoundToInt(rightPadding);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
        contentStartPos = contentRect.anchoredPosition;
        contentRect.DOKill();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - dragStartPos;

        contentRect.anchoredPosition =
            new Vector2(contentStartPos.x + delta.x,
                        contentRect.anchoredPosition.y);

        UpdateClosestIndex();
        UpdateVisuals();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToIndex(currentIndex, true);
    }

    void UpdateClosestIndex()
    {
        float minDistance = float.MaxValue;
        int closest = 0;

        for (int i = 0; i < items.Count; i++)
        {
            // Get item center relative to viewport
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
        // Item center in viewport local space
        Vector3 itemLocalPos =
            viewport.InverseTransformPoint(items[index].position);

        float offset = itemLocalPos.x;

        float targetX = contentRect.anchoredPosition.x - offset;

        if (animate)
        {
            contentRect.DOAnchorPosX(targetX, 0.3f)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            contentRect.anchoredPosition =
                new Vector2(targetX, contentRect.anchoredPosition.y);
        }

        currentIndex = index;
        UpdateVisuals();

        currentLevelChange?.Invoke(currentIndex + 1);
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < items.Count; i++)
        {
            CanvasGroup cg = items[i].GetComponent<CanvasGroup>();

            if (i == currentIndex)
            {
                items[i].DOScale(Vector3.one * 1f, 0.15f);
                if (cg) cg.DOFade(1f, 0.15f);
            }
            else
            {
                items[i].DOScale(Vector3.one * 0.7f, 0.15f);
                if (cg) cg.DOFade(0.5f, 0.15f);
            }
        }

        if (leftIndicator)
            leftIndicator.gameObject.SetActive(currentIndex > 0);

        if (rightIndicator)
            rightIndicator.gameObject.SetActive(currentIndex < items.Count - 1);
    }

    public int CurrentIndex() => currentIndex;
}
