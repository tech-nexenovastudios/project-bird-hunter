using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Rocket_Ability : MonoBehaviour
{
    public float Damage;
    public LayerMask layerMask;
    public float Speed;
    List<Collider2D> nearObjList;



    public void Start()
    {
        StartCoroutine(ScanAndHit());
    }
    IEnumerator ScanAndHit()
    {

            nearObjList = Physics2D.OverlapCircleAll(transform.position, 20, layerMask).ToList();
            nearObjList = nearObjList
                .Where(obj => obj != null) // filter out already destroyed objects
                .OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position))
                .ToList();

            if (nearObjList.Count > 0)
            {
                Transform targetTransform = nearObjList[0].transform;

                // Move bullet toward target safely
                while (targetTransform != null && Vector2.Distance(transform.position, targetTransform.position) > 0.2f)
                {
                    Vector2 direction = (targetTransform.position - transform.position);
                    transform.position += (Vector3)(direction.normalized * Speed * Time.deltaTime);
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle - 90);
                    yield return null;
                }

               
                if (targetTransform != null)
                {
                    var target = targetTransform.GetComponent<IHealthManager>();
                    if (target != null)
                        target.TakeDamage(Damage);
                }
            

        }
        Destroy(gameObject);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 20f);
    }


}
