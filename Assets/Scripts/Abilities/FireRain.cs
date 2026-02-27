using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class FireRain : MonoBehaviour
{
    public float damage;

    List<IHealthManager> healthManager = new();


    private void OnTriggerStay2D(Collider2D collision)
    {
        if(healthManager.Count > 0)
        {
            foreach(IHealthManager health in healthManager)
            {
                health.TakeDamage(damage*Time.deltaTime);
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("Egg"))
        {
            healthManager.Add(collision.GetComponent<IHealthManager>()); 
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("Egg"))
        {
            healthManager.Remove(collision.GetComponent<IHealthManager>());
        }
    }
}
