using BirdHunter.Achievement;
using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public interface IWalletService
    {
        UniTask AddAsync(PlayerContext player, string currencyId, int amount);
    }

    public interface IUnlocksService
    {
        void SetFlag(PlayerContext player, string flagId, bool value);
    }

    public interface IInventoryService
    {
        UniTask AddItemAsync(PlayerContext player, string itemId, int quantity);
    }

    public sealed class CurrencyReward : IReward
    {
        private readonly string _currencyId;
        private readonly int _amount;
        private readonly IWalletService _walletService;

        public CurrencyReward(string currencyId, int amount, IWalletService walletService)
        {
            _currencyId = currencyId;
            _amount = amount;
            _walletService = walletService;
        }

        public async UniTask GiveAsync(PlayerContext player)
        {
            await _walletService.AddAsync(player, _currencyId, _amount);
        }

        public string Describe()
        {
            return $"+{_amount} {_currencyId}";
        }
    }

    public sealed class ItemReward : IReward
    {
        private readonly string _itemId;
        private readonly int _quantity;
        private readonly IInventoryService _inventoryService;

        public ItemReward(string itemId, int quantity, IInventoryService inventoryService)
        {
            _itemId = itemId;
            _quantity = quantity;
            _inventoryService = inventoryService;
        }

        public async UniTask GiveAsync(PlayerContext player)
        {
            await _inventoryService.AddItemAsync(player, _itemId, _quantity);
        }

        public string Describe()
        {
            return $"{_quantity}x {_itemId}";
        }
    }

    public sealed class UnlockFlagReward : IReward
    {
        private readonly string _flagId;
        private readonly IUnlocksService _unlocksService;

        public UnlockFlagReward(string flagId, IUnlocksService unlocksService)
        {
            _flagId = flagId;
            _unlocksService = unlocksService;
        }

        public UniTask GiveAsync(PlayerContext player)
        {
            _unlocksService.SetFlag(player, _flagId, true);
            return UniTask.CompletedTask;
        }

        public string Describe()
        {
            return $"Unlock: {_flagId}";
        }
    }
}