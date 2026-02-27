// using Gameplay.Birds;
// using Gameplay.Eggs;
// using Gameplay.Levels;
// using UnityEngine;
// using UnityEditor;

// namespace Gameplay
// {
//     public class FullChapterLevelProfileGenerator : EditorWindow
//     {
//         private EggTierConfig e1, e2, e3, e4;
//         private BirdConfig b1, b2, b3, b4;
//         private AdaptiveDifficultyConfig adaptiveConfig;

//         private string baseFolder = "Assets/GeneratedLevels";

//         [MenuItem("BirdHunter/Generate ALL 30 Chapters (600 Levels)")]
//         public static void ShowWindow()
//         {
//             GetWindow<FullChapterLevelProfileGenerator>("Full Level Generator");
//         }

//         void OnGUI()
//         {
//             GUILayout.Label("Bird Hunter FULL Progression Generator", EditorStyles.boldLabel);
//             GUILayout.Label("Generates 600 Level Profiles (30 Chapters × 20 Levels)", EditorStyles.helpBox);

//             EditorGUILayout.Space();

//             GUILayout.Label("Required Configs:", EditorStyles.boldLabel);
//             e1 = (EggTierConfig)EditorGUILayout.ObjectField("E1 Config", e1, typeof(EggTierConfig), false);
//             e2 = (EggTierConfig)EditorGUILayout.ObjectField("E2 Config", e2, typeof(EggTierConfig), false);
//             e3 = (EggTierConfig)EditorGUILayout.ObjectField("E3 Config", e3, typeof(EggTierConfig), false);
//             e4 = (EggTierConfig)EditorGUILayout.ObjectField("E4 Config", e4, typeof(EggTierConfig), false);

//             EditorGUILayout.Space();

//             b1 = (BirdConfig)EditorGUILayout.ObjectField("B1 Config", b1, typeof(BirdConfig), false);
//             b2 = (BirdConfig)EditorGUILayout.ObjectField("B2 Config", b2, typeof(BirdConfig), false);
//             b3 = (BirdConfig)EditorGUILayout.ObjectField("B3 Config", b3, typeof(BirdConfig), false);
//             b4 = (BirdConfig)EditorGUILayout.ObjectField("B4 Config", b4, typeof(BirdConfig), false);

//             EditorGUILayout.Space();

//             adaptiveConfig = (AdaptiveDifficultyConfig)EditorGUILayout.ObjectField("Adaptive Config", adaptiveConfig,
//                 typeof(AdaptiveDifficultyConfig), false);

//             EditorGUILayout.Space();

//             baseFolder = EditorGUILayout.TextField("Base Folder", baseFolder);
//             GUILayout.Label($"(Will create: {baseFolder}/Chapter1/Levels, Chapter2/Levels, etc.)",
//                 EditorStyles.helpBox);

//             EditorGUILayout.Space();
//             GUILayout.Label("⚠️ This will create 600+ files. Use version control!", EditorStyles.helpBox);

//             EditorGUILayout.Space();
//             if (GUILayout.Button("🚀 GENERATE ALL 30 CHAPTERS (600 Levels)", GUILayout.Height(50)))
//             {
//                 if (ValidateInputs())
//                 {
//                     GenerateAllChapters();
//                 }
//             }

//             EditorGUILayout.Space();
//             if (GUILayout.Button("📊 Generate Summary Only (No Assets)", GUILayout.Height(30)))
//             {
//                 GenerateSummaryOnly();
//             }
//         }

//         bool ValidateInputs()
//         {
//             if (e1 == null || e2 == null || e3 == null || e4 == null)
//             {
//                 EditorUtility.DisplayDialog("❌ Missing", "Assign all E1-E4 Egg Tier Configs!", "OK");
//                 return false;
//             }

//             if (b1 == null || b2 == null || b3 == null || b4 == null)
//             {
//                 EditorUtility.DisplayDialog("❌ Missing", "Assign all B1-B4 Bird Configs!", "OK");
//                 return false;
//             }

//             if (adaptiveConfig == null)
//             {
//                 EditorUtility.DisplayDialog("❌ Missing", "Assign Adaptive Difficulty Config!", "OK");
//                 return false;
//             }

//             return true;
//         }

//         void GenerateAllChapters()
//         {
//             int totalGenerated = 0;

//             // Progress dialog
//             float totalLevels = 600;
//             float progress = 0f;
//             EditorUtility.DisplayProgressBar("Generating Levels", "Creating Level Profiles...", 0f);

//             for (int chapter = 1; chapter <= 30; chapter++)
//             {
//                 string chapterFolder = $"{baseFolder}/Chapter{chapter}";
//                 string levelsFolder = $"{chapterFolder}/Levels";

//                 // Create folder structure
//                 if (!AssetDatabase.IsValidFolder(baseFolder))
//                     AssetDatabase.CreateFolder("Assets", "GeneratedLevels");

//                 if (!AssetDatabase.IsValidFolder(chapterFolder))
//                     AssetDatabase.CreateFolder(baseFolder, $"Chapter{chapter}");

//                 if (!AssetDatabase.IsValidFolder(levelsFolder))
//                     AssetDatabase.CreateFolder(chapterFolder, "Levels");

//                 // Generate 20 levels per chapter
//                 for (int level = 1; level <= 20; level++)
//                 {
//                     int globalLevel = (chapter - 1) * 20 + level;

//                     LevelProfile profile = ScriptableObject.CreateInstance<LevelProfile>();
//                     var values = CalculateLevelValues(globalLevel, chapter);

//                     profile.chapter = chapter;
//                     profile.globalLevel = globalLevel;
//                     profile.targetScore = values.score;
//                     profile.minDuration = values.minDuration;
//                     profile.maxDuration = values.maxDuration;
//                     profile.pressureMax = values.pMax;
//                     profile.pressureAvg = values.pAvg;
//                     profile.maxE4 = values.maxE4;
//                     profile.maxE3 = values.maxE3;
//                     profile.maxE2 = values.maxE2;
//                     profile.spawnIntervalMin = values.spawnMin;
//                     profile.spawnIntervalMax = values.spawnMax;
//                     profile.expectedDps = values.dps;
//                     profile.hpMultiplier = values.hpMult;

//                     string assetPath = $"{levelsFolder}/Ch{chapter}_L{level:00}.asset";
//                     AssetDatabase.CreateAsset(profile, assetPath);
//                     EditorUtility.SetDirty(profile);

//                     totalGenerated++;

//                     // Update progress
//                     progress = totalGenerated / totalLevels;
//                     EditorUtility.DisplayProgressBar($"Chapter {chapter}/30",
//                         $"Level {level}/20 (Global {globalLevel})", progress);
//                 }
//             }

//             EditorUtility.ClearProgressBar();

//             // Create master summary
//             //CreateMasterSummary(totalGenerated);

//             AssetDatabase.SaveAssets();
//             AssetDatabase.Refresh();

//             Debug.Log($"✅ Generated {totalGenerated} Level Profiles across 30 chapters!");
//             EditorUtility.DisplayDialog("Complete! 🎉",
//                 $"Created {totalGenerated} Level Profiles\n" +
//                 $"Check '{baseFolder}' folder structure\n" +
//                 $"Master summary: {baseFolder}/LevelProgressionSummary.txt",
//                 "OK");
//         }

//         void GenerateSummaryOnly()
//         {
//             System.Text.StringBuilder summary = new System.Text.StringBuilder();
//             summary.AppendLine("Bird Hunter Complete Progression Summary");
//             summary.AppendLine("========================================");
//             summary.AppendLine();

//             for (int chapter = 1; chapter <= 30; chapter++)
//             {
//                 summary.AppendLine($"CHAPTER {chapter}");
//                 summary.AppendLine("--------");

//                 for (int level = 1; level <= 20; level++)
//                 {
//                     int globalLevel = (chapter - 1) * 20 + level;
//                     var values = CalculateLevelValues(globalLevel, chapter);

//                     summary.AppendLine($"  L{level:00} (Global {globalLevel}):");
//                     summary.AppendLine($"    Score: {values.score}");
//                     summary.AppendLine($"    Pressure: {values.pMax}/{values.pAvg}");
//                     summary.AppendLine($"    Caps: E4={values.maxE4}, E3={values.maxE3}, E2={values.maxE2}");
//                     summary.AppendLine($"    Spawn: {values.spawnMin:F2}-{values.spawnMax:F2}s");
//                     summary.AppendLine($"    DPS: {values.dps}, HP Mult: {values.hpMult:F2}");
//                 }

//                 summary.AppendLine();
//             }

//             string summaryPath = $"{baseFolder}/LevelProgressionSummary.txt";
//             System.IO.File.WriteAllText(summaryPath, summary.ToString());
//             AssetDatabase.ImportAsset(summaryPath);

//             Debug.Log("📊 Summary generated: " + summaryPath);
//         }

//         (int score, int pMax, int pAvg, int maxE4, int maxE3, int maxE2,
//             float minDuration, float maxDuration, float spawnMin, float spawnMax, int dps, float hpMult)
//             CalculateLevelValues(int globalLevel, int chapter)
//         {
//             // HP Multiplier (9.5x total growth)
//             float hpMult;
//             if (globalLevel <= 100)
//                 hpMult = 1.0f + 1.5f * (globalLevel / 100f);
//             else if (globalLevel <= 300)
//             {
//                 float baseVal = 2.5f;
//                 float additional = 3.0f * Mathf.Sqrt((globalLevel - 100f) / 200f);
//                 hpMult = baseVal + additional;
//             }
//             else
//             {
//                 float baseVal = 5.5f;
//                 float progress = (globalLevel - 300f) / 300f;
//                 float additional = 4.5f * (1f - Mathf.Exp(-2.5f * progress));
//                 hpMult = baseVal + additional;
//             }

//             // DPS Multiplier → Expected DPS
//             float dpsMult;
//             if (globalLevel <= 100)
//                 dpsMult = 1.0f + 1.8f * (globalLevel / 100f);
//             else if (globalLevel <= 300)
//             {
//                 float baseVal = 2.8f;
//                 float additional = 3.2f * Mathf.Sqrt((globalLevel - 100f) / 200f);
//                 dpsMult = baseVal + additional;
//             }
//             else
//             {
//                 float baseVal = 6.0f;
//                 float progress = (globalLevel - 300f) / 300f;
//                 float additional = 4.5f * (1f - Mathf.Exp(-2.5f * progress));
//                 dpsMult = baseVal + additional;
//             }

//             int expectedDps = Mathf.RoundToInt(12 * dpsMult);

//             // Spawn Interval (floor 0.3s)
//             float spawnMin, spawnMax;
//             if (globalLevel <= 100)
//             {
//                 spawnMin = Mathf.Max(0.3f, 4.0f - 3.0f * (globalLevel / 100f));
//                 spawnMax = Mathf.Max(spawnMin + 0.5f, 6.0f - 4.0f * (globalLevel / 100f));
//             }
//             else if (globalLevel <= 300)
//             {
//                 float progress = (globalLevel - 100f) / 200f;
//                 spawnMin = Mathf.Max(0.3f, 1.0f - 0.5f * progress);
//                 spawnMax = Mathf.Max(spawnMin + 0.3f, 2.0f - 1.0f * progress);
//             }
//             else
//             {
//                 float progress = Mathf.Min(1.0f, (globalLevel - 300f) / 300f);
//                 spawnMin = Mathf.Max(0.3f, 0.5f - 0.2f * progress);
//                 spawnMax = Mathf.Max(spawnMin + 0.2f, 1.0f - 0.3f * progress);
//             }

//             // Pressure Max (cap 200)
//             int pMax;
//             if (globalLevel <= 100)
//                 pMax = Mathf.RoundToInt(15 + 45 * (globalLevel / 100f));
//             else if (globalLevel <= 300)
//             {
//                 float baseVal = 60;
//                 float additional = 70 * Mathf.Sqrt((globalLevel - 100f) / 200f);
//                 pMax = Mathf.RoundToInt(baseVal + additional);
//             }
//             else
//             {
//                 float baseVal = 130;
//                 float additional = 70 * (1f - Mathf.Exp(-(globalLevel - 300f) / 150f));
//                 pMax = Mathf.Min(200, Mathf.RoundToInt(baseVal + additional));
//             }

//             int pAvg = Mathf.RoundToInt(pMax * 0.65f);

//             // Tier Caps
//             int maxE4, maxE3, maxE2;
//             if (globalLevel <= 100)
//             {
//                 maxE4 = Mathf.Min(8, pMax / 48);
//                 maxE3 = Mathf.Min(20, pMax / 20);
//                 maxE2 = Mathf.Min(40, pMax / 4);
//             }
//             else if (globalLevel <= 300)
//             {
//                 maxE4 = Mathf.Min(12, pMax / 40);
//                 maxE3 = Mathf.Min(30, pMax / 16);
//                 maxE2 = Mathf.Min(60, Mathf.RoundToInt(pMax / 3.5f));
//             }
//             else
//             {
//                 maxE4 = Mathf.Min(15, pMax / 35);
//                 maxE3 = Mathf.Min(40, pMax / 14);
//                 maxE2 = Mathf.Min(80, Mathf.RoundToInt(pMax / 3));
//             }

//             // Target Score
//             float avgDuration = 67f;
//             float baseEfficiency;
//             if (globalLevel <= 100) baseEfficiency = 7.5f;
//             else if (globalLevel <= 300) baseEfficiency = 9.0f;
//             else baseEfficiency = 10.5f;

//             float scorePerSec = baseEfficiency * dpsMult;
//             int score = Mathf.RoundToInt(scorePerSec * avgDuration);

//             // Round to nice numbers
//             if (score < 1000) score = (score / 50) * 50;
//             else if (score < 5000) score = (score / 100) * 100;
//             else if (score < 50000) score = (score / 250) * 250;
//             else score = (score / 500) * 500;

//             // Duration progression
//             float minDuration, maxDuration;
//             if (globalLevel <= 100)
//             {
//                 minDuration = 45f;
//                 maxDuration = 70f;
//             }
//             else if (globalLevel <= 300)
//             {
//                 minDuration = 48f;
//                 maxDuration = 80f;
//             }
//             else
//             {
//                 minDuration = 50f;
//                 maxDuration = 90f;
//             }

//             return (score, pMax, pAvg, maxE4, maxE3, maxE2, minDuration, maxDuration, spawnMin, spawnMax, expectedDps,
//                 hpMult);
//         }
//     }

// }
