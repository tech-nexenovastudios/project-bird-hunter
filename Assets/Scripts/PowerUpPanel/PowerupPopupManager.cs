using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.PowerUps;

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
    private TextMeshProUGUI descriptionText;
    private bool isOpen = false;

    private GameObject originalCard;
    private CanvasGroup originalCG;
    private RectTransform spawnParentRT;

    // ── No longer cached — always fetched fresh ───────────────────────────────
    private Canvas RootCanvas
    {
        get
        {
            // Walk up the hierarchy first (cheapest)
            var c = GetComponentInParent<Canvas>();
            if (c != null && c.rootCanvas != null) return c.rootCanvas;

            // Fallback: find the root canvas in the scene
            foreach (var canvas in FindObjectsOfType<Canvas>())
                if (canvas.isRootCanvas) return canvas;

            return null;
        }
    }

    private Camera UICam
    {
        get
        {
            var canvas = RootCanvas;
            if (canvas == null) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
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

            var graphic = card.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;

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

        // Validate canvas before doing anything
        var canvas = RootCanvas;
        if (canvas == null)
        {
            Debug.LogError("[PowerupPopup] No valid root Canvas found. Cannot open popup.");
            return;
        }

        isOpen = true;
        ClearCard();

        originalCard = clickedCard;
        HideOriginalCard();

        Camera cam = UICam;  // derived from fresh canvas lookup

        startScreenPos = RectTransformUtility.WorldToScreenPoint(cam, clickedCard.transform.position);

        panelOpener.OpenPanel(popupPanel);
        AudioManager.Instance?.PlayButtonClickPanelOpen(1f);

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        clonedCard = Instantiate(clickedCard, spawnParentRT);
        clonedCard.name = "PopupCard_Clone";
        clonedCard.transform.SetAsLastSibling();

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

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParentRT, startScreenPos, cam, out Vector2 startLocal);

        clonedRT.anchoredPosition = startLocal;
        clonedCard.transform.localScale = Vector3.one;

        Sequence openSeq = DOTween.Sequence().SetUpdate(true);
        openSeq.Append(clonedRT.DOAnchorPos(Vector2.zero, flyDuration).SetEase(Ease.OutCubic));
        openSeq.Join(clonedCard.transform.DOScale(Vector3.one * cardScale, flyDuration).SetEase(Ease.OutBack));
        openSeq.OnComplete(() =>
        {
            FetchAndSetDescription();
        });

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

    private void FetchAndSetDescription()
    {
        if (descriptionText == null) return;

        var cfg = ResolveConfig(originalCard);
        string text = cfg != null ? cfg.description : string.Empty;

        if (string.IsNullOrEmpty(text))
        {
            descriptionText.text = string.Empty;
            descriptionText.gameObject.SetActive(false);
            if (cfg == null)
                Debug.LogWarning($"[PowerupPopup] No PowerupConfig resolved for '{(originalCard != null ? originalCard.name : "null")}'.");
            return;
        }

        descriptionText.text = text;
        descriptionText.gameObject.SetActive(true);
    }

    private static PowerupConfig ResolveConfig(GameObject card)
    {
        if (card == null) return null;

        var controller = card.GetComponent<PowerupCardController>();
        var db = PowerupGate.Database;

        if (controller != null && !string.IsNullOrEmpty(controller.PowerupId) && db != null)
        {
            var byId = db.GetPowerupById(controller.PowerupId);
            if (byId != null) return byId;
        }

        return PowerupGate.FindByNameOrId(card.name);
    }

    // ==================== Close ====================

    private void ClosePopup()
    {
        if (!isOpen) return;

        AudioManager.Instance?.PlayButtonClickPanelOpen(0.5f);

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

        Camera cam = UICam;  // fresh lookup again — canvas may have been reassigned

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
            if (child.name == childName) return child;
            var result = FindDeep(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    private void OnDestroy() => ClearCard();
}