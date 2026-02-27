using UnityEngine;
using UnityEngine.UI;

public class PowerUpgradeManager : MonoBehaviour
{
    public Transform powerParent;
    public SlotPowerHolder_SO powerUp;
    public Button[] buttons;


 
    private void Start()
    {

        for (int i = 0; i < powerUp.powerUp.Length; i++)
        {
            int temp = i;
            buttons[temp].name = powerUp.powerUp[i].name;
            buttons[temp].transform.GetChild(0).GetComponent<Image>().sprite = powerUp.powerUp[i].iconSprite;
            buttons[temp].onClick.AddListener(() =>
            {
                powerUp.powerUp[temp].UpgradePower();
                buttons[temp].transform.GetChild(1).GetChild(powerUp.powerUp[temp].powerLevel).GetComponent<Image>().color = Color.red;
            });
        }
    }
}
