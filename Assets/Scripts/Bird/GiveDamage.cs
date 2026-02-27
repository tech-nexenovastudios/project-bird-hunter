using System.Collections.Generic;
using UnityEngine;

public class GiveDamage : MonoBehaviour
{
    [SerializeField] string Tag;
    [SerializeField] float damage;
    List<IHealthManager> healthManagers = new List<IHealthManager>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(Tag))
        {
            var health = collision.GetComponent<IHealthManager>();
            if (health != null)
            {
                if (!healthManagers.Contains(health))
                {
                    healthManagers.Add(health);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(Tag))
        {
            foreach (var health in healthManagers)
            {
                health.TakeDamage(damage * Time.deltaTime);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.CompareTag(Tag))
        {
            var health = collision.GetComponent<IHealthManager>();
            if (health != null)
            {
                if (healthManagers.Contains(health))
                {
                    healthManagers.Remove(health);
                }
            }
        }
    }
}
