using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CannonStatsManager : MonoBehaviour
{
    
    public  TMP_Text cannonName;
    public  TMP_Text cannonLevel;
    public  Image cannonImage;
    public float damage;
    public float power;
    public float fireRate;
    public int currentLevel;


    void Start()
    {
        
    }

    public void SetCannonStats(int cannonIndex)
    {
        //get cannon details from backend

    }
 
}
