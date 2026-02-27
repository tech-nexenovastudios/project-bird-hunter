using UnityEngine;

public class TornadoDamage : MonoBehaviour
{
    [SerializeField] float tornadoDamage;

    private void OnEnable()
    {
       this.GetComponent<Collider2D>().enabled = true;
    }

    private void OnDisable()
    {
       
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(TagManager.PlayerTag))
        {
            collision.GetComponent<IHealthManager>().TakeDamage(tornadoDamage);
            this.GetComponent<Collider2D>().enabled = false;
        }
    }
}
