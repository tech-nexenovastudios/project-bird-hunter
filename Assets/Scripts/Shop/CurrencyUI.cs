using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Texts")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI powerText;

    private bool _hasStarted = false;

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += OnCurrencyChanged;
        EventBus.Subscribe<CurrencyLoadedEvent>(OnCurrencyLoaded);

        // Only refresh in OnEnable if Start has already run (i.e., this is a re-enable).
        // First-time paint happens in Start — see note below.
        if (_hasStarted && CurrencyManager.Instance.IsLoaded)
            RefreshAll();
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= OnCurrencyChanged;
        EventBus.Unsubscribe<CurrencyLoadedEvent>(OnCurrencyLoaded);
    }

    private void Start()
    {
        // Paint cache here — by Start(), TMP components are fully initialized.
        // Painting in OnEnable before Start can fail silently due to TMP init timing.
        _hasStarted = true;

        if (CurrencyManager.Instance.IsLoaded)
            RefreshAll();
    }

    private void OnCurrencyLoaded(CurrencyLoadedEvent evt)
    {
        RefreshAll();
    }

    private void OnCurrencyChanged(CurrencyType type, long newValue)
    {
        switch (type)
        {
            case CurrencyType.Gold: SetText(goldText, newValue); break;
            case CurrencyType.Gems: SetText(gemsText, newValue); break;
            case CurrencyType.Power: SetText(powerText, newValue); break;
        }
    }

    private void RefreshAll()
    {
        SetText(goldText, CurrencyManager.Instance.Gold);
        SetText(gemsText, CurrencyManager.Instance.Gems);
        SetText(powerText, CurrencyManager.Instance.Power);
    }

    private void SetText(TextMeshProUGUI text, long value)
    {
        if (text != null) text.text = FormatCurrency(value);
    }

    private string FormatCurrency(long value)
    {
        if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.##") + "M";
        if (value >= 1_000) return (value / 1_000f).ToString("0.##") + "K";
        return value.ToString();
    }
}