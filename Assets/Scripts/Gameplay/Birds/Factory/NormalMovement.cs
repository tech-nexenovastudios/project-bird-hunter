using UnityEngine;

namespace Gameplay.Birds
{
    public class NormalMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
        }

        public void Tick()
        {
            _bird.transform.position +=
                Vector3.right * _config.moveSpeed * Time.deltaTime;
        }

        public void Dispose() { }
    }
}