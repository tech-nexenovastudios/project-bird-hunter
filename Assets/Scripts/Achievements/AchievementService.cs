using BirdHunter.Achievement.GameplayEvents;

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
namespace BirdHunter.Achievement
{
    public sealed class AchievementService
    {
        private readonly List<IAchievement> _achievements = new();
        private readonly IAchievementRepository _repository;
        private readonly IAchievementNotifier _notifier;
        private readonly IAchievementSyncService _syncService;

        private readonly Dictionary<string, bool> _states = new();

        public AchievementService(
            IEnumerable<IAchievement> achievements,
            IAchievementRepository repository,
            IAchievementNotifier notifier,
            IAchievementSyncService syncService)
        {
            _achievements.AddRange(achievements);
            _repository = repository;
            _notifier = notifier;
            _syncService = syncService;
        }

        public async UniTask InitializeAsync(PlayerContext player)
        {
            var loaded = await _repository.LoadStatesAsync(player);

            foreach (var ach in _achievements)
            {
                if (loaded.TryGetValue(ach.Id, out var unlocked) && unlocked)
                    ach.MarkUnlocked();

                _states[ach.Id] = ach.IsUnlocked;
            }
        }

        public async UniTask ProcessEventAsync(GameplayEvent evt, PlayerContext player)
        {
            foreach (var achievement in _achievements)
            {
                if (achievement.IsUnlocked)
                    continue;

                achievement.Condition.OnEvent(evt);

                await _syncService.SyncProgressAsync(achievement);

                if (achievement.Condition.IsMet)
                    await UnlockAsync(achievement, player);
            }
        }

        private async UniTask UnlockAsync(IAchievement achievement, PlayerContext player)
        {
            if (achievement.IsUnlocked)
                return;

            achievement.MarkUnlocked();
            _states[achievement.Id] = true;

            foreach (var reward in achievement.Rewards)
                await reward.GiveAsync(player);

            await _notifier.ShowUnlockedAsync(achievement);

            await _syncService.SyncUnlockedAsync(achievement);

            await _repository.SaveStatesAsync(player, _states);
        }

        public IReadOnlyList<IAchievement> All => _achievements;
    }
}