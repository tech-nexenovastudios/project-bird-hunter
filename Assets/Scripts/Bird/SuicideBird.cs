using DG.Tweening;
using UnityEngine;

public class SuicideBird : MonoBehaviour
{
    public GameObject deathParticle;
    [SerializeField]private float damage;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.name);
        if (collision.transform.CompareTag(TagManager.PlayerTag))
        {
            DOTween.Kill(transform);
            GiveDamage(collision.transform.GetComponent<IHealthManager>());
            Destroy(gameObject);

        }
    }

    public void GiveDamage(IHealthManager health)
    {
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }

    private void OnDestroy()
    {
        Destroy(Instantiate(deathParticle, transform.position, Quaternion.identity), 5f);
    }
}
