using UnityEngine;

namespace Gameplay.Birds
{
    public class ZigZagMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        private int _index;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
            _index = 0;
        }

        public void Tick()
        {
            if (_bird.MovePoints == null || _bird.MovePoints.Length == 0)
                return;

            Vector3 target = _bird.MovePoints[_index];

            _bird.transform.position = Vector3.MoveTowards(
                _bird.transform.position,
                target,
                _config.moveSpeed * Time.deltaTime);

            if (Vector3.Distance(_bird.transform.position, target) < 0.1f)
            {
                _index = (_index + 1) % _bird.MovePoints.Length;
            }
        }

        public void Dispose() { }
    }
}