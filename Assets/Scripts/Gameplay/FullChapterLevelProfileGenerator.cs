using Gameplay.Birds;
using Gameplay.Eggs;
using Gameplay.Levels;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay
{
#if UNITY_EDITOR
    public class FullChapterLevelProfileGenerator : EditorWindow
    {
        private EggTierConfig e1, e2, e3, e4;
        private BirdConfig b1, b2, b3, b4;
        private AdaptiveDifficultyConfig adaptiveConfig;

        private string baseFolder = "Assets/Resources/Data/GeneratedLevels";

        // Must match GameProgress.SpinLevels (0-indexed within chapter)
        private static readonly int[] SpinLevelIndices = { 0, 4, 9, 14 };

        [MenuItem("BirdHunter/Generate ALL 30 Chapters (600 Levels)")]
        public static void ShowWindow()
            => GetWindow<FullChapterLevelProfileGenerator>("Full Level Generator");

        void OnGUI()
        {
            GUILayout.Label("Bird Hunter FULL Progression Generator", EditorStyles.boldLabel);
            GUILayout.Label("Generates 600 Level Profiles (30 Chapters × 20 Levels)", EditorStyles.helpBox);
            EditorGUILayout.Space();

            GUILayout.Label("Required Configs:", EditorStyles.boldLabel);
            e1 = (EggTierConfig)EditorGUILayout.ObjectField("E1 Config", e1, typeof(EggTierConfig), false);
            e2 = (EggTierConfig)EditorGUILayout.ObjectField("E2 Config", e2, typeof(EggTierConfig), false);
            e3 = (EggTierConfig)EditorGUILayout.ObjectField("E3 Config", e3, typeof(EggTierConfig), false);
            e4 = (EggTierConfig)EditorGUILayout.ObjectField("E4 Config", e4, typeof(EggTierConfig), false);
            EditorGUILayout.Space();

            b1 = (BirdConfig)EditorGUILayout.ObjectField("B1 Config", b1, typeof(BirdConfig), false);
            b2 = (BirdConfig)EditorGUILayout.ObjectField("B2 Config", b2, typeof(BirdConfig), false);
            b3 = (BirdConfig)EditorGUILayout.ObjectField("B3 Config", b3, typeof(BirdConfig), false);
            b4 = (BirdConfig)EditorGUILayout.ObjectField("B4 Config", b4, typeof(BirdConfig), false);
            EditorGUILayout.Space();

            adaptiveConfig = (AdaptiveDifficultyConfig)EditorGUILayout.ObjectField(
                "Adaptive Config", adaptiveConfig, typeof(AdaptiveDifficultyConfig), false);
            EditorGUILayout.Space();

            baseFolder = EditorGUILayout.TextField("Base Folder", baseFolder);
            GUILayout.Label($"Output: {baseFolder}/Chapter1/Levels/Ch1_L01.asset ...", EditorStyles.helpBox);
            EditorGUILayout.Space();

            GUILayout.Label("⚠️ Creates 600+ assets. Commit to version control first!", EditorStyles.helpBox);
            EditorGUILayout.Space();

            if (GUILayout.Button("🚀 GENERATE ALL 30 CHAPTERS (600 Levels)", GUILayout.Height(50)))
                if (ValidateInputs()) GenerateAllChapters();

            EditorGUILayout.Space();
            if (GUILayout.Button("📊 Generate Summary Only (No Assets)", GUILayout.Height(30)))
                GenerateSummaryOnly();
        }

        bool ValidateInputs()
        {
            if (e1 == null || e2 == null || e3 == null || e4 == null)
            {
                EditorUtility.DisplayDialog("❌ Missing", "Assign all E1–E4 Egg Tier Configs!", "OK");
                return false;
            }
            if (b1 == null || b2 == null || b3 == null || b4 == null)
            {
                EditorUtility.DisplayDialog("❌ Missing", "Assign all B1–B4 Bird Configs!", "OK");
                return false;
            }
            if (adaptiveConfig == null)
            {
                EditorUtility.DisplayDialog("❌ Missing", "Assign Adaptive Difficulty Config!", "OK");
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────
        // Generation
        // ─────────────────────────────────────────────
        void GenerateAllChapters()
        {
            int total = 0;
            EditorUtility.DisplayProgressBar("Generating Levels", "Starting...", 0f);

            for (int chapter = 1; chapter <= 30; chapter++)
            {
                string levelsFolder = $"{baseFolder}/Chapter{chapter}/Levels";

                EnsureFolder("Assets", "Resources");
                EnsureFolder("Assets/Resources", "Data");
                EnsureFolder("Assets/Resources/Data", "GeneratedLevels");
                EnsureFolder("Assets/Resources/Data/GeneratedLevels", $"Chapter{chapter}");
                EnsureFolder($"Assets/Resources/Data/GeneratedLevels/Chapter{chapter}", "Levels");

                for (int level = 1; level <= 20; level++)
                {
                    int globalLevel = (chapter - 1) * 20 + level;
                    int levelIndex = level - 1;

                    var profile = ScriptableObject.CreateInstance<LevelProfile>();
                    var v = CalculateLevelValues(globalLevel, chapter, levelIndex);

                    profile.chapter = chapter;
                    profile.globalLevel = globalLevel;
                    profile.targetScore = v.score;
                    profile.minDuration = v.minDuration;
                    profile.maxDuration = v.maxDuration;
                    profile.pressureMax = v.pMax;
                    profile.pressureAvg = v.pAvg;
                    profile.maxE4 = v.maxE4;
                    profile.maxE3 = v.maxE3;
                    profile.maxE2 = v.maxE2;
                    profile.spawnIntervalMin = v.spawnMin;
                    profile.spawnIntervalMax = v.spawnMax;
                    profile.expectedDps = v.dps;
                    profile.hpMultiplier = v.hpMult;

                    string assetPath = $"{levelsFolder}/Ch{chapter}_L{level:D2}.asset";
                    AssetDatabase.CreateAsset(profile, assetPath);
                    EditorUtility.SetDirty(profile);

                    total++;
                    EditorUtility.DisplayProgressBar(
                        $"Chapter {chapter}/30",
                        $"Level {level}/20 (Global {globalLevel})", total / 600f);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ Generated {total} Level Profiles across 30 chapters!");
            EditorUtility.DisplayDialog("Complete! 🎉",
                $"Created {total} Level Profiles\nFolder: {baseFolder}", "OK");
        }

        void EnsureFolder(string parent, string folderName)
        {
            string full = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        void GenerateSummaryOnly()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Bird Hunter Complete Progression Summary");
            sb.AppendLine("========================================");

            for (int chapter = 1; chapter <= 30; chapter++)
            {
                sb.AppendLine($"\nCHAPTER {chapter}");
                sb.AppendLine("--------");
                for (int level = 1; level <= 20; level++)
                {
                    int globalLevel = (chapter - 1) * 20 + level;
                    int levelIndex = level - 1;
                    var v = CalculateLevelValues(globalLevel, chapter, levelIndex);
                    bool isSpin = System.Array.IndexOf(SpinLevelIndices, levelIndex) >= 0;

                    sb.AppendLine($"  L{level:D2} (G{globalLevel}){(isSpin ? " 🎰 SPIN" : "")}:");
                    sb.AppendLine($"    Score={v.score}  DPS={v.dps}  HP×{v.hpMult:F2}");
                    sb.AppendLine($"    Pressure={v.pMax}/{v.pAvg}  E4≤{v.maxE4} E3≤{v.maxE3} E2≤{v.maxE2}");
                    sb.AppendLine($"    Spawn={v.spawnMin:F2}–{v.spawnMax:F2}s  Dur={v.minDuration:F0}–{v.maxDuration:F0}s");
                }
            }

            string path = "Assets/LevelProgressionSummary.txt";
            System.IO.File.WriteAllText(path, sb.ToString());
            AssetDatabase.ImportAsset(path);
            Debug.Log("📊 Summary saved: " + path);
        }

        // ─────────────────────────────────────────────
        // Core Calculation
        //
        // chapterT (0..1): global progression across 30 chapters
        // intraT   (0..1): difficulty position within current chapter
        //
        // Spin spike schedule (levelIndex, 0-based):
        //   0  = chapter entry  → intraT 0.00 (easiest)
        //   4  = spin 1 spike   → intraT 0.50
        //   9  = spin 2 spike   → intraT 0.65
        //   14 = spin 3 spike   → intraT 0.80
        //   19 = chapter boss   → intraT 1.00
        //
        // Screen egg count budget (max 15 simultaneous):
        //   Each E4 = 1 slot, E3 = 2, E2 = 4 (splits into 2 E1s each)
        //   Budget formula: maxE4×1 + maxE3×2 + maxE2×4 ≤ 12 (+3 for transient E1 splits)
        // ─────────────────────────────────────────────
        (int score, int pMax, int pAvg, int maxE4, int maxE3, int maxE2,
         float minDuration, float maxDuration,
         float spawnMin, float spawnMax,
         int dps, float hpMult)
        CalculateLevelValues(int globalLevel, int chapter, int levelIndex)
        {
            float chapterT = (chapter - 1) / 29f;
            float intraT = GetIntraDifficultyMultiplier(levelIndex);

            // Combined difficulty: chapterT is dominant, intraT adds local spikes
            float diff = chapterT * 0.75f + intraT * 0.25f;

            // ── HP Multiplier: ×1.0 (Ch1 L1) → ×10.0 (Ch30 L20) ──
            float hpMult = 1.0f + 9.0f * EaseInOutCubic(diff) * (1f + 0.12f * intraT);
            hpMult = Mathf.Round(hpMult * 20f) / 20f; // snap to 0.05 steps

            // ── Expected DPS: scales with hpMult so score stays reachable ──
            float dpsMult = 1.0f + 9.5f * EaseInOutCubic(diff) * (1f + 0.08f * intraT);
            int dps = Mathf.RoundToInt(12f * dpsMult);

            // ── Spawn Interval: 2.0s (Ch1 L1) → 0.3s (Ch30 L20) ──
            float spawnBase = Mathf.Lerp(2.0f, 0.25f, EaseInCubic(diff * (1f + 0.12f * intraT)));
            spawnBase = Mathf.Max(0.2f, spawnBase);
            float spawnMin = spawnBase;
            float spawnMax = Mathf.Max(spawnMin + 0.3f, spawnBase + Mathf.Lerp(2.0f, 0.3f, chapterT));

            // ── Pressure Max: calibrated to concurrent egg pressure, not lifetime ──
            // Range: 8 (Ch1 L1) → 52 (Ch30 L20)
            // Keeps birds spawning at a rate that fills but never floods the screen
            float pBase = Mathf.Lerp(8f, 48f, EaseInOutCubic(diff));
            int pMax = Mathf.Clamp(Mathf.RoundToInt(pBase * (1f + 0.18f * intraT)), 8, 56);
            int pAvg = Mathf.RoundToInt(pMax * 0.60f);

            // ── Egg Tier Caps — screen budget: maxE4×1 + maxE3×2 + maxE2×4 ≤ 12 ──
            int maxE4, maxE3, maxE2;

            if (chapter <= 2)
            {
                // Ch1–2: E1 + E2 only — learning mechanics
                // Max 3 E2s = 12 slots → up to 9 visible eggs (3 E2 + 6 E1 splits) ✅
                maxE4 = 0;
                maxE3 = 0;
                maxE2 = chapter == 1
                    ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 3f, intraT)), 1, 3)
                    : Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 4f, intraT)), 2, 4);
            }
            else if (chapter <= 5)
            {
                // Ch3–5: Introduce E3 — budget: E4=0, E3≤2, E2≤2
                // 0 + 2×2 + 2×4 = 12 → up to 12 eggs ✅
                maxE4 = 0;
                maxE3 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 2f, intraT)), 0, 2);
                maxE2 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 4f, intraT)), 1, 4);
            }
            else if (chapter <= 10)
            {
                // Ch6–10: Introduce E4 — budget: E4≤1, E3≤2, E2≤2
                // 1 + 2×2 + 2×4 = 13 → up to 14 eggs ✅
                maxE4 = intraT >= 0.5f ? 1 : 0;
                maxE3 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 3f, intraT)), 1, 3);
                maxE2 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 3f, intraT)), 1, 3);
            }
            else if (chapter <= 20)
            {
                // Ch11–20 — budget: E4≤2, E3≤2, E2≤2
                // 2 + 2×2 + 2×4 = 14 → up to 14 eggs ✅
                maxE4 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 2f, intraT)), 1, 2);
                maxE3 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 3f, intraT)), 1, 3);
                maxE2 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 3f, intraT)), 1, 3);
            }
            else
            {
                // Ch21–30 endgame — budget: E4≤2, E3≤3, E2≤2
                // 2 + 3×2 + 2×4 = 16 → pMax keeps actual count at ≤15 ✅
                maxE4 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 3f, intraT)), 2, 3);
                maxE3 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 4f, intraT)), 2, 4);
                maxE2 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 4f, intraT)), 2, 4);
            }

            // ── Target Score ──

            // Score uses exponential progression: 80 (Ch1 L1) → 2500 (Ch30 L20)
            float progressT = (globalLevel - 1) / 599f; // normalize to 0..1
            float expFactor = Mathf.Log(2500f / 80f);
            int score = Mathf.RoundToInt(80f * Mathf.Exp(progressT * expFactor) * (1f + 0.15f * intraT));

            score = (score / 10) * 10; // round to nearest 10 for clean UI
            score = Mathf.Max(50, score); // never zero

            // ── Duration ──
            float minDuration, maxDuration;
            if (chapter == 1 && levelIndex == 0)
            {
                minDuration = 20f;  // Ch1 L1: very short — instant win feeling
                maxDuration = 30f;
            }
            else if (chapter <= 5)
            {
                minDuration = Mathf.Lerp(25f, 38f, chapterT * 5f) + 4f * intraT;
                maxDuration = Mathf.Lerp(45f, 60f, chapterT * 5f);
            }
            else if (chapter <= 15)
            {
                float t = (chapterT - 0.138f) / 0.448f;
                minDuration = Mathf.Lerp(38f, 50f, t) + 5f * intraT;
                maxDuration = Mathf.Lerp(60f, 78f, t);
            }
            else
            {
                float t = (chapterT - 0.483f) / 0.517f;
                minDuration = Mathf.Lerp(50f, 60f, t) + 5f * intraT;
                maxDuration = Mathf.Lerp(78f, 95f, t);
            }

            return (score, pMax, pAvg, maxE4, maxE3, maxE2,
                    minDuration, maxDuration, spawnMin, spawnMax, dps, hpMult);
        }

        // ─────────────────────────────────────────────
        // Intra-Chapter Difficulty Multiplier (0..1)
        //
        //  0.00 = easiest (L1 — chapter entry)
        //  1.00 = hardest (L20 — chapter boss)
        //
        //  Pattern:
        //  L1        → easy entry
        //  L2–4      → gentle ramp
        //  L5  (spin)→ spike +50% vs ramp
        //  L6        → reset/relief
        //  L7–9      → medium ramp
        //  L10 (spin)→ spike
        //  L11       → reset/relief
        //  L12–14    → harder ramp
        //  L15 (spin)→ biggest pre-boss spike
        //  L16       → reset/relief
        //  L17–20    → chapter climax, peaks at 1.0
        // ─────────────────────────────────────────────
        float GetIntraDifficultyMultiplier(int levelIndex)
        {
            return levelIndex switch
            {
                0 => 0.00f,
                1 => 0.10f,
                2 => 0.22f,
                3 => 0.35f,
                4 => 0.50f,  // 🎰 Spin 1
                5 => 0.10f,  // relief
                6 => 0.28f,
                7 => 0.40f,
                8 => 0.52f,
                9 => 0.65f,  // 🎰 Spin 2
                10 => 0.20f,  // relief
                11 => 0.42f,
                12 => 0.55f,
                13 => 0.65f,
                14 => 0.80f,  // 🎰 Spin 3
                15 => 0.30f,  // relief
                16 => 0.60f,
                17 => 0.75f,
                18 => 0.88f,
                19 => 1.00f,  // 👑 Chapter boss
                _ => 0f
            };
        }

        // S-curve: smooth ramp for HP/pressure — slow start, slow end
        float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        // Fast end: for spawn intervals — endgame punishes harder
        float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }
    }
#endif
}
