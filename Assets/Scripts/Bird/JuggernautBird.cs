using Spine.Unity;
using UnityEngine;

public class JuggernautBird : MonoBehaviour, IAttack
{
    [SerializeField] float damage;
    [SerializeField] GameObject dustParticle;
    SkeletonAnimation skeletonAnimation;
    Spine.AnimationState animationState;


    private void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        animationState = skeletonAnimation.AnimationState;
    }
    public void Attack()
    {
        Debug.Log("Attack");
        skeletonAnimation.state.SetAnimation(0, "ball", false);
    }

    public void ResetAttack()
    {
        skeletonAnimation.state.SetAnimation(0, "1st", true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Destroy(Instantiate(dustParticle, transform.position, Quaternion.identity), 3f);
        if (collision.transform.CompareTag(TagManager.PlayerTag))
        {
            Debug.Log("Hit");
            collision.GetComponent<IHealthManager>().TakeDamage(damage);

            this.GetComponent<Collider2D>().enabled = false;
        }
    }


}
