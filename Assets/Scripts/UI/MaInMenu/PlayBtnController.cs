using UnityEngine;

public class PlayBtnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChapterSelector chapterSelector;
    private int powerRequired = 5;
    public TMPro.TextMeshProUGUI powerCostText;


    public void OnPlayButtonPressed()
    {
        if (CurrencyManager.Instance.CanAffordPower(powerRequired))
        {
            CurrencyManager.Instance.SpendPower(powerRequired);
            chapterSelector.LoadCurrentWorld();
        }
        else
        {
            powerCostText.text = "Not enough <sprite=\"Nove SDF Sprites\" name=power>!";
        }
            
    }
}