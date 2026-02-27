using System.Collections.Generic;
using UnityEngine;


public enum BirdType
{
    E1 = 0,
    E2 = 1,
    E3 = 2,
    E4 = 3,
    ArrowBird = 4,
    BeeHurdle = 5,
    BombBird = 6,
    ElectricBird = 7,
    FireBird = 8,
    GunBird = 9,
    IceBird = 10,
    JuggernautBird = 11,
    LaserBird = 12,
    SuicideBird = 13,
    VenomBird = 14,
}

public class Bird : MonoBehaviour
{
    public BirdType type;

    public float speed = 2f;
    private BirdSpawner spawner;
    [HideInInspector] public bool hasBeenDestroyed = false;
    [HideInInspector] public bool hasSpawnedEgg = false;
    [SerializeField] float intialHeight;


    public float birdPoint;
    // Dynamic death position (set by BirdSpawner)
    private float deathPositionX = 5f; // Default fallback value

    public void Init(BirdSpawner spawner)
    {
        this.spawner = spawner;
    }

    public void SetDeathPositionX(float deathX)
    {
        deathPositionX = ScreenBounds.maxX + 3f;
    }

    private void OnEnable()
    {
        transform.position = new Vector2(ScreenBounds.minX, intialHeight);
        hasBeenDestroyed = false;
    }

    void Update()
    {
        if (hasBeenDestroyed) return;

        //transform.position += Vector3.right * speed * Time.deltaTime;

        // Use dynamic death position instead of hardcoded 5f
        if (transform.position.x >= deathPositionX)
        {
            DestroyBird();
        }
    }

    public void DestroyBird()
    {
        if (hasBeenDestroyed) return;
        hasBeenDestroyed = true;


        //I move this part into ondestroy event so that auto call when obj destory
        /*    if (spawner != null)
            {
                spawner.BirdDestroyed();
            }*/

        if (GamePoolManager.birdPool.ContainsKey(type))
        {
            GamePoolManager.birdPool[type].Enqueue(this.gameObject);
        }
        else
        {
            GamePoolManager.birdPool.Add(type, new Queue<GameObject>());
            GamePoolManager.birdPool[type].Enqueue(this.gameObject);
        }
        this.gameObject.SetActive(false);
        //Destroy(gameObject);
    }
    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.BirdDestroyed();
        }
    }

    private void OnDestroy()
    {
       /* if (spawner != null)
        {
            spawner.BirdDestroyed();
        }*/
    }

}