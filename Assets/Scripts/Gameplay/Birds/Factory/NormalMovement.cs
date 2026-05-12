using UnityEngine;

namespace Gameplay.Birds
{
    public class NormalMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;
        // +1 = fly right, -1 = fly left. Derived from spawn position so a bird that enters
        // from the right edge flies leftward (toward screen center) instead of off-screen.
        private float _directionSign;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
            float screenMid = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;
            _directionSign = bird.transform.position.x < screenMid ? 1f : -1f;
        }

        public void Tick()
        {
            _bird.transform.position +=
                Vector3.right * (_directionSign * _config.moveSpeed * _bird.SpeedMultiplier * Time.deltaTime);
        }

        public void Dispose() { }
    }
}