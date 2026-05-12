using UnityEngine;

namespace Gameplay.Birds
{
    // Procedural zigzag: at spawn, computes a series of waypoints from the bird's entry edge
    // to the opposite edge, oscillating vertically by zigzagAmplitude. Waypoints are local to
    // the spawn position so the strategy works regardless of where the bird enters from.
    // After lay, abandons the zigzag and flies straight toward the exit edge.
    public class ZigZagMovement : IBirdMovementStrategy
    {
        private BaseBird _bird;
        private BirdConfig _config;

        private Vector3[] _waypoints;
        private int _index;
        private const int WaypointCount = 6;
        private const float ZigZagAmplitude = 1.2f;

        public void Initialize(BaseBird bird, BirdConfig config)
        {
            _bird = bird;
            _config = config;
            _index = 0;
            _waypoints = BuildWaypoints(bird.transform.position);
        }

        private static Vector3[] BuildWaypoints(Vector3 spawn)
        {
            float screenMid = (ScreenBounds.minX + ScreenBounds.maxX) * 0.5f;
            bool fromLeft = spawn.x < screenMid;

            // Cross the full width, ending just past the opposite edge so the bird exits cleanly.
            float exitX = fromLeft ? ScreenBounds.maxX + 1f : ScreenBounds.minX - 1f;
            float totalDx = exitX - spawn.x;

            var pts = new Vector3[WaypointCount];
            for (int i = 0; i < WaypointCount; i++)
            {
                float t = (i + 1) / (float)WaypointCount;
                float x = spawn.x + totalDx * t;
                float yOffset = ((i % 2 == 0) ? 1f : -1f) * ZigZagAmplitude;
                pts[i] = new Vector3(x, spawn.y + yOffset, spawn.z);
            }
            return pts;
        }

        public void Tick()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;

            float speed = _config.moveSpeed * _bird.SpeedMultiplier;
            Vector3 target = _waypoints[_index];

            _bird.transform.position = Vector3.MoveTowards(
                _bird.transform.position,
                target,
                speed * Time.deltaTime);

            if (Vector3.Distance(_bird.transform.position, target) < 0.1f)
            {
                // After lay, skip remaining waypoints and head straight to the final exit point
                // so the chase window is tight and birds don't linger doing pretty patterns.
                if (_bird.HasLaid)
                {
                    _index = _waypoints.Length - 1;
                }
                else
                {
                    _index = Mathf.Min(_index + 1, _waypoints.Length - 1);
                }
            }
        }

        public void Dispose() { }
    }
}
