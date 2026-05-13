using UnityEngine;

namespace Gameplay.Birds
{
    public class NormalMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;
        private float _directionSign;
        private float _baseY;
        private float _bobblePhase;
        private const float BobbleAmplitude = 0.15f;
        private const float BobbleFrequency = 1.2f;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
            float screenMid = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;
            _directionSign = bird.transform.position.x < screenMid ? 1f : -1f;
            _baseY = bird.transform.position.y;
            _bobblePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void Tick()
        {
            Vector3 pos = _bird.transform.position;
            pos.x += _directionSign * _config.moveSpeed * _bird.SpeedMultiplier * Time.deltaTime;
            _bobblePhase += Time.deltaTime * BobbleFrequency * Mathf.PI * 2f;
            pos.y = _baseY + Mathf.Sin(_bobblePhase) * BobbleAmplitude;
            _bird.transform.position = pos;
        }

        public void Dispose() { }
    }
}