public struct PowerupUnlockedEvent
{
    public string powerupId;
}

public struct PowerupLockedEvent
{
    public string powerupId;
}

public struct PlayerProgressChangedEvent
{
    public int chapter;
    public int chapterLevel;
}

public struct PowerupStatesLoadedEvent { }