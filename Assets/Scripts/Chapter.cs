using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chapter", menuName = "Game/Chapter")]
public class Chapter : ScriptableObject
{
    public int chapterNumber;
    public List<LevelData> levels;
}
