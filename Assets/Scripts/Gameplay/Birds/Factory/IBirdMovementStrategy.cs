namespace Gameplay.Birds
{
    public interface IBirdMovementStrategy
    {
        void Initialize(BaseBird bird, BirdConfig config);
        void Tick();
        void Dispose();
    }
}