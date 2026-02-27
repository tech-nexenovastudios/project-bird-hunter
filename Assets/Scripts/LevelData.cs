using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int chapterNumber;
    public int levelNumber;
    public string levelName;

    public int totalPoints = 20;
    public float spawnInterval = 3f;
  

    public List<BirdSpawnEntry> birdsToSpawn = new List<BirdSpawnEntry>();
}
