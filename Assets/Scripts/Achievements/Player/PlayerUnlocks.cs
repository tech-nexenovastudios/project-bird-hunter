using DefaultNamespace;

namespace BirdHunter.Achievement.Player
{
    using System.Collections.Generic;

    public sealed class PlayerUnlocks
    {
        private readonly HashSet<string> _flags = new();

        public bool HasFlag(string flagId)
        {
            return _flags.Contains(flagId);
        }

        public void SetFlag(string flagId, bool value)
        {
            if (value)
                _flags.Add(flagId);
            else
                _flags.Remove(flagId);
        }

        public IReadOnlyCollection<string> Flags => _flags;
    }

    public sealed class UnlocksServiceImpl : IUnlocksService
    {
        public void SetFlag(PlayerContext player, string flagId, bool value)
        {
            player.Unlocks.SetFlag(flagId, value);
        }
    }
}