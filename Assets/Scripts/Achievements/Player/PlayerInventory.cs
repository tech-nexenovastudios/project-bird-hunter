using DefaultNamespace;

namespace BirdHunter.Achievement.Player
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public sealed class PlayerInventory
    {
        private readonly Dictionary<string, int> _items = new();

        public int GetQuantity(string itemId)
        {
            return _items.TryGetValue(itemId, out var qty) ? qty : 0;
        }

        public void SetQuantity(string itemId, int quantity)
        {
            _items[itemId] = quantity;
        }

        public IReadOnlyDictionary<string, int> Items => _items;
    }

    public sealed class InventoryServiceImpl : IInventoryService
    {
        public UniTask AddItemAsync(PlayerContext player, string itemId, int quantity)
        {
            var current = player.Inventory.GetQuantity(itemId);
            player.Inventory.SetQuantity(itemId, current + quantity);
            return UniTask.CompletedTask;
        }
    }
}