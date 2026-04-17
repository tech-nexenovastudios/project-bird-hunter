using Gameplay.Levels;
using UnityEngine;

public class ChapterData : ScriptableObject
{
    public string worldName;
    [SerializeField] private Sprite chapterBackground;
    [SerializeField] private Sprite platform;
    public LevelProfile[] levelProfiles;

   
    public Sprite ChapterBackground => chapterBackground;
    public Sprite Platform => platform;
}