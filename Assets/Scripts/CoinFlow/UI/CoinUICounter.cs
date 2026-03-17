using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CoinUICounter : MonoBehaviour
{
    [Header("═══ Which Currency ═══")]
    [SerializeField] private CurrencyType currencyType;

    [Header("═══ Animation ═══")]
    [SerializeField] private float countDuration = 1.5f;
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float punchSpeed = 8f;

    private TextMeshProUGUI label;
    private RectTransform rect;

    private int actualCoins;
    private float displayedCoins;
    private float currentScale = 1f;
    private float currentCountSpeed;

    // ── Tracks if a flow animation is currently playing ──
    // When true: coins are flying in → we animate count-up.
    // When false: a Refresh happened (no animation) → snap instantly.
    private bool isFlowActive = false;


    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
        UpdateLabel();
    }

    private void OnEnable()
    {
        GameEvent.OnCurrencyArrived += HandleCurrencyArrived;
        GameEvent.OnCurrencyBurstComplete += HandleBurstComplete;
        GameEvent.OnBalanceSynced += HandleBalanceSynced;
    }

    private void OnDisable()
    {
        GameEvent.OnCurrencyArrived -= HandleCurrencyArrived;
        GameEvent.OnCurrencyBurstComplete -= HandleBurstComplete;
        GameEvent.OnBalanceSynced -= HandleBalanceSynced;
    }


    /// <summary>
    /// Called after CurrencyManager.Refresh() finishes.
    /// This is the REAL balance from the server.
    /// </summary>
    private void HandleBalanceSynced(CurrencyType type, int serverBalance)
    {
        if (type != currencyType) return;

        if (isFlowActive)
        {
            // Flow animation is playing — let it finish naturally.
            // The animated count-up is already heading toward the
            // correct value via HandleCurrencyArrived.
            // Just update actual in case of rounding differences.
            actualCoins = serverBalance;
        }
        else
        {
            // No animation playing — this is a fresh load or
            // a background refresh. Snap to server value instantly.
            actualCoins = serverBalance;
            displayedCoins = serverBalance;
            currentCountSpeed = 0f;
            UpdateLabel();
        }
    }


    private void HandleCurrencyArrived(CurrencyType type, int value)
    {
        if (type != currencyType) return;

        isFlowActive = true;
        actualCoins += value;
        currentScale = punchScale;

        float gap = actualCoins - displayedCoins;
        if (gap > 0)
            currentCountSpeed = gap / countDuration;
    }


    private void HandleBurstComplete(CurrencyType type)
    {
        if (type != currencyType) return;

        float gap = actualCoins - displayedCoins;
        if (gap > 0)
            currentCountSpeed = gap / countDuration;

        // Flow is done — next BalanceSynced can snap freely
        isFlowActive = false;
    }


    private void Update()
    {
        if (displayedCoins < actualCoins)
        {
            displayedCoins += currentCountSpeed * Time.deltaTime;
            if (displayedCoins >= actualCoins)
                displayedCoins = actualCoins;
            UpdateLabel();
        }

        if (currentScale > 1f)
        {
            currentScale = Mathf.Lerp(currentScale, 1f, punchSpeed * Time.deltaTime);
            if (Mathf.Abs(currentScale - 1f) < 0.01f)
                currentScale = 1f;
            rect.localScale = Vector3.one * currentScale;
        }
    }


    public void SetCoins(int amount)
    {
        actualCoins = amount;
        displayedCoins = amount;
        currentCountSpeed = 0f;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        label.text = FormatNumber(Mathf.FloorToInt(displayedCoins));
    }


    /// <summary>
    /// Formats large numbers into readable short form.
    ///   999      → "999"
    ///   1,200    → "1.2K"
    ///   15,800   → "15.8K"
    ///   400,000  → "400K"
    ///   1,500,000 → "1.5M"
    ///   2,300,000,000 → "2.3B"
    /// </summary>
    private string FormatNumber(int value)
    {
        if (value < 0)
            return "-" + FormatNumber(-value);

        if (value < 1000)
            return value.ToString();

        if (value < 1_000_000)
        {
            float k = value / 1000f;
            // Show decimal only if it's meaningful (1.2K not 15.0K)
            return k < 10f
                ? k.ToString("0.0") + "K"      // 1.2K, 9.8K
                : k < 100f
                    ? k.ToString("0.0") + "K"  // 15.8K, 99.9K
                    : k.ToString("0") + "K";   // 100K, 999K
        }

        if (value < 1_000_000_000)
        {
            float m = value / 1_000_000f;
            return m < 10f
                ? m.ToString("0.0") + "M"      // 1.4M, 9.9M
                : m < 100f
                    ? m.ToString("0.0") + "M"  // 15.2M, 99.9M
                    : m.ToString("0") + "M";   // 100M, 999M
        }

        float b = value / 1_000_000_000f;
        return b < 10f
            ? b.ToString("0.0") + "B"          // 1.3B, 9.9B
            : b.ToString("0.0") + "B";         // 10.0B+
    }
}