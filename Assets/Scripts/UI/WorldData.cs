using System.Collections.Generic;
using Gameplay.Levels;
using NUnit.Framework;
using UnityEngine;


[CreateAssetMenu(fileName = "Chapter Data", menuName = "BirdHunter/ChapterData")]
public class ChapterData : ScriptableObject
{
    public string worldName;
    public LevelProfile[] levelProfiles;   
}