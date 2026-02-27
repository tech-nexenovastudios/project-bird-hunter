#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class LevelGenerator
{
    [MenuItem("Tools/Generate Chapters and Levels")]
    public static void GenerateAllLevels()
    {

        string levelRoot = "Assets/Resources/Levels";
        string chapterRoot = "Assets/Chapters";
        string Csvpath = "/Editor/Csvs/Bird_Math - Chapter01.csv";
        if (!Directory.Exists(levelRoot)) Directory.CreateDirectory(levelRoot);
        if (!Directory.Exists(chapterRoot)) Directory.CreateDirectory(chapterRoot);

        // Load bird prefabs from Resources
        GameObject bird1 = Resources.Load<GameObject>("Birds/B1-1");
        GameObject bird2 = Resources.Load<GameObject>("Birds/B2-1");
        GameObject bird3 = Resources.Load<GameObject>("Birds/B3-1");
        GameObject bird4 = Resources.Load<GameObject>("Birds/B4-1");

        if (!bird1 || !bird2 || !bird3 || !bird4)
        {
            Debug.LogError("Bird prefabs not found in Resources/Birds/. Make sure B1-1 to B4-1 exist.");
            return;
        }
        string[] allLines = File.ReadAllLines(Application.dataPath + Csvpath);
        for (int c = 1; c <= 50; c++)
        {
            List<LevelData> chapterLevels = new List<LevelData>();
            string chapterFolder = Path.Combine(levelRoot, $"Chapter{c:D2}");
            if (!Directory.Exists(chapterFolder))
                Directory.CreateDirectory(chapterFolder);

            for (int l = 1; l <= 20; l++)
            {
                string[] splitData = allLines[l - 1].Split(',');
                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                level.chapterNumber = c;
                level.levelNumber = l;
                level.levelName = $"Chapter {c} - Level {l}";
                level.spawnInterval = 3f;

                // Add 1 bird of each type by default
                level.birdsToSpawn = new List<BirdSpawnEntry>
                {
                    new BirdSpawnEntry { birdPrefab = bird1, count = int.Parse(splitData[2]) ,minSpawnTime = 1, maxSpawnTime = 3},
                    new BirdSpawnEntry { birdPrefab = bird2, count = int.Parse(splitData[3]) ,minSpawnTime = 4, maxSpawnTime = 5},
                    new BirdSpawnEntry { birdPrefab = bird3, count = int.Parse(splitData[4]) ,minSpawnTime = 6, maxSpawnTime = 8},
                    new BirdSpawnEntry { birdPrefab = bird4, count = int.Parse(splitData[5]) ,minSpawnTime = 10, maxSpawnTime = 12}
                };

                string levelPath = Path.Combine(chapterFolder, $"Level_{l:D2}.asset");
                AssetDatabase.CreateAsset(level, levelPath);
                chapterLevels.Add(level);
            }

            Chapter chapter = ScriptableObject.CreateInstance<Chapter>();
            chapter.chapterNumber = c;
            chapter.levels = chapterLevels;

            string chapterPath = Path.Combine(chapterRoot, $"Chapter{c:D2}.asset");
            AssetDatabase.CreateAsset(chapter, chapterPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Chapters and Levels created with default bird prefabs.");
    }
}
#endif
