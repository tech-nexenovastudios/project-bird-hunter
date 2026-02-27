using System.Collections;
using UnityEngine;

public class GravityUseAbility : MonoBehaviour,IAbilityUse
{
    public GameObject gravitySuckPrefab;
    private GameObject gravitySuckPrefabRef;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float duration = 3f;

    public int abilityLevel { get; set; }
    public bool canUseAbility { get; set; }

    public void UseAbility()
    {
       
       // var hit =  Physics2D.Raycast(transform.position, Vector3.down, 5f,LayerManager.GroundMask());
      //  Debug.Log(hit.point);
        if (gravitySuckPrefabRef == null)
        {
            gravitySuckPrefabRef = Instantiate(gravitySuckPrefab);
        }
        //gravitySuckPrefab.transform.position = new Vector2(hit.point.x,hit.point.y);
        gravitySuckPrefabRef.transform.position = transform.parent.parent.position;

        gravitySuckPrefabRef.SetActive(true);
        Invoke("TimeEnd", duration);

    }

    public void TimeEnd()
    {
        gravitySuckPrefabRef.SetActive(false);
    }

}
