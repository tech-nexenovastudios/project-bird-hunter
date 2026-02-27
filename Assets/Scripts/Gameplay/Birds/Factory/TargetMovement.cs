using UnityEngine;

namespace Gameplay.Birds
{
    public class TargetMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;
        private Transform _target;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;

            if (CannonSpawner.cannon != null)
                _target = CannonSpawner.cannon.transform;
        }

        public void Tick()
        {
            if (_target == null) return;

            _bird.transform.position = Vector3.MoveTowards(
                _bird.transform.position,
                _target.position,
                _config.moveSpeed * Time.deltaTime);

            if (Vector3.Distance(_bird.transform.position, _target.position) < 0.1f)
            {
                _bird.ForceKill();
            }
        }

        public void Dispose() { }
    }
}