using UnityEngine;

public class Coin : MonoBehaviour, IInteract
{
    [SerializeField] float coinValue;
 /*   private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Wall"))
        {
            Vector2 Normal = new Vector2();
            foreach (var col in collision.contacts)
            {
                Normal += col.normal;
            }
            transform.GetComponent<Rigidbody2D>().AddForce(Normal * 2f, ForceMode2D.Impulse);

        }
    }*/
    public void Interact()
    {
       
        CoinManager.instance.totalCoins += coinValue;
        LevelManager.Instance.collectCoin += coinValue;
        Destroy(gameObject);
    }
}
