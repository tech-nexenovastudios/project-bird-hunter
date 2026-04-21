using System;
using System.Collections.Generic;

[Serializable]
public class PowerupUnlockStatusData
{
    public Dictionary<string, bool> unlocks;

    public PowerupUnlockStatusData()
    {
        unlocks = new Dictionary<string, bool>();
    }

    public bool IsUnlocked(string powerupId)
    {
        return unlocks.TryGetValue(powerupId, out var unlocked) && unlocked;
    }

    public void SetUnlocked(string powerupId, bool unlocked)
    {
        unlocks[powerupId] = unlocked;
    }
}