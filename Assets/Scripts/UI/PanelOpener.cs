using DG.Tweening;
using UnityEngine;


public class PanelOpener : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;

    // ==================== Open Animations ====================

    /// <summary>Activates parent, scales child from 0 to full.</summary>
    public void OpenPanel(GameObject parent)
    {
        var child = GetChild(parent);
        if (child == null) return;
        if (parent.activeSelf) return;

        child.localScale = Vector3.zero;
        parent.SetActive(true);

        child.DOKill();
        child.DOScale(Vector3.one, openDuration).SetEase(openEase).SetUpdate(true);
    }

    // /// <summary>Activates parent, slides child in from bottom.</summary>
    // public void OpenFromBottom(GameObject parent)
    // {
    //     var child = GetChildRT(parent);
    //     if (child == null) return;
    //     if (parent.activeSelf) return;

    //     Vector2 targetPos = child.anchoredPosition;
    //     child.anchoredPosition = targetPos + new Vector2(0, -Screen.height);
    //     parent.SetActive(true);

    //     child.DOKill();
    //     child.DOAnchorPos(targetPos, openDuration).SetEase(openEase).SetUpdate(true);
    // }

    // /// <summary>Activates parent, slides child in from top.</summary>
    // public void OpenFromTop(GameObject parent)
    // {
    //     var child = GetChildRT(parent);
    //     if (child == null) return;
    //     if (parent.activeSelf) return;

    //     Vector2 targetPos = child.anchoredPosition;
    //     child.anchoredPosition = targetPos + new Vector2(0, Screen.height);
    //     parent.SetActive(true);

    //     child.DOKill();
    //     child.DOAnchorPos(targetPos, openDuration).SetEase(openEase).SetUpdate(true);
    // }

    // /// <summary>Activates parent, slides child in from left.</summary>
    // public void OpenFromLeft(GameObject parent)
    // {
    //     var child = GetChildRT(parent);
    //     if (child == null) return;
    //     if (parent.activeSelf) return;

    //     Vector2 targetPos = child.anchoredPosition;
    //     child.anchoredPosition = targetPos + new Vector2(-Screen.width, 0);
    //     parent.SetActive(true);

    //     child.DOKill();
    //     child.DOAnchorPos(targetPos, openDuration).SetEase(openEase).SetUpdate(true);
    // }

    // /// <summary>Activates parent, slides child in from right.</summary>
    // public void OpenFromRight(GameObject parent)
    // {
    //     var child = GetChildRT(parent);
    //     if (child == null) return;
    //     if (parent.activeSelf) return;

    //     Vector2 targetPos = child.anchoredPosition;
    //     child.anchoredPosition = targetPos + new Vector2(Screen.width, 0);
    //     parent.SetActive(true);

    //     child.DOKill();
    //     child.DOAnchorPos(targetPos, openDuration).SetEase(openEase).SetUpdate(true);
    // }

    /// <summary>Activates parent, fades child in.</summary>
    // public void OpenFade(GameObject parent)
    // {
    //     var child = GetChild(parent);
    //     if (child == null) return;
    //     if (parent.activeSelf) return;

    //     var cg = child.GetComponent<CanvasGroup>();
    //     if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();

    //     cg.alpha = 0f;
    //     parent.SetActive(true);

    //     DOTween.Kill(cg);
    //     cg.DOFade(1f, openDuration).SetUpdate(true);
    // }

   // ==================== Close Animations ====================

    /// <summary>Scales child down to 0.8f, then deactivates parent.</summary>
    public void ClosePanel(GameObject parent)
    {
        var child = GetChild(parent);
        if (child == null) return;
        if (!parent.activeSelf) return;

        child.DOKill();

        // Tween to 0.8f scale instead of 0
        child.DOScale(Vector3.one * 0.8f, closeDuration).SetEase(closeEase).SetUpdate(true)
            .OnComplete(() =>
            {
                parent.SetActive(false);
                child.localScale = Vector3.one; // Reset scale for the next time it opens
            });
    }

    // /// <summary>Slides child to bottom, then deactivates parent.</summary>
    // public void CloseToBottom(GameObject parent)
    // {
    //     var child = GetChildRT(parent);
    //     if (child == null) return;
    //     if (!parent.activeSelf) return;

    //     Vector2 originalPos = child.anchoredPosition;

    //     child.DOKill();
    //     child.DOAnchorPos(originalPos + new Vector2(0, -Screen.height), closeDuration)
    //         .SetEase(closeEase).SetUpdate(true)
    //         .OnComplete(() =>
    //         {
    //             parent.SetActive(false);
    //             child.anchoredPosition = originalPos;
    //         });
    // }

    // /// <summary>Fades child out, then deactivates parent.</summary>
    // public void CloseFade(GameObject parent)
    // {
    //     var child = GetChild(parent);
    //     if (child == null) return;
    //     if (!parent.activeSelf) return;

    //     var cg = child.GetComponent<CanvasGroup>();
    //     if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();

    //     DOTween.Kill(cg);
    //     cg.DOFade(0f, closeDuration).SetUpdate(true)
    //         .OnComplete(() =>
    //         {
    //             parent.SetActive(false);
    //             cg.alpha = 1f;
    //         });
    // }

    // ==================== Toggle ====================

    /// <summary>Opens if closed, closes if open. Scale animation.</summary>
    public void TogglePanel(GameObject parent)
    {
        if (parent == null) return;

        if (parent.activeSelf)
            ClosePanel(parent);
        else
            OpenPanel(parent);
    }

    // ==================== Helpers ====================

    private Transform GetChild(GameObject parent)
    {
        if (parent == null || parent.transform.childCount == 0)
        {
            Debug.LogWarning("[PanelOpener] Parent is null or has no children.");
            return null;
        }
        return parent.transform.GetChild(0);
    }

    private RectTransform GetChildRT(GameObject parent)
    {
        var child = GetChild(parent);
        if (child == null) return null;
        return child.GetComponent<RectTransform>();
    }
}