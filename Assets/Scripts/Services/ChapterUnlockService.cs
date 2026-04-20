using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ChapterUnlockService
{
    // ─── State ───
    public bool IsReady { get; private set; }
    public int HighestUnlockedIndex { get; private set; }
    public bool DevUnlockAll { get; set; } = false;

    private ChapterUnlockStatusData data;
    private int totalChapterCount;

    // ─── Initialization ───

    /// <summary>
    /// Called by BootController after CloudDatabase has loaded.
    /// Pulls the already-fetched data from CloudDatabase and wires defaults.
    /// </summary>
    public void Initialize(ChapterUnlockStatusData loadedData, int chapterCount, int[] defaultUnlocked)
    {
        data = loadedData ?? new ChapterUnlockStatusData();
        totalChapterCount = chapterCount;

        // Apply dev override
        if (DevUnlockAll)
        {
            for (int i = 0; i < chapterCount; i++)
                data.SetUnlocked(i.ToString(), true);
        }
        // Apply defaults only if no cloud data exists yet
        else if (data.chapters.Count == 0 && defaultUnlocked != null)
        {
            foreach (int i in defaultUnlocked)
                data.SetUnlocked(i.ToString(), true);
        }

        HighestUnlockedIndex = data.GetHighestUnlockedIndex();
        IsReady = true;
    }

    // ─── Queries ───

    public bool IsUnlocked(int chapterIndex)
    {
        return data != null && data.IsUnlocked(chapterIndex.ToString());
    }

    // ─── Mutations ───

    public async UniTask<bool> UnlockChapterAsync(int chapterIndex)
    {
        if (!IsReady || data == null) return false;
        if (IsUnlocked(chapterIndex)) return false;

        data.SetUnlocked(chapterIndex.ToString(), true);

        int previousHighest = HighestUnlockedIndex;
        HighestUnlockedIndex = data.GetHighestUnlockedIndex();

        EventBus.Publish(new ChapterUnlockedEvent { chapterIndex = chapterIndex });

        if (HighestUnlockedIndex != previousHighest)
        {
            EventBus.Publish(new HighestChapterChangedEvent
            {
                newHighestIndex = HighestUnlockedIndex
            });
        }

        if (!DevUnlockAll)
            await SaveAsync();

        return true;
    }

    public async UniTask UnlockAllAsync()
    {
        if (!IsReady || data == null) return;

        for (int i = 0; i < totalChapterCount; i++)
        {
            if (!data.IsUnlocked(i.ToString()))
            {
                data.SetUnlocked(i.ToString(), true);
                EventBus.Publish(new ChapterUnlockedEvent { chapterIndex = i });
            }
        }

        int previousHighest = HighestUnlockedIndex;
        HighestUnlockedIndex = data.GetHighestUnlockedIndex();

        if (HighestUnlockedIndex != previousHighest)
        {
            EventBus.Publish(new HighestChapterChangedEvent
            {
                newHighestIndex = HighestUnlockedIndex
            });
        }

        if (!DevUnlockAll)
            await SaveAsync();
    }

    // ─── Persistence ───

    private async UniTask SaveAsync()
    {
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(CloudKeys.CHAPTER_UNLOCK_STATUS, data);
            Debug.Log("[ChapterUnlockService] Saved to cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChapterUnlockService] Save failed: {ex.Message}");
        }
    }
}