using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Mine : MonoBehaviour
{
    public float damage;
    [SerializeField] private string Tag;
    [SerializeField] private ParticleSystem blastParticle;
    [SerializeField] private GameObject groundBlast;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(Tag))
        {
            var health = collision.GetComponent<IHealthManager>();
            if (health != null)
            {
                collision.GetComponent<IHealthManager>().TakeDamage(damage);
            }
            Destroy(Instantiate(blastParticle.gameObject, transform.position, Quaternion.identity), 3);

            Destroy(this.gameObject);
        }

        if (collision.transform.CompareTag("Ground"))
        {
            //Destroy(Instantiate(groundBlast, transform.position, Quaternion.identity), 3);

            var bigBlast = Instantiate(groundBlast, transform.position, Quaternion.identity);
            bigBlast.transform.localScale = Vector3.zero;
            bigBlast.transform.DOScale(Vector3.one * ScreenBounds.maxX, 0.2f).SetEase(Ease.Linear).OnComplete(() =>
            {
                Destroy(bigBlast, 2f);
            });

            Debug.Log(ScreenBounds.maxX);
            Destroy(this.gameObject);
        }
    }
}
