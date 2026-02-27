using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RocketThrower : MonoBehaviour
{
    public GameObject rocketBulletPrefab;
    public float DelayTime;
    public float Damage;
    public LayerMask layerMask;
    public float Speed;
    List<Collider2D> nearObjList;
    public CannonFire cannonFire;


    public void Start()
    {
        StartCoroutine(ScanAndHit());
    }
    IEnumerator ScanAndHit()
    {
        GameObject go = Instantiate(rocketBulletPrefab, transform.position, Quaternion.identity);
        go.SetActive(false);

        while (true)
        {
            yield return new WaitForSeconds(DelayTime);

            nearObjList = Physics2D.OverlapCircleAll(transform.position, 3f, layerMask).ToList();
            nearObjList = nearObjList
                .Where(obj => obj != null) // filter out already destroyed objects
                .OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position))
                .ToList();

            if (nearObjList.Count > 0)
            {
                Transform targetTransform = nearObjList[0].transform;
                go.transform.position = transform.position;
                go.SetActive(true);

                // Move bullet toward target safely
                while (targetTransform != null && Vector2.Distance(go.transform.position, targetTransform.position) > 0.2f)
                {
                    Vector2 direction = (targetTransform.position - go.transform.position);
                    go.transform.position += (Vector3)(direction.normalized * Speed * Time.deltaTime);
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    go.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
                    yield return null;
                }

                // Apply poison if target is still valid
                if (targetTransform != null)
                {
                    var target = targetTransform.GetComponent<IHealthManager>();
                    if (target != null)
                        target.TakeDamage(Damage*cannonFire.Damage*cannonFire.shotsPerSecond);
                }

                go.SetActive(false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
