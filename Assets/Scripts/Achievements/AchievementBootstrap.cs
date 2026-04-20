using BirdHunter.Achievement.Catalog;
using BirdHunter.Achievement.GameplayEvents;
using BirdHunter.Achievement.Notifier;
using BirdHunter.Achievement.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BirdHunter.Achievement
{
    public sealed class AchievementBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerContextProvider _playerProvider;

        private AchievementService _service;

        private async void Awake()
        {
            var player = _playerProvider.Player;

            var timerFactory = new AchievementTimerFactory();
            var walletService = new WalletServiceImpl();
            var inventoryService = new InventoryServiceImpl();
            var unlocksService = new UnlocksServiceImpl();

            var unityClient = new UnityAchievementClient();
            var syncService = new UnityAchievementSyncService(unityClient);
            var repository = new JsonAchievementRepository();
            var notifier = FindObjectOfType<PopupAchievementNotifier>();

            var achievements = AchievementCatalog.CreateAll(
                timerFactory,
                walletService,
                inventoryService,
                unlocksService);

            _service = new AchievementService(
                achievements,
                repository,
                notifier,
                syncService);

            await _service.InitializeAsync(player);
        }

        public UniTask ProcessEventAsync(GameplayEvent evt)
        {
            return _service.ProcessEventAsync(evt, _playerProvider.Player);
        }
    }
}