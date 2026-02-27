using UnityEngine;
using System.Collections.Generic;


public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public int killBird;
    public float collectCoin;
    public int collectDiamond;
    public int collectPoints;
    public int destroyEggs;
    [SerializeField] List<TaskCreator> creators;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }



    public void UpdateValue()
    {
        foreach(var creator in creators)
        {
            creator.UpdateTask(collectCoin,collectDiamond,collectPoints,destroyEggs,killBird);
        }
        collectCoin = 0;
        collectDiamond = 0;
        collectPoints = 0;
        destroyEggs = 0;
        killBird = 0;

    }
}
