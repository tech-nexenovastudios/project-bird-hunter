#if UNITY_EDITOR
using Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    public class ChapterProgressionConfigGenerator : EditorWindow
    {
        const int TotalChapters = 30;
        const int LevelsPerChapter = 20;
        const int GlobalLevels = TotalChapters * LevelsPerChapter;

        string baseFolder = "Assets/Resources/Data/ChapterProgressions";
        bool overwriteExisting = false;

        // Global geometric endpoints (Ch1 L1 → Ch30 L20)
        float hpMultGlobalMin = 1.0f;
        float hpMultGlobalMax = 4.0f;
        int targetScoreGlobalMin = 500;
        int targetScoreGlobalMax = 200000;

        // Linear chapter-to-chapter endpoints
        int pressureMaxCh1 = 10;
        int pressureMaxCh30 = 32;
        float pressureAvgStartCh1 = 4f;
        float pressureAvgStartCh30 = 11f;
        float pressureAvgStep = 0.5f;

        float spawnEarlyCh1 = 4.5f;
        float spawnEarlyCh30 = 1.2f;
        float spawnLateCh1 = 1.0f;
        float spawnLateCh30 = 0.6f;
        float spawnJitter = 0.25f;

        float minDurStartCh1 = 25f, minDurStartCh30 = 55f;
        float minDurEndCh1 = 35f, minDurEndCh30 = 65f;
        float maxDurStartCh1 = 45f, maxDurStartCh30 = 80f;
        float maxDurEndCh1 = 55f, maxDurEndCh30 = 95f;

        [MenuItem("BirdHunter/Generate Chapter Progression Configs (30)")]
        public static void ShowWindow()
            => GetWindow<ChapterProgressionConfigGenerator>("Chapter Progression");

        void OnGUI()
        {
            GUILayout.Label("Chapter Progression Config Generator", EditorStyles.boldLabel);
            GUILayout.Label($"Emits {TotalChapters} ChapterProgressionConfig assets.", EditorStyles.helpBox);
            EditorGUILayout.Space();

            baseFolder = EditorGUILayout.TextField("Output Folder", baseFolder);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite existing", overwriteExisting);
            EditorGUILayout.Space();

            GUILayout.Label("HP Multiplier (global)", EditorStyles.boldLabel);
            hpMultGlobalMin = EditorGUILayout.FloatField("HP × at Ch1 L1", hpMultGlobalMin);
            hpMultGlobalMax = EditorGUILayout.FloatField("HP × at Ch30 L20", hpMultGlobalMax);

            GUILayout.Label("Target Score (global)", EditorStyles.boldLabel);
            targetScoreGlobalMin = EditorGUILayout.IntField("Score at Ch1 L1", targetScoreGlobalMin);
            targetScoreGlobalMax = EditorGUILayout.IntField("Score at Ch30 L20", targetScoreGlobalMax);

            GUILayout.Label("Pressure (chapter-linear)", EditorStyles.boldLabel);
            pressureMaxCh1 = EditorGUILayout.IntField("pressureMax Ch1", pressureMaxCh1);
            pressureMaxCh30 = EditorGUILayout.IntField("pressureMax Ch30", pressureMaxCh30);
            pressureAvgStartCh1 = EditorGUILayout.FloatField("pressureAvgStart Ch1", pressureAvgStartCh1);
            pressureAvgStartCh30 = EditorGUILayout.FloatField("pressureAvgStart Ch30", pressureAvgStartCh30);
            pressureAvgStep = EditorGUILayout.FloatField("pressureAvgStep / level", pressureAvgStep);

            GUILayout.Label("Spawn Interval (chapter-linear)", EditorStyles.boldLabel);
            spawnEarlyCh1 = EditorGUILayout.FloatField("early (L1) Ch1", spawnEarlyCh1);
            spawnEarlyCh30 = EditorGUILayout.FloatField("early (L1) Ch30", spawnEarlyCh30);
            spawnLateCh1 = EditorGUILayout.FloatField("late (L20) Ch1", spawnLateCh1);
            spawnLateCh30 = EditorGUILayout.FloatField("late (L20) Ch30", spawnLateCh30);
            spawnJitter = EditorGUILayout.FloatField("jitter", spawnJitter);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate 30 Chapter Configs", GUILayout.Height(40)))
                Generate();

            if (GUILayout.Button("Preview Resolved Summary (600 rows)", GUILayout.Height(30)))
                WriteSummary();
        }

        void WriteSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Bird Hunter — Resolved Chapter Progression Summary");
            sb.AppendLine("===================================================");
            int missing = 0;

            for (int ch = 1; ch <= TotalChapters; ch++)
            {
                var cfg = AssetDatabase.LoadAssetAtPath<ChapterProgressionConfig>($"{baseFolder}/Chapter{ch}.asset");
                if (cfg == null) { missing++; sb.AppendLine($"\nCHAPTER {ch}: (missing asset)"); continue; }

                sb.AppendLine($"\nCHAPTER {ch}  pMax={cfg.pressureMax}  hp×[{cfg.hpMultMin:F2}..{cfg.hpMultMax:F2}]  target=[{cfg.targetScoreMin}..{cfg.targetScoreMax}]");
                sb.AppendLine("-----------------------------------------------------------------------");

                for (int i = 0; i < cfg.totalLevels; i++)
                {
                    var p = LevelProfileResolver.Resolve(cfg, i);
                    string boss = p.isBossLevel ? " BOSS" : "";
                    sb.AppendLine($"  L{i + 1:D2} (G{p.globalLevel}): score={p.targetScore}  hp×{p.hpMultiplier:F2}  pAvg={p.pressureAvg}  spawn={p.spawnIntervalMin:F2}–{p.spawnIntervalMax:F2}s  caps E4:{p.maxE4} E3:{p.maxE3} E2:{p.maxE2}{boss}");
                    Object.DestroyImmediate(p);
                }
            }

            string path = "Assets/ChapterProgressionSummary.txt";
            System.IO.File.WriteAllText(path, sb.ToString());
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[ChapterProgressionConfigGenerator] Summary written: {path}  ({missing} missing chapters)");
        }

        void Generate()
        {
            EnsureFolderPath(baseFolder);

            int written = 0, skipped = 0;
            for (int ch = 1; ch <= TotalChapters; ch++)
            {
                string assetPath = $"{baseFolder}/Chapter{ch}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ChapterProgressionConfig>(assetPath);

                if (existing != null && !overwriteExisting)
                {
                    skipped++;
                    continue;
                }

                var cfg = existing != null ? existing : ScriptableObject.CreateInstance<ChapterProgressionConfig>();
                PopulateDefaults(cfg, ch);

                if (existing == null)
                    AssetDatabase.CreateAsset(cfg, assetPath);
                EditorUtility.SetDirty(cfg);
                written++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ChapterProgressionConfigGenerator] Wrote {written}, skipped {skipped} existing.");
        }

        void PopulateDefaults(ChapterProgressionConfig cfg, int chapter)
        {
            cfg.chapter = chapter;
            cfg.totalLevels = LevelsPerChapter;

            int gStart = (chapter - 1) * LevelsPerChapter + 1;
            int gEnd = chapter * LevelsPerChapter;

            cfg.hpMultMin = GeomGlobal(hpMultGlobalMin, hpMultGlobalMax, gStart);
            cfg.hpMultMax = GeomGlobal(hpMultGlobalMin, hpMultGlobalMax, gEnd);

            cfg.targetScoreMin = Mathf.RoundToInt(GeomGlobal(targetScoreGlobalMin, targetScoreGlobalMax, gStart));
            cfg.targetScoreMax = Mathf.RoundToInt(GeomGlobal(targetScoreGlobalMin, targetScoreGlobalMax, gEnd));

            float chT = (chapter - 1) / (float)(TotalChapters - 1);
            cfg.pressureMax = Mathf.RoundToInt(Mathf.Lerp(pressureMaxCh1, pressureMaxCh30, chT));
            cfg.pressureAvgStart = Mathf.Lerp(pressureAvgStartCh1, pressureAvgStartCh30, chT);
            cfg.pressureAvgStep = pressureAvgStep;
            cfg.pressureAvgCap = cfg.pressureMax * 0.9f;

            cfg.spawnIntervalEarly = Mathf.Lerp(spawnEarlyCh1, spawnEarlyCh30, chT);
            cfg.spawnIntervalLate = Mathf.Max(0.3f, Mathf.Lerp(spawnLateCh1, spawnLateCh30, chT));
            cfg.spawnIntervalJitter = spawnJitter;

            cfg.minDurationStart = Mathf.Lerp(minDurStartCh1, minDurStartCh30, chT);
            cfg.minDurationEnd = Mathf.Lerp(minDurEndCh1, minDurEndCh30, chT);
            cfg.maxDurationStart = Mathf.Lerp(maxDurStartCh1, maxDurStartCh30, chT);
            cfg.maxDurationEnd = Mathf.Lerp(maxDurEndCh1, maxDurEndCh30, chT);

            AssignEggCaps(cfg, chapter);
            AssignBirdMix(cfg, chT);
            AssignVarianceDefaults(cfg, chT);

            if (cfg.spinLevelIndices == null || cfg.spinLevelIndices.Length == 0)
                cfg.spinLevelIndices = new[] { 0, 4, 9, 14 };
        }

        // Lerps the bird mix smoothly across the campaign. Early chapters lean B1/B2 (easy targets);
        // late chapters add B3/B4 (high-tier eggs) for endgame variety.
        static void AssignBirdMix(ChapterProgressionConfig cfg, float chT)
        {
            cfg.b1WeightStart = Mathf.Lerp(0.65f, 0.30f, chT);
            cfg.b1WeightEnd   = Mathf.Lerp(0.45f, 0.20f, chT);
            cfg.b2WeightStart = Mathf.Lerp(0.28f, 0.30f, chT);
            cfg.b2WeightEnd   = Mathf.Lerp(0.32f, 0.30f, chT);
            cfg.b3WeightStart = Mathf.Lerp(0.06f, 0.25f, chT);
            cfg.b3WeightEnd   = Mathf.Lerp(0.16f, 0.30f, chT);
            cfg.b4WeightStart = Mathf.Lerp(0.01f, 0.15f, chT);
            cfg.b4WeightEnd   = Mathf.Lerp(0.07f, 0.20f, chT);
            cfg.perSpawnWeightJitter = 0.25f;
        }

        // Variance widens slightly in late chapters — early chapters stay clean to teach mechanics.
        static void AssignVarianceDefaults(ChapterProgressionConfig cfg, float chT)
        {
            cfg.pressureVariancePercent = Mathf.Lerp(0.10f, 0.20f, chT);
            cfg.pressureNoiseFrequency  = Mathf.Lerp(0.14f, 0.22f, chT);
            cfg.replayHpStep            = 0.02f;
            cfg.replayPressureStep      = 0.03f;
            cfg.replayMaxBumps          = 3;
            cfg.replayJitterPercent     = 0.5f;
        }

        // Global geometric interpolation: value at global level g (1..GlobalLevels)
        static float GeomGlobal(float min, float max, int globalLevel)
        {
            if (min <= 0f || max <= 0f) return Mathf.Lerp(min, max, (globalLevel - 1) / (float)(GlobalLevels - 1));
            float t = (globalLevel - 1) / (float)(GlobalLevels - 1);
            return min * Mathf.Pow(max / min, t);
        }

        static void AssignEggCaps(ChapterProgressionConfig cfg, int chapter)
        {
            if (chapter <= 2)
            {
                cfg.maxE4Start = 0; cfg.maxE4End = 0;
                cfg.maxE3Start = 0; cfg.maxE3End = 0;
                cfg.maxE2Start = chapter == 1 ? 1 : 2;
                cfg.maxE2End = chapter == 1 ? 3 : 4;
            }
            else if (chapter <= 5)
            {
                cfg.maxE4Start = 0; cfg.maxE4End = 0;
                cfg.maxE3Start = 1; cfg.maxE3End = 2;
                cfg.maxE2Start = 2; cfg.maxE2End = 4;
            }
            else if (chapter <= 10)
            {
                cfg.maxE4Start = 0; cfg.maxE4End = 1;
                cfg.maxE3Start = 1; cfg.maxE3End = 3;
                cfg.maxE2Start = 2; cfg.maxE2End = 3;
            }
            else if (chapter <= 20)
            {
                cfg.maxE4Start = 1; cfg.maxE4End = 2;
                cfg.maxE3Start = 2; cfg.maxE3End = 3;
                cfg.maxE2Start = 2; cfg.maxE2End = 3;
            }
            else
            {
                cfg.maxE4Start = 2; cfg.maxE4End = 3;
                cfg.maxE3Start = 2; cfg.maxE3End = 4;
                cfg.maxE2Start = 2; cfg.maxE2End = 4;
            }
        }

        static void EnsureFolderPath(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] parts = folderPath.Split('/');
            string accumulated = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{accumulated}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(accumulated, parts[i]);
                accumulated = next;
            }
        }
    }
}
#endif
