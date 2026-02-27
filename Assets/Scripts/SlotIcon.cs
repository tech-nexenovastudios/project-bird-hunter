using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.UI;

public class SlotIcon : MonoBehaviour
{
    public PowerupConfig powerUp; 
    public void SetIcon(PowerupConfig power)
    {
        if (power != null)
        {
            this.gameObject.GetComponent<Image>().sprite = power.icon;
        }
        powerUp = power;
    }

    public void ApplyPowerUp()
    {
        //powerUp.GrantAbility(CannonPower.instance);
        //TODO: Apply powerup
        Debug.Log("Applying PowerUp");
        powerUp = null;
        
    }
}
