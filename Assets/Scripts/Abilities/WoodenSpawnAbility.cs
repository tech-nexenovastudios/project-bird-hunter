using UnityEngine;

public class WoodenSpawnAbility : MonoBehaviour,IAbilityUse
{
    public GameObject woodenPrefab;
    private GameObject woodenPrefabRef;

    public int abilityLevel { get; set; }
    public bool canUseAbility { get; set; }

    public void UseAbility()
    {
      if(woodenPrefabRef == null)
      {
           woodenPrefabRef = Instantiate(woodenPrefab);
      }
        woodenPrefabRef.SetActive(true);
        Invoke("EndTime", 5f);
    }

    public void EndTime()
    {
        woodenPrefabRef.SetActive(false);
    }
}
