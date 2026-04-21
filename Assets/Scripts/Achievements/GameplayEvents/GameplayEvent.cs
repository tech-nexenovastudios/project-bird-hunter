namespace BirdHunter.Achievement.GameplayEvents
{
    public abstract class GameplayEvent { }

    public sealed class EnemyKilledEvent : GameplayEvent
    {
        public string EnemyType { get; }

        public EnemyKilledEvent(string enemyType)
        {
            EnemyType = enemyType;
        }
    }

    public sealed class LevelReachedEvent : GameplayEvent
    {
        public int NewLevel { get; }

        public LevelReachedEvent(int newLevel)
        {
            NewLevel = newLevel;
        }
    }
}