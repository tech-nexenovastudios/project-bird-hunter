using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class ButtonShine : MonoBehaviour
{
    [SerializeField] RectTransform shine;
    [SerializeField] float duration = 0.8f;
    [SerializeField] float interval = 2f;
    [SerializeField] Ease ease = Ease.InOutSine;

    Sequence seq;

    void OnEnable()
    {
        if (!shine) return;

        float width = ((RectTransform)transform).rect.width;
        float start = -width;
        float end = width;

        shine.anchoredPosition = new Vector2(start, shine.anchoredPosition.y);

        seq = DOTween.Sequence()
            .Append(shine.DOAnchorPosX(end, duration).SetEase(ease))
            .AppendCallback(() => shine.anchoredPosition = new Vector2(start, shine.anchoredPosition.y))
            .AppendInterval(interval)
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    void OnDisable() => seq?.Kill();
}