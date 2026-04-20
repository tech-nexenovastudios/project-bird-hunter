using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chapters Config", menuName = "BirdHunter/ChaptersConfig")]
public class ChaptersConfig : ScriptableObject
{
    public List<ChapterData> worlds;
    public int GetCurrentChapterIndex(ChapterData world) => worlds.IndexOf(world);
    public ChapterData GetChapterData(int index) => worlds[index];
    public ChapterData GetNextChapterData(int index) => worlds[index + 1];
    public int GetWorldCount() => worlds.Count;
}