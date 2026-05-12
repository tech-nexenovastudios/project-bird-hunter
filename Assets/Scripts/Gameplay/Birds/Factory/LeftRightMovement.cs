using UnityEngine;

namespace Gameplay.Birds
{
    public class LeftRightMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        private float _minX;
        private float _maxX;
        private float _direction;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;

            var collider = bird.GetComponent<Collider2D>();
            float halfWidth = collider != null ? collider.bounds.extents.x : 0.5f;

            _minX = ScreenBounds.minX + halfWidth;
            _maxX = ScreenBounds.maxX - halfWidth;

            Vector3 position = bird.transform.position;
            float screenMidX = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;

            _direction = position.x < screenMidX ? 1f : -1f;

            position.x = _direction > 0f ? _minX : _maxX;
            bird.transform.position = position;

            _bird.FlipDirection(_direction);
        }

        public void Tick()
        {
            float speed = _config.moveSpeed * _bird.SpeedMultiplier;
            Vector3 position = _bird.transform.position;

            if (_bird.HasLaid)
            {
                position.x += _direction * speed * Time.deltaTime;
                _bird.transform.position = position;
                return;
            }

            position.x += _direction * speed * Time.deltaTime;

            if (position.x >= _maxX)
            {
                position.x = _maxX;
                _direction = -1f;
                _bird.FlipDirection(_direction);
            }
            else if (position.x <= _minX)
            {
                position.x = _minX;
                _direction = 1f;
                _bird.FlipDirection(_direction);
            }

            _bird.transform.position = position;
        }

        public void Dispose()
        {
        }
    }
}