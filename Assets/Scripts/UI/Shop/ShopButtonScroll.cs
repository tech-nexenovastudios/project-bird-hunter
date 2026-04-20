using DG.Tweening;
using UnityEngine;

public class ShopButtonScroll : MonoBehaviour
{
    [Tooltip("The 'Content' RectTransform that holds all the buttons.")]
    [SerializeField] private RectTransform scrollRect;

    [Tooltip("The 'Viewport' RectTransform of the Scroll View.")]
    [SerializeField] private RectTransform viewport;

    public void Scroll(RectTransform rect)
    {
        if (rect != null && viewport != null)
        {

            float itemY = Mathf.Abs(rect.anchoredPosition.y);

            float targetPosition = itemY - ((viewport.rect.height / 2f) + (rect.rect.height / 2f));


            float maxScrollY = Mathf.Max(0, scrollRect.rect.height - viewport.rect.height);


            targetPosition = Mathf.Clamp(targetPosition, 0, maxScrollY);


            scrollRect.DOAnchorPosY(targetPosition, 0.2f);
        }
    }
}
