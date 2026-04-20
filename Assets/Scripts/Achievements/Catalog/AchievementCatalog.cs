using BirdHunter.Achievement.Conditions;
using BirdHunter.Achievement.GameplayEvents;
using DefaultNamespace;
using System.Collections.Generic;


namespace BirdHunter.Achievement.Catalog
{

    public static class AchievementCatalog
    {
        public static IEnumerable<IAchievement> CreateAll(
            IAchievementTimerFactory timerFactory,
            IWalletService walletService,
            IInventoryService inventoryService,
            IUnlocksService unlocksService)
        {
            yield return new Achievement(
                "kill_10_any",
                "First Bloodbath",
                "Defeat 10 enemies.",
                new CounterCondition(
                    e => e is EnemyKilledEvent,
                    10),
                new IReward[]
                {
                    new CurrencyReward("gold", 100, walletService),
                    new UnlockFlagReward("badge_first_bloodbath", unlocksService)
                },
                new UnityAchievementLink("kill_10_any_ugc", true));

            yield return new Achievement(
                "reach_level_10",
                "Growing Strong",
                "Reach level 10.",
                new LevelReachedCondition(10),
                new IReward[]
                {
                    new ItemReward("epic_sword", 1, inventoryService)
                },
                new UnityAchievementLink("reach_level_10_ugc", true));
        }
    }
}