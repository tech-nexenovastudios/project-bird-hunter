using System;
using Cysharp.Threading.Tasks;
using Gameplay.PowerUps;
using UnityEngine;

public class PowerupUnlockService
{
    public bool IsReady { get; private set; }

    private PowerupUnlockStatusData data;
    private PowerupDatabase database;
    private int lastChapter = 0;
    private int lastChapterLevel = 0;

    // ─── Initialization ───

    public void Initialize(PowerupUnlockStatusData loadedData, PowerupDatabase powerupDatabase)
    {
        data = loadedData ?? new PowerupUnlockStatusData();
        database = powerupDatabase;

        if (database == null)
        {
            Debug.LogError("[PowerupUnlockService] PowerupDatabase is null.");
            return;
        }

        // Seed initially-unlocked powerups (skip if already set in data)
        foreach (var cfg in database.allPowerups)
        {
            if (cfg.initiallyUnlocked && !data.IsUnlocked(cfg.id))
                data.SetUnlocked(cfg.id, true);
        }

        IsReady = true;
        EventBus.Publish(new PowerupStatesLoadedEvent());
    }

    // ─── Queries ───

    public bool IsUnlocked(string id) => data != null && data.IsUnlocked(id);

    public bool IsUnlocked(int index)
    {
        if (database == null || index < 0 || index >= database.allPowerups.Count) return false;
        return IsUnlocked(database.allPowerups[index].id);
    }

    public bool MeetsChapterGate(PowerupConfig cfg, int chapter, int chapterLevel)
        => chapter >= cfg.unlockFromChapter && chapterLevel >= cfg.spinUnlockLevel;

    public PowerupDatabase GetDatabase() => database;

    // ─── Progress-driven unlock ───

    public void OnLevelCompleted(int chapter, int chapterLevel)
    {
        lastChapter = chapter;
        lastChapterLevel = chapterLevel;

        EventBus.Publish(new PlayerProgressChangedEvent
        {
            chapter = chapter,
            chapterLevel = chapterLevel
        });

        if (chapterLevel == 0)
            ResetForNewChapter();

        EvaluateForChapter(chapter, chapterLevel);
    }

    public void EvaluateForChapter(int chapter, int chapterLevel)
    {
        if (!IsReady || database == null) return;

        bool anyChanged = false;
        foreach (var cfg in database.allPowerups)
        {
            if (data.IsUnlocked(cfg.id)) continue;
            if (MeetsChapterGate(cfg, chapter, chapterLevel))
            {
                UnlockInternal(cfg.id);
                anyChanged = true;
            }
        }

        if (anyChanged)
            SaveAsync().Forget();
    }

    public void ResetForNewChapter()
    {
        if (!IsReady || database == null) return;

        foreach (var cfg in database.allPowerups)
        {
            if (!cfg.initiallyUnlocked && data.IsUnlocked(cfg.id))
            {
                data.SetUnlocked(cfg.id, false);
                EventBus.Publish(new PowerupLockedEvent { powerupId = cfg.id });
            }
        }

        SaveAsync().Forget();
        Debug.Log("[PowerupUnlockService] New chapter — gated powerups reset.");
    }

    // ─── Manual unlock/lock ───

    public void UnlockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (data.IsUnlocked(id)) return;

        UnlockInternal(id);
        SaveAsync().Forget();
    }

    public void UnlockPowerup(int index)
    {
        if (database == null || index < 0 || index >= database.allPowerups.Count) return;
        UnlockPowerup(database.allPowerups[index].id);
    }

    public void LockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        data.SetUnlocked(id, false);
        EventBus.Publish(new PowerupLockedEvent { powerupId = id });
        SaveAsync().Forget();
    }

    // ─── Internal ───

    private void UnlockInternal(string id)
    {
        data.SetUnlocked(id, true);
        EventBus.Publish(new PowerupUnlockedEvent { powerupId = id });
        Debug.Log($"[PowerupUnlockService] '{id}' unlocked.");
    }

    private async UniTask SaveAsync()
    {
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(CloudKeys.POWERUP_UNLOCK_STATUS, data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PowerupUnlockService] Save failed: {ex.Message}");
        }
    }
}