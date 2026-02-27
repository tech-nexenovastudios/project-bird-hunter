using System.Collections.Generic;
using UnityEngine;

public class RadiusDamage : MonoBehaviour
{
    //This script you can use for radius damage or any type of damge you want to give
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //Here you can give every object which you want to give damage
    [SerializeField]List<string> objectTag = new();
    [SerializeField] float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        foreach(string tag in objectTag)
        {
            if (collision.transform.CompareTag(tag))
            {
                this.GetComponent<Collider2D>().enabled = false;
                collision.transform.GetComponent<IHealthManager>().TakeDamage(damage);
            }
        }
    }
}
