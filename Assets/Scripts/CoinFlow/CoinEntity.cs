using System;
using DG.Tweening;
using Gameplay.Events;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class CoinEntity : MonoBehaviour
{
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Sequence currentSequence;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Launch(
        Vector2 origin,
        Vector2 target,
        CoinFlowConfig cfg,
        int value,
        Action<CoinEntity> onComplete)
    {
        currentSequence?.Kill();

        rect.position = origin;
        rect.localScale = Vector3.one * cfg.startScale;
        canvasGroup.alpha = 1f;

        // ── Straight line with tiny random offset ────────
        // No big arcs. Just a small horizontal nudge so
        // coins don't perfectly overlap each other.

        float tinyOffset = UnityEngine.Random.Range(-cfg.spreadWidth, cfg.spreadWidth);

        Vector2 midPoint = new Vector2(
            Mathf.Lerp(origin.x, target.x, 0.5f) + tinyOffset,
            Mathf.Lerp(origin.y, target.y, 0.5f)
        );

        Vector3[] path = new Vector3[]
        {
            origin,
            (Vector3)midPoint,
            (Vector3)target
        };

        currentSequence = DOTween.Sequence();

        currentSequence.Append(
            rect.DOPath(path, cfg.flightDuration, PathType.CatmullRom)
                .SetEase(cfg.dotweenFlightEase)
        );

        currentSequence.Insert(0f,
            rect.DOScale(Vector3.one * cfg.endScale, cfg.flightDuration)
                .SetEase(Ease.InQuad)
        );

        currentSequence.Insert(
            cfg.flightDuration * 0.85f,
            canvasGroup.DOFade(0.4f, cfg.flightDuration * 0.15f)
        );

        currentSequence.OnComplete(() =>
        {
            GameEvent.CoinArrived(value);
            onComplete?.Invoke(this);
        });
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }
}