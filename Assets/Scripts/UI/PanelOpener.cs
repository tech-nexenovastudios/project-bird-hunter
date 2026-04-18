using DG.Tweening;
using UnityEngine;


public class PanelOpener : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.15f;
    [SerializeField] private float closeDuration = 0.15f;
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

        var group = parent.gameObject.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 0;
            group.DOFade(1, openDuration).SetEase(openEase).SetUpdate(true).OnComplete(() => { group.interactable = true; });
        }

        child.DOKill();
        child.DOScale(Vector3.one, openDuration).SetEase(openEase).SetUpdate(true);
    }

    // ==================== Close Animations ====================
    /// <summary>Scales child down to 0.8f, then deactivates parent.</summary>
    public void ClosePanel(GameObject parent)
    {
        var child = GetChild(parent);
        if (child == null) return;
        if (!parent.activeSelf) return;

        child.DOKill();

        var group = parent.gameObject.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = false;
            group.DOFade(0, closeDuration).SetEase(closeEase).SetUpdate(true);
        }

        // Tween to 0.7f scale instead of 0
        child.DOScale(Vector3.one * 0.7f, closeDuration).SetEase(closeEase).SetUpdate(true)
            .OnComplete(() =>
            {
                parent.SetActive(false);
                child.localScale = Vector3.one; 
            });
    }

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
}