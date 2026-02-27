using DG.Tweening;
using UnityEngine;

/// <summary>
/// Attach to ONE central GameObject (e.g., "ButtonAnimator" or "UIManager").
/// 
/// From any Button's OnClick() in Inspector:
///   1. Drag this central GameObject into the object slot
///   2. Select ButtonAnimator → PunchScale (or any method)
///   3. Drag the BUTTON ITSELF into the GameObject parameter slot
/// </summary>
public class ButtonAnimator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float strength = 0.15f;

    public void PunchScale(GameObject target)
    {
        target.transform.DOKill();
        target.transform.localScale = Vector3.one;
        target.transform.DOPunchScale(Vector3.one * strength, duration, 6, 0.5f);
    }

    public void PressScale(GameObject target)
    {
        target.transform.DOKill();
        target.transform.DOScale(0.90f, duration * 0.4f).OnComplete(() =>
            target.transform.DOScale(1f, duration * 0.6f).SetEase(Ease.OutBack));
    }

    public void Bounce(GameObject target)
    {
        target.transform.DOKill();
        target.transform.localScale = Vector3.one;
        target.transform.DOScale(1f + strength, duration * 0.4f).OnComplete(() =>
            target.transform.DOScale(1f, duration * 0.6f).SetEase(Ease.OutBounce));
    }

    public void PunchRotation(GameObject target)
    {
        target.transform.DOKill();
        target.transform.localRotation = Quaternion.identity;
        target.transform.DOPunchRotation(new Vector3(0, 0, 15f), duration, 6);
    }

    public void Shake(GameObject target)
    {
        target.transform.DOKill();
        target.transform.localScale = Vector3.one;
        target.transform.DOShakeScale(duration, strength, 10, 50f);
    }

    /// <summary>
    /// Slight downward press + color dim, then snaps back.
    /// No scale change — feels like a physical button press.
    /// </summary>
    public void Press(GameObject target)
    {
        var rt = target.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.DOKill();

        // Quick push down and darken
        Vector2 originalPos = rt.anchoredPosition;
        var image = target.GetComponent<UnityEngine.UI.Image>();
        Color originalColor = image != null ? image.color : Color.white;

        // Move down slightly
        rt.DOAnchorPos(originalPos + new Vector2(0, -4f), duration * 0.3f).SetEase(Ease.OutQuad);

        // Dim color
        if (image != null)
            image.DOColor(originalColor * 0.7f, duration * 0.3f);

        // Snap back
        DOVirtual.DelayedCall(duration * 0.35f, () =>
        {
            rt.DOAnchorPos(originalPos, duration * 0.4f).SetEase(Ease.OutBack);
            if (image != null)
                image.DOColor(originalColor, duration * 0.4f);
        });
    }
}