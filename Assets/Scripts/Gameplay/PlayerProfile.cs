using BirdHunter.Inventory.Data;
using BirdHunter.Inventory.Services;
using Gameplay.Managers;

namespace Gameplay
{
    public static class PlayerProfile
    {
        public static GameProgress Data => GameProgressManager.Instance != null ? GameProgressManager.Instance.Data : null;
        public static int CurrentChapter => GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentChapter : 1;
        public static int CurrentLevel   => GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentLevel : 1;
        public static int OverallHighScore => GameProgressManager.Instance != null ? GameProgressManager.Instance.HighScore : 0;
        public static int PlayerLevel => Data != null ? Data.playerLevel : 1;
        public static int PlayerXP    => Data != null ? Data.playerXP : 0;

        public static ChapterProgress Chapter(int chapter)
            => GameProgressManager.Instance != null ? GameProgressManager.Instance.GetChapterProgress(chapter) : null;
        public static ChapterProgress CurrentChapterProgress => Chapter(CurrentChapter);
        public static int ChapterHighScore(int chapter) => Chapter(chapter)?.highScore ?? 0;

        public static string EquippedCannon => CannonInventoryService.Instance != null ? CannonInventoryService.Instance.EquippedKey : null;
        public static int  CannonLevel(string key)      => CannonInventoryService.Instance != null ? CannonInventoryService.Instance.GetLevel(key) : 0;
        public static bool IsCannonUnlocked(string key) => CannonInventoryService.Instance != null && CannonInventoryService.Instance.IsUnlocked(key);
        public static CannonBaseStatsDto CannonStats(string key) => CannonStatsRepository.Instance?.GetByKey(key);

        public static class Wallet
        {
            public static long Coins => CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold  : 0;
            public static long Gems  => CurrencyManager.Instance != null ? CurrencyManager.Instance.Gems  : 0;
            public static long Power => CurrencyManager.Instance != null ? CurrencyManager.Instance.Power : 0;
        }
    }
}
