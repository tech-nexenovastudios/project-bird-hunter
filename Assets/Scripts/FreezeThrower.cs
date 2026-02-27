using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FreezeThrower : MonoBehaviour
{
    public GameObject poisonBulletPrefab;
    public float DelayTime;
    public LayerMask layerMask;
    public float Speed;
    public float effectDuration;
    public List<Collider2D> nearObjList;



    public void Start()
    {
        StartCoroutine(ScanAndHit());
    }
    IEnumerator ScanAndHit()
    {
        GameObject go = Instantiate(poisonBulletPrefab, transform.position, Quaternion.identity);
        go.SetActive(false);

        while (true)
        {
            yield return new WaitForSeconds(DelayTime);
            nearObjList = Physics2D.OverlapCircleAll(transform.position, 8f, layerMask).ToList();
            nearObjList = nearObjList.OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position)).ToList();

            if (nearObjList.Count > 0 && nearObjList[0] != null)
            {
                Transform targetTransform = nearObjList[0].transform;
                go.transform.position = transform.position;
                go.SetActive(true);

                // Move toward the target
                while (targetTransform != null && Vector2.Distance(go.transform.position, targetTransform.position) > 0.2f)
                {
                    Vector2 direction = (targetTransform.position - go.transform.position);
                    go.transform.position += (Vector3)(direction.normalized * Speed * Time.deltaTime);
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    go.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
                    yield return null;
                }

                // Apply effect if still valid
                if (targetTransform != null)
                {
                    var target = targetTransform.GetComponent<IDamageEffect>();
                    if (target != null)
                        target.FreezeEffect(effectDuration);
                }

                go.SetActive(false);
                nearObjList.Clear();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 8f);
    }
}
