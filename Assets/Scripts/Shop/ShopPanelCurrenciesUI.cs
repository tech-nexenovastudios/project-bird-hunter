using UnityEngine;
using TMPro;

public class ShopPanelCurrenciesUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text diamondText;
    [SerializeField] private TMP_Text cannonCardText;
    [SerializeField] private TMP_Text slotPowerCardText;

    private void Update()
    {
        if (CoinManager.instance != null)
            coinText.text = Format(CoinManager.instance.totalCoins);

        if (DiamondManager.instance != null)
            diamondText.text = Format(DiamondManager.instance.totalDiamonds);

        if (CardManager.instance != null)
        {
            cannonCardText.text = Format(CardManager.instance.GetCount(CardType.Cannon));
            slotPowerCardText.text = Format(CardManager.instance.GetCount(CardType.SlotPower));
        }
    }

    private string Format(float value)
    {
        if (value >= 1_000_000f)
            return (value / 1_000_000f).ToString("0.##") + "M";
        if (value >= 1_000f)
            return (value / 1_000f).ToString("0.##") + "K";
        return value.ToString("0");
    }
}
