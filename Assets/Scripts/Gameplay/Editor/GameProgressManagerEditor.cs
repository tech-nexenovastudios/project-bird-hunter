using Gameplay.Managers;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(GameProgressManager))]
public class GameProgressManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GameProgressManager mgr = (GameProgressManager)target;
        
        EditorGUILayout.Space();
        GUILayout.Label("🔧 Debug Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Test Load Ch1 L1"))
        {
            var profile = mgr.LoadLevelProfile(1, 1);
            Debug.Log(profile != null ? $"✅ Loaded Ch1 L1: {profile.globalLevel}" : "❌ Missing");
        }
        
        if (GUILayout.Button("Test Load Ch30 L20"))
        {
            var profile = mgr.LoadLevelProfile(30, 20);
            Debug.Log(profile != null ? $"✅ Loaded Ch30 L20: {profile.globalLevel}" : "❌ Missing");
        }

        if (GUILayout.Button("Test Save"))
        {
            mgr.SaveProgress();
            Debug.Log("Saved");
        }

        if (GUILayout.Button("Test Load"))
        {
            mgr.LoadProgress();
            Debug.Log("Loaded");
        }
        
        if (GUILayout.Button("Reset"))
        {
            mgr.ResetProgress();
        }
    }
}
#endif