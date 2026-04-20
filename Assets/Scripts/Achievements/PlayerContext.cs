using BirdHunter.Achievement.Player;

namespace BirdHunter.Achievement
{
    public sealed class PlayerContext
    {
        public PlayerWallet Wallet { get; }
        public PlayerInventory Inventory { get; }
        public PlayerUnlocks Unlocks { get; }
        
        public PlayerContext(PlayerWallet wallet, PlayerInventory inventory, PlayerUnlocks unlocks)
        {
            Wallet = wallet;
            Inventory = inventory;
            Unlocks = unlocks;
        }
    }
}