using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private GameObject currentOriginalCard;
    private RectTransform clonedRT;
    private Vector2 startScreenPos;
    private Canvas rootCanvas;
    private RectTransform canvasRT;
    private TextMeshProUGUI descriptionText;
    private bool isOpen = false;

    private void Awake()
    {
        Debug.Log("<color=cyan>Manager Awake - Initializing...</color>");
        if (popupPanel != null) popupPanel.SetActive(false);

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        if (rootCanvas != null) canvasRT = rootCanvas.GetComponent<RectTransform>();

        if (popupPanel != null)
        {
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
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (cardContainer == null)
        {
            Debug.LogError("Card Container is missing on PowerupPopupManager!");
            return;
        }

        int count = 0;
        foreach (Transform card in cardContainer)
        {
            var btn = card.GetComponent<Button>();
            if (btn == null) btn = card.gameObject.AddComponent<Button>();

            // Remove all existing functions to ensure only THIS script runs
            btn.onClick.RemoveAllListeners();

            GameObject cardObj = card.gameObject;
            btn.onClick.AddListener(() => {
                Debug.Log($"Button clicked on: {cardObj.name}"); // TEST LOG
                ShowPopup(cardObj);
            });
            count++;
        }
        Debug.Log($"<color=green>Listeners setup for {count} cards.</color>");
    }

    public void ShowPopup(GameObject clickedCard)
    {
        // If this log doesn't show, the button is not calling THIS script
        Debug.Log($"<color=yellow>ShowPopup called for:</color> {clickedCard.name}");

        if (popupPanel == null || clickedCard == null || isOpen) return;

        isOpen = true;
        currentOriginalCard = clickedCard;

        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
        startScreenPos = RectTransformUtility.WorldToScreenPoint(cam, currentOriginalCard.transform.position);

        // 1. CLONE
        clonedCard = Instantiate(currentOriginalCard, canvasRT);
        clonedCard.name = "DEBUG_CLONE";

        // Remove scripts from clone to avoid recursion
        var cloneBtn = clonedCard.GetComponent<Button>();
        if (cloneBtn != null) Destroy(cloneBtn);

        // 2. HIDE ORIGINAL (The "Nuclear" way)
        SetVisibility(currentOriginalCard, false);

        // 3. PAUSE EDITOR (Diagnostic)
        // This will pause Unity the moment you click. 
        // Look at the Hierarchy: is the original card hidden?
        // Debug.Break(); 

        // 4. ANIMATE
        clonedCard.transform.SetAsLastSibling();
        clonedRT = clonedCard.GetComponent<RectTransform>();
        clonedRT.anchorMin = clonedRT.anchorMax = clonedRT.pivot = new Vector2(0.5f, 0.5f);

        RectTransform sourceRT = currentOriginalCard.GetComponent<RectTransform>();
        clonedRT.sizeDelta = sourceRT.sizeDelta;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, startScreenPos, cam, out Vector2 startLocal);
        clonedRT.anchoredPosition = startLocal;

        panelOpener.OpenPanel(popupPanel);

        Sequence openSeq = DOTween.Sequence().SetUpdate(true);
        openSeq.Append(clonedRT.DOAnchorPos(Vector2.zero, flyDuration).SetEase(Ease.OutCubic));
        openSeq.Join(clonedCard.transform.DOScale(Vector3.one * cardScale, flyDuration).SetEase(Ease.OutBack));

        // 5. Setup Close
        var btn = popupPanel.GetComponent<Button>();
        if (btn == null) btn = popupPanel.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ClosePopup);
    }

    private void SetVisibility(GameObject obj, bool visible)
    {
        if (obj == null) return;
        Debug.Log($"Setting {obj.name} visibility to {visible}");

        // Option A: Alpha
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = visible ? 1 : 0;
        cg.blocksRaycasts = visible;

        // Option B: Scale (Standard Layout won't break if scale is 0)
        obj.transform.localScale = visible ? Vector3.one : Vector3.zero;

        // Option C: Components
        var images = obj.GetComponentsInChildren<Image>(true);
        foreach (var img in images) img.enabled = visible;

        var texts = obj.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts) txt.enabled = visible;
    }

    public void ClosePopup()
    {
        Debug.Log("Closing Popup...");
        if (!isOpen) return;

        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, startScreenPos, cam, out Vector2 returnLocal);

        Sequence closeSeq = DOTween.Sequence().SetUpdate(true);
        closeSeq.Append(clonedCard.transform.DOScale(Vector3.one, flyDuration * 0.5f).SetEase(Ease.InCubic));
        closeSeq.Join(clonedRT.DOAnchorPos(returnLocal, flyDuration).SetEase(Ease.InCubic));
        closeSeq.OnComplete(() => {
            panelOpener.ClosePanel(popupPanel);
            if (clonedCard != null) Destroy(clonedCard);
            SetVisibility(currentOriginalCard, true);
            isOpen = false;
        });
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
}