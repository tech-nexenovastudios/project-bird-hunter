using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdatePanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Message Scroll (cap + scroll)")]
    [SerializeField] private ScrollRect messageScroll;
    [SerializeField] private LayoutElement messageScrollLayout;
    [SerializeField] private float maxMessageHeight = 500f;

    [Header("Buttons")]
    [SerializeField] private Button updateButton;
    [SerializeField] private Button notNowButton;

    private string storeUrl;
    private UniTaskCompletionSource dismissTcs;

    private void Awake()
    {
        Debug.Log($"[UpdatePanel] Awake on '{gameObject.name}' panelRoot={(panelRoot != null ? panelRoot.name : "<null>")} selfRef={(panelRoot == gameObject)}");
        if (updateButton != null) updateButton.onClick.AddListener(OnUpdateClicked);
        if (notNowButton != null) notNowButton.onClick.AddListener(OnNotNowClicked);
        // Do NOT call panelRoot.SetActive(false) here.
        // panelRoot is wired to this controller's own GameObject, so toggling it
        // off in Awake would re-hide the panel right after Configure activates it
        // (Awake runs synchronously the first time the GameObject is activated).
        // Initial visibility is controlled by the scene/prefab setup instead.
    }

    private void OnDestroy()
    {
        if (updateButton != null) updateButton.onClick.RemoveListener(OnUpdateClicked);
        if (notNowButton != null) notNowButton.onClick.RemoveListener(OnNotNowClicked);
        dismissTcs?.TrySetCanceled();
    }

    /// <summary>
    /// Show the panel. When isForceUpdate is true the Not Now button is hidden
    /// and the panel becomes a hard gate.
    /// </summary>
    public void Configure(bool isForceUpdate, string url, string message, string title = null)
    {
        Debug.Log($"[UpdatePanel] Configure isForce={isForceUpdate} url='{url}' panelRoot={(panelRoot != null ? panelRoot.name : "<null>")} parentActive={(panelRoot != null && panelRoot.transform.parent != null ? panelRoot.transform.parent.gameObject.activeInHierarchy.ToString() : "n/a")}");
        storeUrl = url;
        if (titleText != null && !string.IsNullOrEmpty(title))
            titleText.text = title;
        if (messageText != null && !string.IsNullOrEmpty(message))
            messageText.text = message;

        if (notNowButton != null)
            notNowButton.gameObject.SetActive(!isForceUpdate);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            ApplyMessageHeight();
            Debug.Log($"[UpdatePanel] After SetActive(true): self={panelRoot.activeSelf} inHierarchy={panelRoot.activeInHierarchy}");
        }
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeInHierarchy)
        {
            ApplyMessageHeight();
        }
    }

    private void ApplyMessageHeight()
    {
        if (messageText == null || messageScrollLayout == null) return;

        // Force a canvas update to ensure TMP has calculated its preferred height correctly based on current width
        Canvas.ForceUpdateCanvases();
        
        float contentHeight = messageText.preferredHeight;
        bool needsScroll = contentHeight > maxMessageHeight;

        float targetHeight = needsScroll ? maxMessageHeight : contentHeight;
        
        // Only update and mark for rebuild if the value actually changed to avoid layout thrashing
        if (!Mathf.Approximately(messageScrollLayout.preferredHeight, targetHeight))
        {
            messageScrollLayout.preferredHeight = targetHeight;

            if (messageScroll != null)
            {
                messageScroll.vertical = needsScroll;
                messageScroll.verticalNormalizedPosition = 1f;
            }

            // Mark the parent for rebuild so the ContentSizeFitter on the panel picks up the change
            var panelRT = panelRoot.transform as RectTransform;
            if (panelRT != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(panelRT);
            }
        }
    }

    public async UniTask AwaitDismissAsync(CancellationToken ct)
    {
        dismissTcs = new UniTaskCompletionSource();
        await dismissTcs.Task.AttachExternalCancellation(ct);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnUpdateClicked()
    {
        if (!string.IsNullOrEmpty(storeUrl))
            Application.OpenURL(storeUrl);
    }

    private void OnNotNowClicked()
    {
        Hide();
        dismissTcs?.TrySetResult();
    }
}
