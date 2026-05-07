using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdatePanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI messageText;

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
    public void Configure(bool isForceUpdate, string url, string message)
    {
        Debug.Log($"[UpdatePanel] Configure isForce={isForceUpdate} url='{url}' panelRoot={(panelRoot != null ? panelRoot.name : "<null>")} parentActive={(panelRoot != null && panelRoot.transform.parent != null ? panelRoot.transform.parent.gameObject.activeInHierarchy.ToString() : "n/a")}");
        storeUrl = url;
        if (messageText != null && !string.IsNullOrEmpty(message))
            messageText.text = message;

        if (notNowButton != null)
            notNowButton.gameObject.SetActive(!isForceUpdate);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Debug.Log($"[UpdatePanel] After SetActive(true): self={panelRoot.activeSelf} inHierarchy={panelRoot.activeInHierarchy}");
        }
    }

    /// <summary>
    /// Awaits the user clicking Not Now. For a force update the panel never
    /// dismisses on its own — the caller should not await this in that case.
    /// </summary>
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
