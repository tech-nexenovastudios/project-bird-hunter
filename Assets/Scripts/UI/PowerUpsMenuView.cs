
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpsMenuView : MonoBehaviour, IMenuPage
{
    public PageType PageType => PageType.PowerUps;

    [Header("Sub-Panels & Popups (will be hidden on page enter)")]
    [Tooltip("All popup/overlay panels in this page that should be hidden when the page opens.")]
    [SerializeField] private GameObject[] popupsToHide;

    [Header("Scroll Rects (will be reset to top)")]
    [SerializeField] private ScrollRect[] scrollRects;



    private CancellationTokenSource _cts;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _cts = new CancellationTokenSource();
    }

    private void Start()
    {
        PageManager.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (PageManager.Instance != null)
            PageManager.Instance.Unregister(this);
    }

    // ── IMenuPage Contract ───────────────────────────────────────────

    public void OnPageEnter()
    {
        Debug.Log("[ShopMenuView] OnPageEnter — resetting state");
        ResetToDefaultState();
    }

    public void OnPageExit()
    {
        Debug.Log("[ShopMenuView] OnPageExit — cleanup");
        CleanupOnExit();
    }

    // ── Reset Implementation ─────────────────────────────────────────

    private void ResetToDefaultState()
    {
        // 1. Stop any active DOTween animations on this hierarchy
        PageResetHelper.KillTweens(transform, complete: false);

        // 2. Cancel any in-flight async work
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // 3. Hide all popups/overlays
        if (popupsToHide != null)
        {
            foreach (var popup in popupsToHide)
                if (popup != null) popup.SetActive(false);
        }

        // 4. Reset all scroll positions to top
        if (scrollRects != null)
        {
            foreach (var sr in scrollRects)
            {
                if (sr == null) continue;
                sr.verticalNormalizedPosition = 1f;
                //sr.horizontalNormalizedPosition = 0f;
            }
        }
        // Also catch any ScrollRects we didn't manually assign
        PageResetHelper.ResetScrolls(transform);



    }

    private void CleanupOnExit()
    {
        // Stop animations cleanly so they don't continue running off-screen
        PageResetHelper.KillTweens(transform, complete: false);

        // Cancel any pending async operations
        _cts?.Cancel();
    }
}