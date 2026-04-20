using System.Collections.Generic;

namespace BirdHunter.Achievement
{
    using Cysharp.Threading.Tasks;

    public interface IAchievementSyncService
    {
        UniTask SyncProgressAsync(IAchievement achievement);
        UniTask SyncUnlockedAsync(IAchievement achievement);
    }

    public sealed class UnityAchievementSyncService : IAchievementSyncService
    {
        private readonly IUnityAchievementClient _unityClient;

        public UnityAchievementSyncService(IUnityAchievementClient unityClient)
        {
            _unityClient = unityClient;
        }

        public async UniTask SyncProgressAsync(IAchievement achievement)
        {
            var link = achievement.UnityLink;
            if (link == null || !link.IsProgressive)
                return;

            float progress = achievement.Condition.GetProgress01();
            await _unityClient.SetProgressAsync(link.UnityId, progress);
        }

        public async UniTask SyncUnlockedAsync(IAchievement achievement)
        {
            var link = achievement.UnityLink;
            if (link == null)
                return;

            await _unityClient.UnlockAsync(link.UnityId);
        }
    }
    public sealed class NullAchievementSyncService : IAchievementSyncService
    {
        public UniTask SyncProgressAsync(IAchievement achievement)
        {
            return UniTask.CompletedTask;
        }

        public UniTask SyncUnlockedAsync(IAchievement achievement)
        {
            return UniTask.CompletedTask;
        }
    }
    
    public interface IAchievementRepository
    {
        UniTask<Dictionary<string, bool>> LoadStatesAsync(PlayerContext player);
        UniTask SaveStatesAsync(PlayerContext player, IReadOnlyDictionary<string, bool> states);
    }

    public interface IAchievementNotifier
    {
        UniTask ShowUnlockedAsync(IAchievement achievement);
    }
}