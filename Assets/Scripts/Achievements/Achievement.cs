using System;
using System.Collections.Generic;
using System.Linq;
using BirdHunter.Achievement.GameplayEvents;
using Cysharp.Threading.Tasks;
using UnityEngine.SocialPlatforms;

namespace BirdHunter.Achievement
{
    public sealed class UnityAchievementLink
    {
        public string UnityId { get; }
        public bool IsProgressive { get; }

        public UnityAchievementLink(string unityId, bool isProgressive)
        {
            UnityId = unityId;
            IsProgressive = isProgressive;
        }
    }
    public interface IReward
    {
        UniTask GiveAsync(PlayerContext player);
        string Describe();
    }

    public interface IAchievementCondition
    {
        void OnEvent(GameplayEvent evt);
        bool IsMet { get; }
        float GetProgress01();
    }

    public interface IAchievement
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        bool IsSecret { get; }

        IAchievementCondition Condition { get; }
        IReadOnlyList<IReward> Rewards { get; }

        bool IsUnlocked { get; }
        void MarkUnlocked();

        UnityAchievementLink UnityLink { get; }
    }
    public sealed class Achievement : IAchievement
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public bool IsSecret { get; }
        public IAchievementCondition Condition { get; }
        public IReadOnlyList<IReward> Rewards { get; }
        public bool IsUnlocked { get; private set; }
        public UnityAchievementLink UnityLink { get; }

        public Achievement(
            string id,
            string displayName,
            string description,
            IAchievementCondition condition,
            IEnumerable<IReward> rewards,
            UnityAchievementLink unityLink = null,
            bool isSecret = false,
            bool isUnlocked = false)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Condition = condition;
            Rewards = rewards.ToList().AsReadOnly();
            UnityLink = unityLink;
            IsSecret = isSecret;
            IsUnlocked = isUnlocked;
        }

        public void MarkUnlocked()
        {
            IsUnlocked = true;
        }
    }
    
}