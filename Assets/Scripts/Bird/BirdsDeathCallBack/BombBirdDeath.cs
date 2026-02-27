using UnityEngine;

public class BombBirdDeath : MonoBehaviour, IDeath
{
    public ParticleSystem deathParticle;
    public GameObject miniGrenade;
    public void OnDeath()
    {
        if (deathParticle != null)
        {
            Destroy(Instantiate(deathParticle, transform.position, Quaternion.identity), 5f);
        }
        if(miniGrenade != null)
        {
            AddForce(Instantiate(miniGrenade,transform.position, Quaternion.identity),Random.Range(1,3));   
            AddForce(Instantiate(miniGrenade,transform.position, Quaternion.identity),Random.Range(1,3));   
            AddForce(Instantiate(miniGrenade,transform.position, Quaternion.identity),Random.Range(-1,-3));   
            AddForce(Instantiate(miniGrenade,transform.position, Quaternion.identity),Random.Range(-1,-3));
        }
    }

    public void AddForce(GameObject miniGrenade, float force)
    {
        miniGrenade.GetComponent<Rigidbody2D>().AddForce(transform.right * force, ForceMode2D.Impulse);
    }
}
