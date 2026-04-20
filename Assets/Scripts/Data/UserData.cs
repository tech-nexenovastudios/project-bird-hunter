using System;

[Serializable]
public class UserData
{
    public string userId;
    public string displayName;
    public string username;          // ← NEW: editable, shown in UI
    public int avatarIndex;          // ← NEW: index into avatar sprite list
    public int level;
    public long lastLoginTimestamp;
    public long accountCreatedTimestamp;

    public UserData()
    {
        userId = string.Empty;
        displayName = "Player";
        username = string.Empty;
        avatarIndex = 0;
        level = 1;
        lastLoginTimestamp = 0;
        accountCreatedTimestamp = 0;
    }
}