using UnityEngine;

namespace Gameplay.Birds
{
    public class LeftRightMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        private float _minX;
        private float _maxX;
        private bool _movingRight = true;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;

            bird.transform.PlayerBoundCalculate(
                bird.GetComponent<BoxCollider2D>(),
                out Vector2 size);

            _minX = ScreenBounds.minX + size.x;
            _maxX = ScreenBounds.maxX - size.x;
        }

        public void Tick()
        {
            float direction = _movingRight ? 1f : -1f;

            _bird.transform.position +=
                Vector3.right * direction * _config.moveSpeed * Time.deltaTime;

            if (_bird.transform.position.x >= _maxX)
                _movingRight = false;
            else if (_bird.transform.position.x <= _minX)
                _movingRight = true;
        }

        public void Dispose() { }
    }
}