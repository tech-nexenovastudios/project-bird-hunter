#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor conveniences for retrieving and managing the feature logs.</summary>
internal static class GameLoggerMenu
{
    [MenuItem("Tools/Game Logger/Open Log Folder")]
    private static void OpenLogFolder()
    {
        GameLogger.Flush();
        string dir = GameLogger.LogDirectory;
        Directory.CreateDirectory(dir);
        EditorUtility.RevealInFinder(dir);
    }

    [MenuItem("Tools/Game Logger/Open Economy Log")]
    private static void OpenEconomyLog()
    {
        GameLogger.Flush();
        string path = GameLogger.GetLogPath(LogCategory.Economy);
        if (File.Exists(path)) EditorUtility.RevealInFinder(path);
        else EditorUtility.DisplayDialog("Game Logger", "No economy log yet — play and trigger some currency changes first.", "OK");
    }

    [MenuItem("Tools/Game Logger/Clear All Logs")]
    private static void ClearLogs()
    {
        if (EditorUtility.DisplayDialog("Game Logger", "Delete all log files?", "Delete", "Cancel"))
            GameLogger.ClearFiles();
    }
}
#endif
