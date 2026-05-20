#if UNITY_6000_3_OR_NEWER
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class PlayToolbarExtension
{
    private const string SceneDropdownId = "BirdHunter/SceneSwitcher";
    private const string BootButtonId = "BirdHunter/PlayFromBoot";
    private const string ShownPrefKey = "BirdHunter.ToolbarShown.v2";

    private const string RestartIconName = "d_Refresh";
    private const string StopIconName = "d_PreMatQuad";

    private static MethodInfo _showAllMethod;

    [MainToolbarElement(SceneDropdownId, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 2)]
    private static MainToolbarElement CreateSceneDropdown()
    {
        var icon = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;
        var content = new MainToolbarContent(ActiveSceneShortName(), icon, "Switch active scene (from Build Settings)");
        return new MainToolbarDropdown(content, OpenSceneMenu);
    }

    [MainToolbarElement(BootButtonId, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 1)]
    private static MainToolbarElement CreateBootButton()
    {
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        var icon = EditorGUIUtility.IconContent(playing ? StopIconName : RestartIconName).image as Texture2D;
        string tooltip = playing
            ? "Stop Play mode"
            : "Open scene at build index 0 and enter Play mode";
        var content = new MainToolbarContent(string.Empty, icon, tooltip);
        return new MainToolbarButton(content, OnBootButtonClicked);
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        if (EditorPrefs.GetBool(ShownPrefKey, false)) return;
        EditorApplication.delayCall += ShowOnce;
    }

    [MenuItem("Tools/Bird Hunter/Show Toolbar Elements")]
    private static void ShowToolbarElementsMenu() => ShowOnce();

    private static void ShowOnce()
    {
        if (!ShowElement(SceneDropdownId) || !ShowElement(BootButtonId)) return;
        EditorPrefs.SetBool(ShownPrefKey, true);
    }

    private static bool ShowElement(string id)
    {
        if (_showAllMethod == null)
        {
            _showAllMethod = typeof(MainToolbar).GetMethod(
                "ShowAll", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (_showAllMethod == null)
            {
                Debug.LogWarning("[PlayToolbarExtension] MainToolbar.ShowAll not found in this Unity version.");
                return false;
            }
        }
        _showAllMethod.Invoke(null, new object[] { id });
        return true;
    }

    private static void OnActiveSceneChanged(Scene _, Scene __) => MainToolbar.Refresh(SceneDropdownId);

    private static void OnPlayModeStateChanged(PlayModeStateChange _) => MainToolbar.Refresh(BootButtonId);

    private static void OnBootButtonClicked()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        PlayFromBuildIndexZero();
    }

    private static void OpenSceneMenu(Rect rect)
    {
        var menu = new GenericMenu();
        var scenes = EditorBuildSettings.scenes;
        string activePath = SceneManager.GetActiveScene().path;
        int visibleIndex = 0;
        bool any = false;

        for (int i = 0; i < scenes.Length; i++)
        {
            var s = scenes[i];
            if (!s.enabled) continue;
            any = true;
            string label = $"{visibleIndex++}: {Path.GetFileNameWithoutExtension(s.path)}";
            string path = s.path;
            menu.AddItem(new GUIContent(label), activePath == path, () => OpenScene(path));
        }

        if (!any) menu.AddDisabledItem(new GUIContent("Add scenes in File > Build Settings"));
        menu.DropDown(rect);
    }

    private static string ActiveSceneShortName()
    {
        string path = SceneManager.GetActiveScene().path;
        return string.IsNullOrEmpty(path) ? "Untitled" : Path.GetFileNameWithoutExtension(path);
    }

    private static void OpenScene(string path, bool enterPlayMode = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (enterPlayMode) EditorApplication.isPlaying = true;
    }

    private static void PlayFromBuildIndexZero()
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("No Scenes", "Add scenes in File > Build Settings first.", "OK");
            return;
        }

        var first = scenes[0];
        if (!first.enabled)
        {
            EditorUtility.DisplayDialog("Scene Disabled",
                $"Scene at build index 0 ({first.path}) is disabled in Build Settings.", "OK");
            return;
        }

        OpenScene(first.path, enterPlayMode: true);
    }
}
#endif
