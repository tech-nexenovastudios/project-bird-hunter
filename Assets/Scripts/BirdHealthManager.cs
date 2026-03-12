using UnityEngine;

[System.Obsolete("Use Gameplay.Health.BirdHealth with IDamageable instead. Legacy flow - will be removed.")]
public class BirdHealthManager : MonoBehaviour, IHealthManager
{
    public int maxHealth = 10;
    private float currentHealth;

    [SerializeField] GameObject[] Coin;
    [SerializeField] GameObject blastParticle;

    private Bird birdScript;

    public bool birdDied;


    [SerializeField] float increaseCannonHealth;


    private void OnEnable()
    {

    }
    public void Init(BirdSpawner spawner)
    {
        birdDied = false;
        birdScript = GetComponent<Bird>();
        if (birdScript != null)
        {
            birdScript.Init(spawner);
        }

        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {


        ScoreManager.scoreUpdateCallBack?.Invoke(Mathf.Clamp(Mathf.Min(currentHealth, damage), 0, float.MaxValue)); // Send Score To scoreManager
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            if (!birdDied)
            {
                birdDied = true;
                LevelManager.Instance.killBird += 1;
                GameObject go = null;
                if (GamePoolManager.blastParticlePool.Count > 0)
                {
                    go = GamePoolManager.blastParticlePool.Dequeue();
                    go.transform.position = transform.position;
                    go.transform.rotation = Quaternion.Euler(Vector3.zero);
                    go.transform.localScale = transform.localScale;
                    go.SetActive(true);
                }
                else
                {
                    go = Instantiate(blastParticle, transform.position, Quaternion.identity);
                    go.transform.localScale = transform.localScale;
                }
                /*Destroy(go, 3f);*/
                GamePoolManager.SetBackToPool<GameObject>(3, GamePoolManager.blastParticlePool, go);
                Die();
            }
        }
    }




    void Die()
    {
        if (CannonPower.instance.getBackHealth)
        {
            if (Random.Range(0, 100) < CannonPower.instance.probabilityToGetBackHealth)
            {
                CannonHealthOld.increaseHealthCallBack?.Invoke(increaseCannonHealth);
            }
        }
        if (Coin.Length > 0)
        {
            Instantiate(Coin[Random.Range(0, Coin.Length)], transform.position, Quaternion.identity)
                .GetComponent<Rigidbody2D>().AddForce(Vector3.right * 5, ForceMode2D.Impulse);
        }
        if (birdScript != null)
        {
            birdScript.DestroyBird();
        }
        else
        {
            //for testing purpose only
            var death = GetComponent<IDeath>();
            if (death != null)
            {
                death.OnDeath();
            }
            Destroy(this.gameObject);
        }

    }
}
