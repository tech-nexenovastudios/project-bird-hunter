using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager instance;
    [SerializeField] Abilities_SO[] abilities;
    public Abilities_SO activeAbility;
    public GameObject Player;


    public Transform abilityParent;

    public GameObject abilityDataPrefab;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

    }

    private void Start()
    {
        return;
        if (PlayerPrefs.HasKey("AbilityIndex"))
        {
            activeAbility = abilities[PlayerPrefs.GetInt("AbilityIndex")];
            //abilityParent.GetChild(PlayerPrefs.GetInt("AbilityIndex")).GetComponent<AbilityData>().equipButton.image.color = Color.green;
        }
        for(int i = 0; i < abilities.Length; i++)
        {
            int temp = i;
            abilityParent.GetChild(i).GetComponent<Image>().sprite = abilities[i].abilitySprite;
        }
        /*  foreach(var ability in abilities)
          {
              ability.powerLevel = 0;
          }
          for(int i = 0;i< abilities.Length; i++)
          {
              int temp = i;  // Using a temp variable because class work on ref so if i use value of its update every time in memory
              var data = abilityParent?.GetChild(temp)?.GetComponent<AbilityData>();
              if(data == null)
              {
                 data =  Instantiate(abilityDataPrefab, abilityParent).GetComponent<AbilityData>();  
              }
              data.titleText.text = abilities[temp].abilityName;
              data.upgradeButton.GetComponentsInChildren<TMP_Text>()[0].text = "Upgrade";
              data.equipButton.GetComponentsInChildren<TMP_Text>()[0].text = "Equip";
              data.upgradeButton.onClick.AddListener(() =>
              {
                  UpgradePower(abilities[temp],data);
              });

              data.equipButton.onClick.AddListener(() =>
              {
                  if (PlayerPrefs.HasKey("AbilityIndex"))
                  {
                      abilityParent.GetChild(PlayerPrefs.GetInt("AbilityIndex")).GetComponent<AbilityData>().equipButton.image.color = Color.white;
                  }
                  if (activeAbility == abilities[temp])
                  {
                      activeAbility = null;
                      PlayerPrefs.DeleteKey("AbilityIndex");
                      abilityParent.GetChild(temp).GetComponent<AbilityData>().equipButton.image.color = Color.white;
                  }
                  else
                  {
                      activeAbility = abilities[temp];
                      abilityParent.GetChild(temp).GetComponent<AbilityData>().equipButton.image.color = Color.green;
                      PlayerPrefs.SetInt("AbilityIndex", temp);
                  }
              });
          }*/



    }



    public void EquipPower(int temp)
    {
        Debug.Log(temp);
      /*  if (PlayerPrefs.HasKey("AbilityIndex"))
        {
            abilityParent.GetChild(PlayerPrefs.GetInt("AbilityIndex")).GetComponent<AbilityData>().equipButton.image.color = Color.white;
        }*/
        if (activeAbility == abilities[temp])
        {
            activeAbility = null;
            PlayerPrefs.DeleteKey("AbilityIndex");
           // abilityParent.GetChild(temp).GetComponent<AbilityData>().equipButton.image.color = Color.white;
        }
        else
        {
            activeAbility = abilities[temp];
           // abilityParent.GetChild(temp).GetComponent<AbilityData>().equipButton.image.color = Color.green;
            PlayerPrefs.SetInt("AbilityIndex", temp);
        }
    }

    private void UpgradePower(Abilities_SO ability, AbilityData uiData)
    {
        if (ability.powerLevel < 5)
        {
            ability.powerLevel++;
            uiData.powerLevelParent.GetChild(ability.powerLevel - 1).GetComponent<Image>().color = Color.red;
        }


    }
}
