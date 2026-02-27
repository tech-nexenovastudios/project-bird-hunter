using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "BirdHunter/Adaptive Difficulty Config")]
    public class AdaptiveDifficultyConfig : ScriptableObject
    {
        [Header("Performance Window")]
        public float performanceWindowSeconds = 15f;
        public float highPerformanceThreshold = 1.25f;  // 25% ahead
        public float lowPerformanceThreshold  = 0.75f;  // 25% behind

        [Header("High Performance Adjustments")]
        public Vector2 spawnIntervalMultiplierHigh = new Vector2(0.80f, 0.90f);
        public Vector2 pressureMaxMultiplierHigh   = new Vector2(1.10f, 1.15f);
        public float b4WeightShiftHigh = 0.02f;
        public float b3WeightShiftHigh = 0.03f;

        [Header("Low Performance Adjustments")]
        public Vector2 spawnIntervalMultiplierLow = new Vector2(1.15f, 1.35f);
        public Vector2 pressureMaxMultiplierLow   = new Vector2(0.85f, 0.90f);
        public float b1WeightShiftLow = 0.15f;
        public float b2WeightShiftLow = 0.05f;
        public bool  blockB4WhenLow   = true;

        [Header("Relief Mode")]
        public float reliefEnterRatio   = 1.0f;  // CurrentPressure > Max
        public float reliefExitRatio    = 0.75f; // exit when < 75% Max
        public float reliefScoreBonus   = 1.25f; // +25% score
    }

}