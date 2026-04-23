using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// Attach to a UI GameObject. Displays currency values and auto-updates on changes.
/// Loads balances on Start if not already loaded.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Header("Currency Texts")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI powerText;

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += OnCurrencyChanged;
        if (!CurrencyManager.Instance.IsLoaded)
            LoadCurrency().Forget();
        else
            RefreshAll();
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= OnCurrencyChanged;
    }

    private async UniTaskVoid LoadCurrency()
    {
        await CurrencyManager.Instance.LoadBalances(forceReload: true); 
        RefreshAll();
    }

    private void OnCurrencyChanged(CurrencyType type, long newValue)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                SetText(goldText, newValue);
                break;
            case CurrencyType.Gems:
                SetText(gemsText, newValue);
                break;
            case CurrencyType.Power:
                SetText(powerText, newValue);
                break;
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
        if (text != null)
            text.text = FormatCurrency(value);
    }

    private string FormatCurrency(long value)
    {
        if (value >= 1_000_000)
            return (value / 1_000_000f).ToString("0.##") + "M";
        if (value >= 1_000)
            return (value / 1_000f).ToString("0.##") + "K";
        return value.ToString();
    }
}