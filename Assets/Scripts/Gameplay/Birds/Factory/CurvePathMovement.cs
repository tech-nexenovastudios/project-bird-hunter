using DG.Tweening;

namespace Gameplay.Birds
{
    public class CurvePathMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;

            if (_bird.MovePoints == null || _bird.MovePoints.Length == 0)
                return;

            _bird.transform.DOPath(
                    _bird.MovePoints,
                    2f * _bird.MovePoints.Length,
                    PathType.CatmullRom)
                .SetEase(Ease.Linear)
                .SetLoops(-1);
        }

        public void Tick() { }

        public void Dispose()
        {
            DOTween.Kill(_bird.transform);
        }
    }
}