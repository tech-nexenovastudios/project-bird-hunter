using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class ButtonShine : MonoBehaviour
{
    public enum ShineDirection { Horizontal, DiagonalTopLeftToBottomRight }

    [SerializeField] RectTransform shine;
    [SerializeField] ShineDirection direction = ShineDirection.Horizontal;
    [SerializeField] float duration = 0.8f;
    [SerializeField] float interval = 2f;
    [SerializeField] Ease ease = Ease.InOutSine;

    Sequence seq;

    void OnEnable()
    {
        if (!shine) return;
        StartCoroutine(StartShineNextFrame());
    }

    IEnumerator StartShineNextFrame()
    {
        // Wait for layout to settle
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

        var rect = ((RectTransform)transform).rect;
        Vector2 start, end;

        if (direction == ShineDirection.DiagonalTopLeftToBottomRight)
        {
            start = new Vector2(-rect.width, rect.height);
            end = new Vector2(rect.width, -rect.height);
        }
        else
        {
            start = new Vector2(-rect.width, shine.anchoredPosition.y);
            end = new Vector2(rect.width, shine.anchoredPosition.y);
        }

        shine.anchoredPosition = start;

        seq = DOTween.Sequence()
            .Append(shine.DOAnchorPos(end, duration).SetEase(ease))
            .AppendCallback(() => shine.anchoredPosition = start)
            .AppendInterval(interval)
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    void OnDisable() => seq?.Kill();
}