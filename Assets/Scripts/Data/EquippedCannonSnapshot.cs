using System;

[Serializable]
public class EquippedCannonSnapshot
{
    public string cannonKey;
    public int level;
    public long savedAtTimestamp;

    public EquippedCannonSnapshot()
    {
        cannonKey = string.Empty;
        level = 1;
        savedAtTimestamp = 0;
    }
}