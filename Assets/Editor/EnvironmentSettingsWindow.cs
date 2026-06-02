using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window to pick the Unity Services (UGS) environment and the build settings
/// that pair with it. The environment is written to Resources/environment_config.json
/// (read at runtime by <see cref="UnityEnvironment"/>); the build flags map to
/// <see cref="EditorUserBuildSettings"/>. Open via Tools ▸ Unity Services ▸ Environment.
/// </summary>
public class EnvironmentSettingsWindow : EditorWindow
{
    private const string ConfigPath = "Assets/Resources/environment_config.json";
    private static readonly string[] Presets = { "production", "development" };

    private string _selected;

    [MenuItem("Tools/Unity Services/Environment")]
    private static void Open()
    {
        var window = GetWindow<EnvironmentSettingsWindow>(true, "UGS Environment");
        window.minSize = new Vector2(360, 320);
        window._selected = ReadCurrent();
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(_selected)) _selected = ReadCurrent();

        DrawBanner();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Applied at runtime when UnityServices initializes (auth boot step). " +
            "Saved value: " + ReadCurrent(), MessageType.Info);

        foreach (var preset in Presets)
        {
            if (GUILayout.Toggle(_selected == preset, " " + preset, EditorStyles.radioButton))
                _selected = preset;
        }

        EditorGUILayout.LabelField("Custom", EditorStyles.miniBoldLabel);
        _selected = EditorGUILayout.TextField(_selected);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_selected) || _selected == ReadCurrent()))
        {
            if (GUILayout.Button("Apply Environment & Save", GUILayout.Height(28)))
                Write(_selected.Trim());
        }

        EditorGUILayout.Space(12);
        DrawBuildSettings();
    }

    private void DrawBanner()
    {
        bool isProd = ReadCurrent() == "production";
        var rect = EditorGUILayout.GetControlRect(false, 34);
        EditorGUI.DrawRect(rect, isProd ? new Color(0.13f, 0.42f, 0.20f) : new Color(0.62f, 0.45f, 0.10f));

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        EditorGUI.LabelField(rect,
            isProd ? "PRODUCTION" : $"{ReadCurrent().ToUpperInvariant()}  —  NOT FOR RELEASE",
            style);
    }

    private void DrawBuildSettings()
    {
        EditorGUILayout.LabelField("Build Settings (this machine)", EditorStyles.boldLabel);

        bool dev = EditorGUILayout.Toggle("Development Build", EditorUserBuildSettings.development);
        if (dev != EditorUserBuildSettings.development) EditorUserBuildSettings.development = dev;

        using (new EditorGUI.DisabledScope(!dev))
        {
            bool debug = EditorGUILayout.Toggle("Script Debugging", EditorUserBuildSettings.allowDebugging);
            if (debug != EditorUserBuildSettings.allowDebugging) EditorUserBuildSettings.allowDebugging = debug;

            bool prof = EditorGUILayout.Toggle("Autoconnect Profiler", EditorUserBuildSettings.connectProfiler);
            if (prof != EditorUserBuildSettings.connectProfiler) EditorUserBuildSettings.connectProfiler = prof;
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Match Build Settings to Saved Environment", GUILayout.Height(24)))
            MatchBuildSettings(ReadCurrent() == "production");
    }

    private static void MatchBuildSettings(bool production)
    {
        EditorUserBuildSettings.development = !production;
        EditorUserBuildSettings.allowDebugging = !production;
        EditorUserBuildSettings.connectProfiler = false;
        Debug.Log($"[UGS] Build settings matched to {(production ? "production" : "development")}.");
    }

    private static string ReadCurrent()
    {
        if (!File.Exists(ConfigPath)) return UnityEnvironment.Default;
        return File.ReadAllText(ConfigPath).Contains("development") ? "development" : "production";
    }

    private static void Write(string env)
    {
        File.WriteAllText(ConfigPath, $"{{\"environmentName\":\"{env}\"}}\n");
        AssetDatabase.ImportAsset(ConfigPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[UGS] Environment set to '{env}'.");
        EditorUtility.DisplayDialog("UGS Environment", $"Environment set to:\n\n{env}", "OK");
    }
}
