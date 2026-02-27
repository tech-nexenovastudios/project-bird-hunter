using UnityEngine;

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
        EggHealth health = collision.GetComponent<EggHealth>();
        if (health != null)
        {
            if(health.Health < Damage)
            {
                health.TakeDamage(Damage);
            }
            else
            {
                health.TakeDamage(Damage);
                Destroy(gameObject);
            }
        }
       
    }
}
