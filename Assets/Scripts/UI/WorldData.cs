using System.Collections.Generic;
using Gameplay.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Chapter Data", menuName = "BirdHunter/ChapterData")]
public class ChapterData : ScriptableObject
{
    public string worldName;
    public Sprite chapterBackground;
    public Sprite platform;
    public LevelProfile[] levelProfiles;
    
}