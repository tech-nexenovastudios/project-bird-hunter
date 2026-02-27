using UnityEngine;

namespace Gameplay.Birds
{
    public class DiagonalMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        private Vector2 _target;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;

            _bird.transform.position =
                new Vector2(ScreenBounds.minX - 2, ScreenBounds.maxY + 2);

            _target =
                new Vector2(ScreenBounds.maxX + 2, ScreenBounds.minY - 2);
        }

        public void Tick()
        {
            _bird.transform.position = Vector2.MoveTowards(
                _bird.transform.position,
                _target,
                _config.moveSpeed * Time.deltaTime);

            if (Vector2.Distance(_bird.transform.position, _target) < 0.1f)
            {
                _bird.ForceKill();
            }
        }

        public void Dispose() { }
    }
}