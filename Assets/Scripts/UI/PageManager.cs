using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PageType
{
    MainMenu,
    Inventory,
    PowerUps,
    Events,
    ShopMenu,
}

/// <summary>
/// Stack-based page navigation. The top of the stack is the visible page.
/// Pages self-register via <see cref="Register"/>; the stack drives the
/// horizontal slide between pre-placed page slots.
/// </summary>
public class PageManager : MonoBehaviour
{
    public static PageManager Instance { get; private set; }

    [Serializable]
    public class PageSlot
    {
        public PageType type;
        public GameObject root;   // GameObject to SetActive on transition
    }

    [Header("Layout")]
    [Tooltip("Parent rect that slides horizontally to reveal the current page.")]
    [SerializeField] private RectTransform pageRect;
    [Tooltip("Horizontal distance between page slots (usually screen width).")]
    [SerializeField] private float slideDistance = 1080f;
    [SerializeField] private float animationTime = 0.3f;

    [Header("Pages")]
    [Tooltip("List position == slide order. Leftmost slot is index 0.")]
    [SerializeField] private List<PageSlot> pages = new();
    [Tooltip("The permanent bottom-of-stack page. You can never pop below this.")]
    [SerializeField] private PageType rootPage = PageType.MainMenu;

    // ── Runtime ────────────────────────────────────────────────────────
    private readonly Dictionary<PageType, PageSlot>   _byType     = new();
    private readonly Dictionary<PageType, int>        _slotIndex  = new();
    private readonly Dictionary<PageType, IMenuPage>  _registered = new();
    private readonly Stack<PageType>                  _stack      = new();

    private Coroutine _setPageCoroutine;
    private float _lastNavTime;
    private int _centerIndex;
    private bool _initialized;

    /// <summary>Top of the stack. What the user currently sees.</summary>
    public PageType CurrentPage => _stack.Count > 0 ? _stack.Peek() : rootPage;
    public int StackDepth => _stack.Count;

    /// <summary>Args: (from, to). Fires after every transition.</summary>
    public event Action<PageType, PageType> OnPageChanged;

    // ════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < pages.Count; i++)
        {
            var slot = pages[i];
            if (slot == null || slot.root == null) continue;
            if (_byType.ContainsKey(slot.type))
            {
                Debug.LogWarning($"[PageManager] Duplicate slot for {slot.type} — ignoring second entry.");
                continue;
            }
            _byType[slot.type] = slot;
            _slotIndex[slot.type] = i;
        }

        _centerIndex = _slotIndex.TryGetValue(rootPage, out var c) ? c : (pages.Count > 0 ? pages.Count / 2 : 0);
    }

    private void Start()
    {
        _stack.Push(rootPage);

        int idx = _slotIndex.TryGetValue(rootPage, out var ri) ? ri : _centerIndex;
        if (pageRect != null)
            pageRect.anchoredPosition = new Vector2(-slideDistance * (idx - _centerIndex), pageRect.anchoredPosition.y);
        DeactivateAllExcept(idx);

        _initialized = true;
        if (_registered.TryGetValue(rootPage, out var p)) p.OnPageEnter();
        OnPageChanged?.Invoke(rootPage, rootPage);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════════════════════════════════
    // Registration — pages call these in OnEnable / OnDisable
    // ════════════════════════════════════════════════════════════════════

    public void Register(IMenuPage page)
    {
        if (page == null) return;
        _registered[page.PageType] = page;
        if (_initialized && CurrentPage == page.PageType) page.OnPageEnter();
    }

    public void Unregister(IMenuPage page)
    {
        if (page == null) return;
        if (_registered.TryGetValue(page.PageType, out var existing) && existing == page)
            _registered.Remove(page.PageType);
    }

    public bool TryGetPage<T>(PageType type, out T page) where T : class, IMenuPage
    {
        if (_registered.TryGetValue(type, out var p) && p is T typed) { page = typed; return true; }
        page = null;
        return false;
    }

    // ════════════════════════════════════════════════════════════════════
    // Stack API — push / pop / replace / popToRoot
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Push a page on top. Previous page stays underneath.</summary>
    public void PushPage(PageType type)
    {
        if (!_byType.ContainsKey(type))      { Debug.LogWarning($"[PageManager] No slot for {type}."); return; }
        if (_stack.Count > 0 && _stack.Peek() == type) return;

        var from = CurrentPage;
        _stack.Push(type);
        Transition(from, type);
    }

    /// <summary>Pop the top page. Returns false if only the root remains.</summary>
    public bool PopPage()
    {
        if (_stack.Count <= 1) return false;
        var from = _stack.Pop();
        var to   = _stack.Peek();
        Transition(from, to);
        return true;
    }

    /// <summary>Swap the top page without changing stack depth (tab-switch semantics).</summary>
    public void ReplacePage(PageType type)
    {
        if (!_byType.ContainsKey(type)) { Debug.LogWarning($"[PageManager] No slot for {type}."); return; }
        if (_stack.Count == 0) { PushPage(type); return; }
        if (_stack.Peek() == type) return;

        var from = _stack.Pop();
        _stack.Push(type);
        Transition(from, type);
    }

    /// <summary>Pop everything except the root page.</summary>
    public void PopToRoot()
    {
        if (_stack.Count <= 1) return;
        var from = _stack.Peek();
        while (_stack.Count > 1) _stack.Pop();
        Transition(from, _stack.Peek());
    }

    /// <summary>True if this page is anywhere in the stack.</summary>
    public bool Contains(PageType type) => _stack.Contains(type);

    /// <summary>Read-only snapshot of the stack (top-first). Useful for debugging.</summary>
    public PageType[] GetStackSnapshot() => _stack.ToArray();

    // ════════════════════════════════════════════════════════════════════
    // Backwards compat — existing button hookups calling UpdatePage(int)
    // ════════════════════════════════════════════════════════════════════

    public void UpdatePage(int slideOffset)
    {
        int absIndex = _centerIndex + slideOffset;
        if (absIndex < 0 || absIndex >= pages.Count)
        {
            Debug.LogWarning($"[PageManager] UpdatePage offset {slideOffset} out of range.");
            return;
        }
        ReplacePage(pages[absIndex].type);
    }

    // ════════════════════════════════════════════════════════════════════
    // Core transition
    // ════════════════════════════════════════════════════════════════════

    private void Transition(PageType from, PageType to)
    {
        if (_registered.TryGetValue(from, out var fromPage)) fromPage.OnPageExit();

        // Flush pending deactivation so OnEnable/OnDisable fire reliably on rapid nav.
        if (_setPageCoroutine != null)
        {
            StopCoroutine(_setPageCoroutine);
            _setPageCoroutine = null;
        }

        _lastNavTime = Time.time;

        // Activate source, target, and everything in between for the slide.
        int fromIdx = _slotIndex.TryGetValue(from, out var fi) ? fi : _centerIndex;
        int toIdx   = _slotIndex[to];
        int lo = Mathf.Min(fromIdx, toIdx);
        int hi = Mathf.Max(fromIdx, toIdx);
        for (int i = lo; i <= hi; i++)
            if (pages[i]?.root != null) pages[i].root.SetActive(true);

        if (pageRect != null)
        {
            float x = -slideDistance * (toIdx - _centerIndex);
            pageRect.DOAnchorPos(new Vector2(x, pageRect.anchoredPosition.y), animationTime);
        }

        _setPageCoroutine = StartCoroutine(FinalizeActivation(toIdx));

        if (_registered.TryGetValue(to, out var toPage)) toPage.OnPageEnter();
        OnPageChanged?.Invoke(from, to);
    }

    private IEnumerator FinalizeActivation(int targetIndex)
    {
        yield return new WaitForSeconds(animationTime + 0.5f);
        if (Time.time - _lastNavTime < animationTime + 0.5f) yield break;
        DeactivateAllExcept(targetIndex);
        _setPageCoroutine = null;
    }

    private void DeactivateAllExcept(int keepIndex)
    {
        for (int i = 0; i < pages.Count; i++)
            if (pages[i]?.root != null) pages[i].root.SetActive(i == keepIndex);
    }
}
