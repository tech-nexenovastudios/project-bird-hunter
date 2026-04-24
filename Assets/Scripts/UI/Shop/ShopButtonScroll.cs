using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ShopButtonScroll : MonoBehaviour
{
    [SerializeField] private RectTransform scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private ButtonSelection buttonSelection;

    [System.Serializable]
    public class Section
    {
        public RectTransform contentRect;
        public Transform sideButton;
    }

    [SerializeField] private Section[] sections;

    private int _activeIndex = -1;
    private bool _scrolling;

    private void OnEnable() => scroll.onValueChanged.AddListener(OnScroll);
    private void OnDisable() => scroll.onValueChanged.RemoveListener(OnScroll);

    private void OnScroll(Vector2 _)
    {
        if (_scrolling) return;

        float centerY = scrollRect.anchoredPosition.y + viewport.rect.height / 2f;
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < sections.Length; i++)
        {
            float dist = Mathf.Abs(Mathf.Abs(sections[i].contentRect.anchoredPosition.y) - centerY);
            if (dist < minDist) { minDist = dist; closest = i; }
        }

        if (closest == _activeIndex) return;
        _activeIndex = closest;
        buttonSelection.btnSelection(sections[closest].sideButton);
    }

    public void ScrollTo(int index)
    {
        _activeIndex = index;
        buttonSelection.btnSelection(sections[index].sideButton);

        RectTransform rect = sections[index].contentRect;
        float target = Mathf.Clamp(
            Mathf.Abs(rect.anchoredPosition.y) - (viewport.rect.height / 2f + rect.rect.height / 2f -250) ,
            0, scrollRect.rect.height - viewport.rect.height 
        );

        _scrolling = true;
        scrollRect.DOAnchorPosY(target, 0.2f).OnComplete(() => _scrolling = false);
    }
}