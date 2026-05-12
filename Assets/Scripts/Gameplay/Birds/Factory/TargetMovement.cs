using UnityEngine;

namespace Gameplay.Birds
{
    // Homes in on the cannon at chaseSpeed, kamikaze-style. Used for aggressive bird types
    // that intentionally trade their life for cannon damage. After the bird lays its egg
    // (HasLaid), homing is abandoned and the bird flees toward the nearest screen edge so the
    // chase reward path stays consistent with the rest of the one-lay model.
    public class TargetMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;
        private Transform _target;
        private float _fleeDirSign;
        private bool _isFleeing;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
            _isFleeing = false;

            if (CannonSpawner.cannon != null)
                _target = CannonSpawner.cannon.transform;
        }

        public void Tick()
        {
            float speed = _config.chaseSpeed * _bird.SpeedMultiplier;

            // Transition to flee mode once the egg is laid — homing into the cannon for a kamikaze
            // hit after already laying would feel like a double-tax on the player.
            if (_bird.HasLaid && !_isFleeing)
            {
                _isFleeing = true;
                float screenMid = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;
                _fleeDirSign = _bird.transform.position.x < screenMid ? -1f : 1f;
            }

            if (_isFleeing)
            {
                _bird.transform.position += Vector3.right * (_fleeDirSign * speed * Time.deltaTime);
                return;
            }

            if (_target == null) return;

            _bird.transform.position = Vector3.MoveTowards(
                _bird.transform.position,
                _target.position,
                speed * Time.deltaTime);

            if (Vector3.Distance(_bird.transform.position, _target.position) < 0.1f)
            {
                _bird.ForceKill();
            }
        }

        public void Dispose() { }
    }
}
