using UnityEngine;

public class BirdEggSpawner : MonoBehaviour
{
    Bird bird;
    public float speed = 2f;
    public GameObject Egg;


    private void OnEnable()
    {
        bird = GetComponent<Bird>();
        Invoke("SpawnEgg", Random.Range(1f, 1.5f));
    }
    public void SpawnEgg()
    {
        if (bird.hasBeenDestroyed) return;  // Don't spawn if already destroyed

        bird.hasSpawnedEgg = true;
        GameObject go = null;

        //Debug.Log(GamePoolManager.EggPool.ContainsKey(Egg.GetComponent<EggHealth>().eggType));

        if (GamePoolManager.EggPool.ContainsKey(Egg.GetComponent<EggHealth>().eggType))
        {
            Debug.Log(GamePoolManager.EggPool[Egg.GetComponent<EggHealth>().eggType].Count);
        }
        if (GamePoolManager.EggPool.ContainsKey(Egg.GetComponent<EggHealth>().eggType) && GamePoolManager.EggPool[Egg.GetComponent<EggHealth>().eggType].Count > 0)
        {
            Debug.Log(GamePoolManager.EggPool[Egg.GetComponent<EggHealth>().eggType].Count);
            go = GamePoolManager.EggPool[Egg.GetComponent<EggHealth>().eggType].Dequeue();
            go.transform.position = transform.position;
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(Egg, transform.position, Quaternion.identity);
        }
        go.GetComponent<EggHealth>().IntialValueSet();
        var sprite = go.GetComponent<SpriteRenderer>();
        sprite.material.SetColor("_Color", sprite.color);
        sprite.color = Color.white;
        go.GetComponent<Rigidbody2D>().AddForce(Vector2.right * speed, ForceMode2D.Impulse);
    }

    private void OnDestroy()
    {
        // If the bird is destroyed before spawning egg, count the egg as destroyed
        if (!bird.hasSpawnedEgg && Egg != null)
        {
            var eggHealth = Egg.GetComponent<EggHealth>();
            if (eggHealth != null && LevelProgressTracker.Instance != null)
            {
                // Notify that an egg of this type was destroyed
                LevelProgressTracker.Instance.OnUnspawnedEggDestroyed(eggHealth.eggType);
            }
        }
    }
}
