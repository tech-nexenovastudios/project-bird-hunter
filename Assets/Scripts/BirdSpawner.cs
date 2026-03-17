using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

public class BirdSpawner : MonoBehaviour
{
   // public GameManager gameManager;

    private int totalBirds;
    private int birdsDestroyed;
    private bool alreadyAllBirdDestroy;

    Dictionary<GameObject, int> birdTime = new Dictionary<GameObject, int>();

    [SerializeField] GameObject GoldenBird;

    // Dynamic spawn and death positions based on screen bounds
    private Vector3 spawnPosition;
    [SerializeField] Camera MainCam; // Assign main camera

    float previousTime = 0f;
    private float deathPositionX;


    public float totalMaxPoints;


    Queue<BirdQueue> birdQueue = new Queue<BirdQueue>();


    public List<Action> birdSpawnEvent = new List<Action>();

    public static bool SpawnBird;




    [Header("Spawn Config")]
    [SerializeField] float timeBetweenSpawn;
    [SerializeField] float delayAfterPointReach;
    bool pointReach;

    public GameObject[] bird;


    private void Awake()
    {
        if (MainCam == null)
            MainCam = Camera.main;

        CalculateScreenBounds();
    }

    private void Start()
    {
        SpawnBird = true;
    }

    private void Update()
    {
        //Debug.LogError(PointManager.totalPoints);
    }
    private void CalculateScreenBounds()
    {
        if (MainCam == null) return;

        // Get screen bounds in world space
        //Vector3 leftEdge = MainCam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2, 0));
        //Vector3 rightEdge = MainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.width / 2, 0));

        float screenWidth = ScreenBounds.maxX - ScreenBounds.minX;

        // Spawn position = leftmost screen point - width of screen / 2
        float spawnX = ScreenBounds.minX - screenWidth / 2;
        spawnPosition = new Vector3(spawnX, 2.5f, 0f); // Keep Y at 2.5f as before

        // Death position = rightmost point + width of screen / 2
        deathPositionX = ScreenBounds.maxX + screenWidth / 2;

        Debug.Log($"Screen bounds calculated - Spawn X: {spawnX}, Death X: {deathPositionX}");
    }

    public void StartSpawning(LevelData levelData)
    {
        // Recalculate bounds in case screen orientation changed
        CalculateScreenBounds();

        //Debug.Log("[BirdSpawner] StartSpawning called");
        if (Random.Range(0, 100) <= 5)
        {
            Instantiate(GoldenBird, spawnPosition, Quaternion.identity);
        }
        StartCoroutine(SpawnBirds(levelData));
    }

    private void OnEnable()
    {
        EggManager.onEggsChanged += EggDestroyed;
    }

    private void OnDisable()
    {
        EggManager.onEggsChanged -= EggDestroyed;

        GamePoolManager.birdPool.Clear();
    }

    private IEnumerator SpawnBirds(LevelData levelData)
    {
        totalBirds = 0;
        birdsDestroyed = 0;


        foreach (var entry in levelData.birdsToSpawn)
        {
            totalBirds += entry.count;
        }
        bird = new GameObject[totalBirds];
        int counter = 0;
        foreach (var entry in levelData.birdsToSpawn)
        {
            //StartCoroutine(BirdSpawnCoroutine(entry));
            for (int i = 0; i < entry.count; i++)
            {
                bird[counter] = entry.birdPrefab;
                counter++;
            }
        }

        for (int i = bird.Length - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (bird[i], bird[rand]) = (bird[rand], bird[i]);
        }
        Debug.Log("Toatal birds to be spawned: " + totalBirds);
        yield return null;

        /*       foreach (var entry in levelData.birdsToSpawn)
               {
                   for (int i = 0; i < entry.count; i++)
                   {
                       yield return new WaitUntil(() => PointManager.totalPoints <= levelData.totalPoints);
                       if (birdTime.ContainsKey(entry.birdPrefab))
                       {
                           birdTime[entry.birdPrefab] += 1;
                       }
                       else
                       {
                           birdTime[entry.birdPrefab] = 1;
                       }
                       float time = Random.Range(entry.minSpawnTime, entry.maxSpawnTime) * birdTime[entry.birdPrefab];
                       BirdQueueAdd(entry.birdPrefab, time);
                       // GameObject bird = Instantiate(entry.birdPrefab, spawnPosition, Quaternion.identity);
                       // yield return new WaitForSeconds(time);
                   }
               }*/


       
            //StartCoroutine(BirdSpawnCoroutine(entry));
            for (int i = 0; i < bird.Length; i++)
            {
                //yield return new WaitForSeconds(Random.Range(entry.minSpawnTime, entry.maxSpawnTime));
                /* if (birdTime.ContainsKey(entry.birdPrefab))
                 {
                     birdTime[entry.birdPrefab] += 1;
                 }
                 else
                 {
                     birdTime[entry.birdPrefab] = 1;
                 }*/
               // float randomTime = Random.Range(entry.minSpawnTime, entry.maxSpawnTime);
                yield return new WaitUntil(() => (PointManager.totalPoints <= totalMaxPoints));
                yield return new WaitForSeconds(timeBetweenSpawn);
             //   Debug.Log(randomTime);
                if (PointManager.totalPoints >= totalMaxPoints)
                {
                    pointReach = true;
                }
                yield return new WaitUntil(() => (PointManager.totalPoints <= totalMaxPoints));
                if (pointReach)
                {
                    pointReach = false;
                    yield return new WaitForSeconds(delayAfterPointReach);
                }
                StartCoroutine(BirdSpawnInTime(1, bird[i]));
                // Debug.Log(birdTime[entry.birdPrefab]);
                yield return null;
                // yield return new WaitForSeconds(levelData.spawnInterval);
            }

            yield return null;
        

        birdTime.Clear();
    }


    IEnumerator BirdSpawnCoroutine(BirdSpawnEntry entry)
    {

        for (int i = 0; i < entry.count; i++)
        {
            //yield return new WaitForSeconds(Random.Range(entry.minSpawnTime, entry.maxSpawnTime));
            if (birdTime.ContainsKey(entry.birdPrefab))
            {
                birdTime[entry.birdPrefab] += 1;
            }
            else
            {
                birdTime[entry.birdPrefab] = 1;
            }
            float randomTime = Random.Range(entry.minSpawnTime, entry.maxSpawnTime);
            yield return new WaitUntil(() => (PointManager.totalPoints <= totalMaxPoints));
            yield return new WaitForSeconds(timeBetweenSpawn);
            Debug.Log(randomTime);
            if (PointManager.totalPoints <= totalMaxPoints)
            {
                pointReach = true;
            }
            yield return new WaitUntil(() => (PointManager.totalPoints <= totalMaxPoints));
            if (pointReach)
            {
                pointReach = false;
                yield return new WaitForSeconds(delayAfterPointReach);
            }
            StartCoroutine(BirdSpawnInTime(randomTime, bird[i]));
            // Debug.Log(birdTime[entry.birdPrefab]);
            yield return null;
            // yield return new WaitForSeconds(levelData.spawnInterval);
        }

        yield return null;
    }

    public void BirdQueueAdd(GameObject bird, float time)
    {
        BirdQueue birdQueues = new BirdQueue()
        {
            bird = bird,
            timer = time
        };
        birdQueue.Enqueue(birdQueues);
    }

    IEnumerator BirdSpawn()
    {
        foreach (var bird in birdQueue)
        {


        }
        yield return null;
    }
    IEnumerator BirdSpawnInTime(float time, GameObject entry)
    {
        //  yield return new WaitForSeconds(time);

        //   SpawnBird = false;

        if (GamePoolManager.birdPool.ContainsKey(entry.GetComponent<Bird>().type))
        {
            // Debug.Log(entry.birdPrefab.GetComponent<Bird>().type + " " + birdPool[entry.birdPrefab.GetComponent<Bird>().type].Count);
        }

        var key = entry.GetComponent<Bird>();
        GameObject bird = null;
        if (GamePoolManager.birdPool.ContainsKey(key.type) && GamePoolManager.birdPool[key.type].Count > 0)
        {
            bird = GamePoolManager.birdPool[key.type].Dequeue();
        }
        else
        {
            bird = Instantiate(entry, spawnPosition, Quaternion.identity);
        }
        bird.SetActive(true);

        //Debug.Log(bird.name);

        BirdHealthManager health = bird.GetComponent<BirdHealthManager>();
        if (health != null)
        {
            health.Init(this);  // This handles Bird.Init() as well
        }

        // Set the death position for the bird (you'll need to pass this to the bird script)
        Bird birdScript = bird.GetComponent<Bird>();
        if (birdScript != null)
        {
            birdScript.Init(this);
            birdScript.SetDeathPositionX(deathPositionX);
        }

        previousTime = Time.time;

        yield return null;
        //Debug.Log(levelData.name);
    }

    public void BirdDestroyed()
    {
        birdsDestroyed++;
        //Debug.Log($"✅ Bird destroyed {birdsDestroyed}/{totalBirds}");

        if (birdsDestroyed >= totalBirds)
        {
            alreadyAllBirdDestroy = true;
            EggDestroyed();
        }
    }

    public void EggDestroyed()
    {

        if (alreadyAllBirdDestroy)
        {
            if (EggManager.eggsList.Count <= 0)
            {
                //gameManager.OnAllBirdsDestroyed();
                alreadyAllBirdDestroy = false;
            }
        }
    }

    // Public getter methods for other scripts that might need these values
    public Vector3 GetSpawnPosition() => spawnPosition;
    public float GetDeathPositionX() => deathPositionX;





    public class BirdQueue
    {
        public GameObject bird;
        public float timer;
    }

}
