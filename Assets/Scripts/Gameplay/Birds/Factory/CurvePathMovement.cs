using DG.Tweening;
using UnityEngine;

namespace Gameplay.Birds
{
    // Smooth Catmull-Rom curve from the spawn edge to the opposite exit edge. Waypoints are
    // generated procedurally so the curve works from any spawn position, not just the fixed
    // points baked into the prefab. After lay, the tween is killed and a straight exit takes
    // over — keeps the post-lay chase window short and predictable.
    public class CurvePathMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;
        private Tween _pathTween;
        private bool _exitingAfterLay;

        private const int WaypointCount = 5;
        private const float CurveAmplitude = 1.5f;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
            _exitingAfterLay = false;

            var path = BuildCurve(bird.transform.position);
            if (path.Length == 0) return;

            // Duration scales with lifetime so the bird arrives at the exit edge around the
            // same time it would have died from lifetime — keeps the visual timing consistent
            // with chapter difficulty curves.
            float duration = Mathf.Max(2f, config.lifetime * 0.85f);
            _pathTween = bird.transform
                .DOPath(path, duration, PathType.CatmullRom)
                .SetEase(Ease.InOutSine)
                .SetSpeedBased(false);
        }

        private static Vector3[] BuildCurve(Vector3 spawn)
        {
            float screenMid = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;
            bool fromLeft = spawn.x < screenMid;
            float exitX = fromLeft ? ScreenBounds.maxX + 3.5f : ScreenBounds.minX - 3.5f;

            var pts = new Vector3[WaypointCount];
            for (int i = 0; i < WaypointCount; i++)
            {
                float t = (i + 1) / (float)WaypointCount;
                float x = Mathf.Lerp(spawn.x, exitX, t);
                // Sine-shaped vertical wave so the bird arcs smoothly instead of zigzagging.
                float y = spawn.y + Mathf.Sin(t * Mathf.PI * 2f) * CurveAmplitude;
                pts[i] = new Vector3(x, y, spawn.z);
            }
            return pts;
        }

        public void Tick()
        {
            // When the bird lays, transition the existing path tween to its end and switch to
            // a straight flee. Apply SpeedMultiplier in the flee branch since DOTween paths
            // run on their own clock and can't trivially re-time mid-flight.
            if (_bird.HasLaid && !_exitingAfterLay)
            {
                _exitingAfterLay = true;
                _pathTween?.Kill();
                _pathTween = null;
            }

            if (_exitingAfterLay)
            {
                float screenMid = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;
                float dirSign = _bird.transform.position.x < screenMid ? -1f : 1f;
                float speed = _config.moveSpeed * _bird.SpeedMultiplier;
                _bird.transform.position += Vector3.right * (dirSign * speed * Time.deltaTime);
            }
        }

        public void Dispose()
        {
            _pathTween?.Kill();
            _pathTween = null;
            if (_bird != null) DOTween.Kill(_bird.transform);
        }
    }
}
