using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;

/// <summary>
/// Stateless unlock gate for powerups. A powerup is unlocked iff the player's
/// current chapter and chapter-level meet the thresholds on its config, OR the
/// config is flagged <see cref="PowerupConfig.initiallyUnlocked"/>.
///
/// No cloud state, no events — the truth is derivable from config + current
/// chapter progress. Loads <see cref="PowerupDatabase"/> from Resources on
/// first access for iteration/auto-resolve consumers.
/// </summary>
public static class PowerupGate
{
    private const string DATABASE_RESOURCE_PATH = "PowerupDatabase";

    private static PowerupDatabase _database;

    public static PowerupDatabase Database
    {
        get
        {
            if (_database == null)
            {
                _database = Resources.Load<PowerupDatabase>(DATABASE_RESOURCE_PATH);
                if (_database == null)
                    Debug.LogError($"[PowerupGate] '{DATABASE_RESOURCE_PATH}' not found under Resources/.");
            }
            return _database;
        }
    }

    public static bool IsUnlocked(PowerupConfig cfg)
    {
        if (cfg == null) return false;
        if (cfg.initiallyUnlocked) return true;

        var pm = GameProgressManager.Instance;
        int chapter      = pm != null ? pm.CurrentChapter : 1;
        int chapterLevel = pm != null ? pm.CurrentLevel   : 1;

        return IsUnlocked(cfg, chapter, chapterLevel);
    }

    public static bool IsUnlocked(PowerupConfig cfg, int chapter, int chapterLevel)
    {
        if (cfg == null) return false;
        if (cfg.initiallyUnlocked) return true;
        return chapter >= cfg.unlockFromChapter && chapterLevel >= cfg.spinUnlockLevel;
    }

    public static bool IsUnlocked(string powerupId)
    {
        var db = Database;
        if (db == null || string.IsNullOrEmpty(powerupId)) return false;
        foreach (var cfg in db.allPowerups)
            if (cfg.id == powerupId) return IsUnlocked(cfg);
        return false;
    }

    public static PowerupConfig FindByNameOrId(string lookup)
    {
        var db = Database;
        if (db == null || string.IsNullOrEmpty(lookup)) return null;

        foreach (var cfg in db.allPowerups)
            if (cfg.displayName == lookup || cfg.id == lookup) return cfg;

        string stripped = lookup.Replace("(Clone)", "").Trim();
        foreach (var cfg in db.allPowerups)
            if (cfg.displayName == stripped || cfg.id == stripped) return cfg;

        return null;
    }
}
