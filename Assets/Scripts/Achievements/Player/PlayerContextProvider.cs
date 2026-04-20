using BirdHunter.Achievement.Catalog;
using BirdHunter.Achievement.GameplayEvents;
using BirdHunter.Achievement.Notifier;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BirdHunter.Achievement.Player
{
    public sealed class PlayerContextProvider : MonoBehaviour
    {
        [SerializeField] private PlayerWallet _wallet;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerUnlocks _unlocks;

        private PlayerContext _player;

        public PlayerContext Player => _player;

        private void Awake()
        {
            if (_wallet == null)
                _wallet = new PlayerWallet();
            
            if (_inventory == null)
                _inventory =   new PlayerInventory();
            
            if (_unlocks == null)
                _unlocks =   new PlayerUnlocks();
            
            _player = new PlayerContext(_wallet, _inventory, _unlocks);
        }
    }
}