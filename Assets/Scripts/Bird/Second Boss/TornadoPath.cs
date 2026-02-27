using DG.Tweening;
using UnityEngine;

public class TornadoPath : MonoBehaviour
{
    public LayerMask groundLayer; // assign in inspector

    BoxCollider2D boxCollider;
    float halfWidth;

    public int remainingBounces;
    bool goingRight;

    [SerializeField]float damageToPlayer = 3;


    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        halfWidth = boxCollider.bounds.extents.x;

        remainingBounces = Random.Range(3, 6); // number of zigzag steps before final
        goingRight = true;

        DoNextStep();
        
    }

    void DoNextStep()
    {
      
       
        // Find ground Y under us
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, LayerManager.GroundMask);
        float groundY = hit.collider ? hit.point.y + boxCollider.bounds.extents.y : -Mathf.Infinity;

        // if we’re already at or below ground, stop
        if (transform.position.y <= groundY + 0.05f)
            return;

        // pick how far to go down this segment
        float stepDown = Random.Range(1.5f, 3.0f);
        float targetY = Mathf.Max(groundY, transform.position.y - stepDown);

        float targetX;

        if (remainingBounces > 0)
        {
            // zigzag between boundaries
            targetX = goingRight ? ScreenBounds.maxX - halfWidth : ScreenBounds.minX + halfWidth;
            goingRight = !goingRight;
        }
        else
        {
            // final drift to random x
            targetX = Random.Range(ScreenBounds.minX + halfWidth, ScreenBounds.maxX - halfWidth);
     

        }

        Vector2 targetPos = new Vector2(targetX, targetY);

        // constant speed: compute time based on distance
        float distance = Vector2.Distance(transform.position, targetPos);
        float speed = 5f; // units per second
        float duration = distance / speed;

        transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear) // linear movement
            .OnComplete(() =>
            {
                if (remainingBounces > 0)
                {
                    remainingBounces--;
                }
                if (transform.position.y == groundY)
                {

                    Destroy(gameObject);
                }
                else
                {

                    // keep going until we’re below ground
                    DoNextStep();
                }


            });
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.GetComponent<IHealthManager>().TakeDamage(damageToPlayer);
          
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
    /*  private void OnDrawGizmosSelected()
      {
          Gizmos.color = Color.red;
          Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 20f);
      }*/
}
