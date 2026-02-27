using DG.Tweening;
using UnityEngine;

public class BirdBullet : MonoBehaviour
{
    [SerializeField] float speed = 30f;
    [SerializeField] float damage = 5f;
    private void Start()
    {
        var rayHit = Physics2D.Raycast(transform.position, -transform.up, float.MaxValue, LayerManager.GroundMask);
        transform.DOMove(rayHit.point, 40).SetSpeedBased().OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(TagManager.PlayerTag))
        {
            DOTween.Kill(transform);
         
            var health = collision.GetComponent<IHealthManager>();
            if(health != null)
            {
                Destroy(gameObject);
                health.TakeDamage(damage);
            }
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, -transform.up * 50f);
    }
}
