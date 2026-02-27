using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ManaIconOverlayController : MonoBehaviour
{
    [Header("References")]
    public Image manaBarFill;     // the vertical Filled Image for the mana bar (0..1)
    public Image overlayIcon;     // the black overlay Image (same sprite as icon)
    public RectTransform iconRect; // RectTransform of the centre-icon (overlay should match this rect)

    [Header("Behavior")]
    public bool latchWhenFullyCrossed = true; // when fill crosses icon top, keep overlay full
    public float smoothSpeed = 20f; // 0 for instant, >0 for smoothing

    private bool latched = false;
    private float currentOverlayFill = 0f;

    void Update()
    {
        if (manaBarFill == null || overlayIcon == null || iconRect == null) return;
        UpdateOverlay();
    }

    void UpdateOverlay()
    {
        RectTransform mRT = manaBarFill.rectTransform;

        // get world Y of mana rect bottom/top
        Vector3[] mCorners = new Vector3[4];
        mRT.GetWorldCorners(mCorners);
        float manaBottomY = mCorners[0].y;
        float manaTopY = mCorners[1].y; // top-left has index 1

        // filled area top in world Y
        float filledY = Mathf.Lerp(manaBottomY, manaTopY, Mathf.Clamp01(manaBarFill.fillAmount));

        // get world Y of icon bottom/top
        Vector3[] iconCorners = new Vector3[4];
        iconRect.GetWorldCorners(iconCorners);
        float iconBottomY = iconCorners[0].y;
        float iconTopY = iconCorners[1].y;
        float iconHeight = Mathf.Max(0.0001f, iconTopY - iconBottomY);

        // Latch logic:
        if (latchWhenFullyCrossed)
        {
            if (filledY >= iconTopY) latched = true;
            else if (filledY <= iconBottomY) latched = false;
        }

        float targetFill = 0f;

        if (latched)
        {
            targetFill = 1f;
        }
        else
        {
            // overlap between filledY and icon rect
            float overlapTop = Mathf.Min(filledY, iconTopY);
            float overlapHeight = Mathf.Max(0f, overlapTop - iconBottomY);
            targetFill = Mathf.Clamp01(overlapHeight / iconHeight);
        }

        // smoothing
        if (smoothSpeed > 0f)
            currentOverlayFill = Mathf.MoveTowards(currentOverlayFill, targetFill, smoothSpeed * Time.deltaTime);
        else
            currentOverlayFill = targetFill;

        // Ensure overlay image is set to Filled / Vertical / Origin Bottom
        overlayIcon.fillAmount = currentOverlayFill;
    }
}
