using System.Collections;
using UnityEngine;

public class IceBird : MonoBehaviour
{
    [SerializeField] GameObject freezeEgg;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(TagManager.PlayerTag))
        {
            collision.GetComponent<IDamageEffect>().FreezeEffect(4f);
        }
    }
    private void OnEnable()
    {
        StartCoroutine(SpawnEggs());
    }

    IEnumerator SpawnEggs()
    {
        while (true)
        {
            yield return new WaitForSeconds(2);
            GameObject go = Instantiate(freezeEgg, transform.position, Quaternion.identity);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
