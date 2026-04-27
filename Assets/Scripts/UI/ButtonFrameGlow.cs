using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class ButtonFrameGlow : MonoBehaviour
{
    [SerializeField] RectTransform glow;
    [SerializeField] float duration = 2.5f;
    [SerializeField] float xExtent = 0.45f;
    [SerializeField] float yExtent = 0.35f;
    [SerializeField] Ease ease = Ease.InOutSine;

    Sequence seq;

    void OnEnable()
    {
        if (!glow) return;

        var frame = (RectTransform)transform;
        float xAmp = (frame.rect.width - 75f) * xExtent; 
        float yAmp = frame.rect.height * yExtent;

        Vector2 bottomLeft = new Vector2(-xAmp, -yAmp);
        Vector2 bottomRight = new Vector2(xAmp, -yAmp);
        Vector2 topRight = new Vector2(xAmp, yAmp);

        glow.anchoredPosition = bottomLeft;

        seq = DOTween.Sequence()
            .Append(glow.DOAnchorPos(bottomRight, duration * 0.5f).SetEase(ease))
            .Append(glow.DOAnchorPos(topRight, duration * 0.5f).SetEase(ease))
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    void OnDisable() => seq?.Kill();
}