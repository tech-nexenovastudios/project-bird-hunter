using UnityEngine;

public class ElectricField : MonoBehaviour
{
    [SerializeField] LayerMask groundMask;
    public float damage;

    Vector2 intialPos;
    private void Start()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, groundMask);

        if (hit.collider != null)
        {
            intialPos = hit.point;
            transform.position = new Vector2(0, intialPos.y);
        }
     
    }

    public void ResetValue()
    {
        transform.position = new Vector2(0, intialPos.y);
    }

    private void Update()
    {
        transform.position += transform.up * 5 * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.transform.CompareTag("Player"))
        {
            var health = collision.GetComponent<IHealthManager>();
            if(health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}
