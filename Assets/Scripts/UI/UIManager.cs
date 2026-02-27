using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] TMP_Text coin_Text, diamond_Text;

  
    private void OnEnable()
    {
        CoinManager.OnCoinsChanged += CoinUpdate;
        DiamondManager.OnDiamondChanged += DiamondChange;
    }
    private void OnDisable()
    {
        CoinManager.OnCoinsChanged -= CoinUpdate;
        DiamondManager.OnDiamondChanged -= DiamondChange;
    }

    private void CoinUpdate(float value)
    {
        coin_Text.text = ""+value.ToString();  
    }

    private void DiamondChange(float value)
    {
        diamond_Text.text = ""+value.ToString();
    }
}
