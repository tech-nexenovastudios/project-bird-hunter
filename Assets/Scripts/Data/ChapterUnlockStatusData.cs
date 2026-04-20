using System;
using System.Collections.Generic;

[Serializable]
public class ChapterUnlockStatusData
{
    public Dictionary<string, bool> chapters;

    public ChapterUnlockStatusData()
    {
        chapters = new Dictionary<string, bool>();
    }

    public bool IsUnlocked(string chapterId)
    {
        return chapters.TryGetValue(chapterId, out var unlocked) && unlocked;
    }

    public void SetUnlocked(string chapterId, bool unlocked)
    {
        chapters[chapterId] = unlocked;
    }

    /// <summary>
    /// Returns the highest chapter index that's unlocked.
    /// Chapter IDs are expected to be numeric strings ("0", "1", "2", ...).
    /// </summary>
    public int GetHighestUnlockedIndex()
    {
        int highest = 0;
        foreach (var kvp in chapters)
        {
            if (kvp.Value && int.TryParse(kvp.Key, out int idx) && idx > highest)
                highest = idx;
        }
        return highest;
    }
}