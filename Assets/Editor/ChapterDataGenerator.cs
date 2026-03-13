// Place this file in: Assets/Editor/ChapterDataGenerator.cs

using UnityEngine;
using UnityEditor;
using System.IO;

public class ChapterDataGenerator : EditorWindow
{
    private GameObject parentObject;
    private string savePath = "Assets/Resources/Chapters";

    [MenuItem("BirdHunter/Generate Chapter Data From Children")]
    public static void ShowWindow()
    {
        GetWindow<ChapterDataGenerator>("Chapter Data Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chapter Data Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        parentObject = (GameObject)EditorGUILayout.ObjectField(
            "Parent Object", parentObject, typeof(GameObject), true);

        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();

        if (parentObject != null)
        {
            EditorGUILayout.HelpBox(
                $"Found {parentObject.transform.childCount} children. Will create {parentObject.transform.childCount} ChapterData assets.",
                MessageType.Info);
        }

        EditorGUI.BeginDisabledGroup(parentObject == null);
        if (GUILayout.Button("Generate ChapterData ScriptableObjects"))
        {
            GenerateChapterData();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void GenerateChapterData()
    {
        if (parentObject == null)
        {
            Debug.LogError("No parent object selected!");
            return;
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        int childCount = parentObject.transform.childCount;
        int created = 0;
        int skipped = 0;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = parentObject.transform.GetChild(i);
            string childName = $"{i + 1}-{child.name}";
            string assetPath = $"{savePath}/{childName}.asset";

            // Skip if asset already exists
            if (File.Exists(assetPath))
            {
                Debug.LogWarning($"Skipped '{childName}' — asset already exists at {assetPath}");
                skipped++;
                continue;
            }

            // Create the ScriptableObject
            ChapterData chapterData = ScriptableObject.CreateInstance<ChapterData>();
            chapterData.worldName = childName;

            AssetDatabase.CreateAsset(chapterData, assetPath);
            created++;

            Debug.Log($"Created ChapterData: {childName} at {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Generation Complete",
            $"Done!\n\nCreated: {created}\nSkipped (already existed): {skipped}\n\nSaved to: {savePath}",
            "OK");

        // Ping the folder in Project window
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(savePath);
        if (folder != null) EditorGUIUtility.PingObject(folder);
    }
}