using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Gameplay.PowerUps;

[CustomEditor(typeof(PowerupDatabase))]
public class PowerUpDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector (the list and header)
        DrawDefaultInspector();

        PowerupDatabase database = (PowerupDatabase)target;

        EditorGUILayout.Space(10);
        GUI.backgroundColor = Color.cyan;

        if (GUILayout.Button("Find & Add All PowerupConfigs", GUILayout.Height(30)))
        {
            RefreshDatabase(database);
        }
    }

    private void RefreshDatabase(PowerupDatabase database)
    {
        // Find all GUIDs of assets with the PowerupConfig type
        string[] guids = AssetDatabase.FindAssets("t:PowerupConfig");
        List<PowerupConfig> foundConfigs = new List<PowerupConfig>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PowerupConfig config = AssetDatabase.LoadAssetAtPath<PowerupConfig>(path);
            if (config != null)
            {
                foundConfigs.Add(config);
            }
        }

        // Apply to the ScriptableObject
        database.SetPowerups(foundConfigs);
        
        // Mark as dirty so Unity saves the changes
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully synchronized {foundConfigs.Count} powerups to the database.");
    }
}