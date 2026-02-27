using UnityEngine;

public class Diamon : MonoBehaviour, IInteract
{
    [SerializeField] int diamondValue;
    private void OnCollisionEnter2D(Collision2D collision)
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
    }
    public void Interact()
    {
        DiamondManager.instance.totalDiamonds += diamondValue;
        LevelManager.Instance.collectDiamond += diamondValue;
        Destroy(gameObject);
    }
}
