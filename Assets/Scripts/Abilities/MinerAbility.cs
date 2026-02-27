using DG.Tweening;
using UnityEngine;
public class MinerAbility : MonoBehaviour,IAbilityUse
{
    public Transform parent;
    public GameObject minePrefab;
    public LayerMask groundMask;
    public float damage;
    public int abilityLevel { get; set; }
    public bool canUseAbility { get; set; }



    public void UseAbility()
    {
        var rayHit = Physics2D.Raycast(transform.position,transform.position+Vector3.down,2.5f,groundMask);
        var mine = Instantiate(minePrefab, transform.position,Quaternion.identity);
        mine.GetComponent<Mine>().damage = damage;
        Debug.Log(rayHit.point);
        mine.transform.DOLocalMoveY(rayHit.point.y, 20f).SetSpeedBased();
    }

/*    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 3f);
    }*/
}
