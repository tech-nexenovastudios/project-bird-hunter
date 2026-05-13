using UnityEngine;

namespace Gameplay.Birds
{
    public class DiagonalMovement : IBirdMovementStrategy
    {
        private const float EXIT_OVERSHOOT = 3.5f;

        private BaseBird _bird;
        private BirdConfig _config;
        private Vector2 _target;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;

            Vector2 spawnPos = bird.transform.position;

            float screenMidX = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;

            float exitX = spawnPos.x < screenMidX
                ? ScreenBounds.maxX + EXIT_OVERSHOOT
                : ScreenBounds.minX - EXIT_OVERSHOOT;

            float screenHeight = ScreenBounds.maxY - ScreenBounds.minY;
            float upperLimitY = ScreenBounds.minY + screenHeight / 3f;

            float exitY = Mathf.Max(spawnPos.y - screenHeight * 0.25f, upperLimitY);

            _target = new Vector2(exitX, exitY);
        }

        public void Tick()
        {
            float speed = _config.moveSpeed * _bird.SpeedMultiplier;

            _bird.transform.position = Vector2.MoveTowards(
                _bird.transform.position,
                _target,
                speed * Time.deltaTime
            );
        }

        public void Dispose()
        {
        }
    }
}