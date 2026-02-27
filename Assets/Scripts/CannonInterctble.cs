using UnityEngine;

public class CannonInterctble : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
     
        if(collision.transform.tag == "Interctble")
        {
            var collect = collision.transform.GetComponent<IInteract>();
            if (collect != null)
            {
                collect.Interact();
            }
           
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.tag == "Interctble")
        {
            var collect = collision.transform.GetComponent<IInteract>();
            if (collect != null)
            {
                collect.Interact();
            }
               
        }
    }
}
