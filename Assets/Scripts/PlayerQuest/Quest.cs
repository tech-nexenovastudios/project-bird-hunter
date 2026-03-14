using System;
using UnityEngine;

namespace PlayerQuest
{
    using System;

    [System.Serializable]
    public class QuestTemplate
    {
        public string id, title;
        public string type;  // "DestroyEggs" etc. → parsed to QuestType
        public int target;
        public QuestReward rewards;
        public int cannonIndex;  // -1 for non-specific
    }

    [System.Serializable]
    public class QuestReward
    {
        public int coins, gems, power;
    }

    [System.Serializable]
    public class Quest
    {
        public string id, title;
        public QuestType type;
        public int target;
        public QuestReward rewards;
        public int cannonIndex;
    
        // Progress tracking
        public float progress;
        public bool isComplete, isClaimed;
        public long startTime, expiryTime;
    
        // Serialization helpers
        [NonSerialized] public string typeString;  // For JSON
    
        public Quest(QuestTemplate template)
        {
            id = template.id;
            title = template.title;
            type = QuestTypeParser.Parse(template.type);
            typeString = template.type;  // For saving
            target = template.target;
            rewards = template.rewards;
            cannonIndex = template.cannonIndex;
            startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            expiryTime = startTime + 86400;  // 24h
            progress = 0f;
            isComplete = false;
            isClaimed = false;
        }
    }

    public static class QuestTypeParser
    {
        public static QuestType Parse(string typeString)
        {
            return typeString switch
            {
                "DestroyEggs" => QuestType.DestroyEggs,
                "ReachCombo" => QuestType.ReachCombo,
                "CannonWins" => QuestType.CannonWins,
                "SpendCoins" => QuestType.SpendCoins,
                "ClearLevels" => QuestType.ClearLevels,
                "PowerSpent" => QuestType.PowerSpent,
                "GemsEarned" => QuestType.GemsEarned,
                "AccuracyPercent" => QuestType.AccuracyPercent,
                _ => QuestType.DestroyEggs
            };
        }
    }

    public enum QuestType
    {
        DestroyEggs,
        ReachCombo,
        CannonWins,
        SpendCoins,
        ClearLevels,
        PowerSpent,
        GemsEarned,
        AccuracyPercent
    }
}