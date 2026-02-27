using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chapters Config", menuName = "BirdHunter/ChaptersConfig")]
public class ChaptersConfig : ScriptableObject
{
    public List<ChapterData> worlds;
    public int GetCurrentWorldIndex(ChapterData world) => worlds.IndexOf(world);
    public ChapterData GetWorldData(int index) => worlds[index];
    public int GetWorldCount() => worlds.Count;
}