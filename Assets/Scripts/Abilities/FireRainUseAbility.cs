using System.Collections;
using UnityEngine;

public class FireRainUseAbility : MonoBehaviour,IAbilityUse
{
    public GameObject fireRainPrefab;
    private GameObject fireRainPrefabRef;
    public float[] damagePerSecond;

    FireRain fireRain;
    public int abilityLevel { get; set; }
    public bool canUseAbility { get ; set; }

    public void Start()
    {
      
    }

    public void UseAbility()
    {
        if (fireRainPrefabRef == null)
        {
            fireRainPrefabRef = Instantiate(fireRainPrefab);
            fireRain = fireRainPrefabRef.GetComponent<FireRain>();
            fireRain.damage = damagePerSecond[0];
        }
        fireRainPrefabRef.transform.position = new Vector2(transform.parent.parent.position.x, 7f);
        fireRainPrefabRef.SetActive(true);
        StartCoroutine(DisableRain());
       
    }

    IEnumerator DisableRain()
    {
        yield return new WaitForSeconds(3f);
        fireRainPrefabRef.SetActive(false);
    }

}
