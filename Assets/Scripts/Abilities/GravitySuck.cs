using DG.Tweening;
using UnityEngine;

public class GravitySuck : MonoBehaviour
{
    public Transform portal;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == "Egg")
        {
            collision.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            collision.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            collision.GetComponent<Collider2D>().enabled = false;
            collision.transform.DOMove(portal.position, 0.2f).OnComplete(() =>
            {
                collision.transform.GetComponent<IHealthManager>().TakeDamage(float.MaxValue);
            });
        }
    }
}
