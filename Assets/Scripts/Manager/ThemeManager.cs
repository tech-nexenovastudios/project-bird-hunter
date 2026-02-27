using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChapterTheme
{
    public int chapterNumber;
    public Sprite background;
}

public class ThemeManager : MonoBehaviour
{
    public List<ChapterTheme> chapterThemes;

    public static ThemeManager Instance;

    private Dictionary<int, Sprite> themeDict;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional if you want it to persist
            BuildThemeDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildThemeDictionary()
    {
        themeDict = new Dictionary<int, Sprite>();
        foreach (var theme in chapterThemes)
        {
            themeDict[theme.chapterNumber] = theme.background;
        }
    }

    public Sprite GetThemeForChapter(int chapterNumber)
    {
        if (themeDict != null && themeDict.TryGetValue(chapterNumber, out var sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"No background assigned for Chapter {chapterNumber}");
        return null;
    }
}
