using UnityEngine;
using Gameplay.Interfaces;

public class BigBullet : MonoBehaviour
{
    public float Damage;
    public float speed;

    private void Update()
    {
        transform.position += transform.up * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector3 hitPoint = collision.ClosestPoint(transform.position);
        var damageable = collision.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive)
        {
            damageable.TakeDamage((int)Damage);
            Destroy(gameObject);
        }
        else
        {
            var legacyHealth = collision.GetComponent<IHealthManager>();
            if (legacyHealth != null)
            {
                legacyHealth.TakeDamage(Damage);
                Destroy(gameObject);
            }
        }
    }
}
