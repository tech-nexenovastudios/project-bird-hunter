using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Clone spawns as sibling of popupPanel.
/// Original card hides when clone flies out, reappears when clone returns.
///
/// Hierarchy:
///   SomeParent
///     ├── PowerUpPopUpPanel (blur overlay, inactive)
///     │     └── DescriptionText (TMP)
///     └── PopupCard_Clone (spawned at runtime as sibling of popupPanel)
/// </summary>
public class PowerupPopupManager : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private PanelOpener panelOpener;
    [SerializeField] private GameObject popupPanel;

    [Header("Card Container")]
    [SerializeField] private Transform cardContainer;

    [Header("Card Settings")]
    [SerializeField] private float cardScale = 1.75f;
    [SerializeField] private float flyDuration = 0.4f;

    private GameObject clonedCard;
    private RectTransform clonedRT;
    private Vector2 startScreenPos;
    private Canvas rootCanvas;
    private TextMeshProUGUI descriptionText;
    private bool isOpen = false;

    // Original card reference for hide/show
    private GameObject originalCard;
    private CanvasGroup originalCG;

    // Parent where clone spawns (same parent as popupPanel)
    private RectTransform spawnParentRT;

    private void Awake()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            spawnParentRT = popupPanel.transform.parent.GetComponent<RectTransform>();

            var dt = FindDeep(popupPanel.transform, "DescriptionText");
            if (dt != null)
            {
                descriptionText = dt.GetComponent<TextMeshProUGUI>();
                descriptionText.gameObject.SetActive(false);
            }
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = FindObjectOfType<Canvas>();
    }

    private void Start()
    {
        if (cardContainer == null)
        {
            Debug.LogError("[PowerupPopup] cardContainer is NULL!");
            return;
        }

        int count = 0;
        foreach (Transform card in cardContainer)
        {
            var btn = card.GetComponent<Button>();
            if (btn == null)
                btn = card.gameObject.AddComponent<Button>();

            // Ensure card has a raycast target (needed for click detection)
            var graphic = card.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;

            // Ensure Navigation is None so ScrollRect isn't affected
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.transition = Selectable.Transition.None;

            var cardObj = card.gameObject;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[PowerupPopup] Card clicked: {cardObj.name}");
                ShowPopup(cardObj);
            });
            count++;
        }

        Debug.Log($"[PowerupPopup] Hooked {count} cards in '{cardContainer.name}'.");
    }

    // ==================== Show Popup ====================

    public void ShowPopup(GameObject clickedCard)
    {
        if (popupPanel == null || clickedCard == null || isOpen) return;

        isOpen = true;
        ClearCard();

        // Store original card and hide it
        originalCard = clickedCard;
        HideOriginalCard();

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;

        startScreenPos = RectTransformUtility.WorldToScreenPoint(cam, clickedCard.transform.position);

        // Open blur panel
        panelOpener.OpenPanel(popupPanel);

        // Hide description until card is enlarged
        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        // Clone card as sibling of popupPanel
        clonedCard = Instantiate(clickedCard, spawnParentRT);
        clonedCard.name = "PopupCard_Clone";
        clonedCard.transform.SetAsLastSibling();

        // Make clone fully visible (in case it inherited hidden state)
        var cloneCG = clonedCard.GetComponent<CanvasGroup>();
        if (cloneCG != null)
        {
            cloneCG.alpha = 1f;
            cloneCG.blocksRaycasts = true;
        }

        clonedRT = clonedCard.GetComponent<RectTransform>();
        var sourceRT = clickedCard.GetComponent<RectTransform>();

        clonedRT.anchorMin = new Vector2(0.5f, 0.5f);
        clonedRT.anchorMax = new Vector2(0.5f, 0.5f);
        clonedRT.pivot = new Vector2(0.5f, 0.5f);
        clonedRT.sizeDelta = sourceRT.sizeDelta;

        // Place clone at original card's position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParentRT, startScreenPos, cam, out Vector2 startLocal);

        clonedRT.anchoredPosition = startLocal;
        clonedCard.transform.localScale = Vector3.one;

        // Fly to center and scale up
        Sequence openSeq = DOTween.Sequence().SetUpdate(true);
        openSeq.Append(clonedRT.DOAnchorPos(Vector2.zero, flyDuration).SetEase(Ease.OutCubic));
        openSeq.Join(clonedCard.transform.DOScale(Vector3.one * cardScale, flyDuration).SetEase(Ease.OutBack));
        openSeq.OnComplete(() =>
        {
            if (descriptionText != null)
            {
                descriptionText.text = "This powerup enhances your cannon abilities during battle. Collect more powerups to unlock stronger effects and dominate.";
                descriptionText.gameObject.SetActive(true);
            }
            FetchAndSetDescription();
        });

        // Click anywhere on blur panel to close
        var btn = popupPanel.GetComponent<Button>();
        if (btn == null)
            btn = popupPanel.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ClosePopup);
    }

    // ==================== Hide / Show Original Card ====================

    private void HideOriginalCard()
    {
        if (originalCard == null) return;

        originalCG = originalCard.GetComponent<CanvasGroup>();
        if (originalCG == null)
            originalCG = originalCard.AddComponent<CanvasGroup>();

        originalCG.alpha = 0f;
        originalCG.blocksRaycasts = false;
        originalCG.interactable = false;
    }

    private void ShowOriginalCard()
    {
        if (originalCG != null)
        {
            originalCG.alpha = 1f;
            originalCG.blocksRaycasts = true;
            originalCG.interactable = true;
        }
        originalCard = null;
        originalCG = null;
    }

    // ==================== Description ====================

    /// <summary>TODO: Fetch actual description and set descriptionText here.</summary>
    private void FetchAndSetDescription()
    {
        // Fill later
    }

    // ==================== Close ====================

    private void ClosePopup()
    {
        if (!isOpen) return;

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        if (clonedCard == null || clonedRT == null)
        {
            ShowOriginalCard();
            panelOpener.ClosePanel(popupPanel);
            isOpen = false;
            return;
        }

        clonedCard.transform.DOKill();
        clonedRT.DOKill();

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParentRT, startScreenPos, cam, out Vector2 returnLocal);

        Sequence closeSeq = DOTween.Sequence().SetUpdate(true);
        closeSeq.Append(clonedCard.transform.DOScale(Vector3.one, flyDuration * 0.5f).SetEase(Ease.InCubic));
        closeSeq.Join(clonedRT.DOAnchorPos(returnLocal, flyDuration).SetEase(Ease.InCubic));
        closeSeq.OnComplete(() =>
        {
            ShowOriginalCard();
            panelOpener.ClosePanel(popupPanel);
            ClearCard();
            isOpen = false;
        });
    }

    // ==================== Helpers ====================

    private void ClearCard()
    {
        if (clonedCard != null)
        {
            clonedCard.transform.DOKill();
            var rt = clonedCard.GetComponent<RectTransform>();
            if (rt != null) rt.DOKill();
            Destroy(clonedCard);
            clonedCard = null;
            clonedRT = null;
        }
    }

    private Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            var result = FindDeep(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    private void OnDestroy() => ClearCard();
}