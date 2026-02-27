using DG.Tweening;
using UnityEngine;

public class ShopButtonScroll : MonoBehaviour
{
    [SerializeField]RectTransform scrollRect;
    public void Scroll(RectTransform rect)
    {
        if (rect != null)
        {
            scrollRect.DOAnchorPosY(Mathf.Clamp(Mathf.Abs(rect.anchoredPosition.y) - Mathf.Abs(rect.sizeDelta.y / 2), 0, Mathf.Abs(scrollRect.sizeDelta.y / 2)),0.2f);
        }
    }
}
