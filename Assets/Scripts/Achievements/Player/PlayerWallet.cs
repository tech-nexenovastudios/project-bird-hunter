using DefaultNamespace;

namespace BirdHunter.Achievement.Player
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public sealed class PlayerWallet
    {
        private readonly Dictionary<string, int> _currencies = new();

        public int Get(string currencyId)
        {
            return _currencies.TryGetValue(currencyId, out var amount) ? amount : 0;
        }

        public void Set(string currencyId, int amount)
        {
            _currencies[currencyId] = amount;
        }
    }

    public sealed class WalletServiceImpl : IWalletService
    {
        public UniTask AddAsync(PlayerContext player, string currencyId, int amount)
        {
            var current = player.Wallet.Get(currencyId);
            player.Wallet.Set(currencyId, current + amount);
            return UniTask.CompletedTask;
        }
    }
}