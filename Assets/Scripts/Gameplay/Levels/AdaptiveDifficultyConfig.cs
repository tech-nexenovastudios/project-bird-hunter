using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "BirdHunter/Adaptive Difficulty Config")]
    public class AdaptiveDifficultyConfig : ScriptableObject
    {
        [Header("Performance Window")]
        [Tooltip("how long to consider performance")]
        public float performanceWindowSeconds = 5f;
        [Tooltip("how much difficulty can be increase")]
        public float highPerformanceThreshold = 1.25f;  // 25% ahead
        [Tooltip("how much difficulty can be decrease")]
        public float lowPerformanceThreshold  = 0.75f;  // 25% behind

        [Header("Performance Curve")]
        public AnimationCurve performanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float performanceCurveShift = 0.05f;
        
        [Header("Performance Adjustments")]
        public float baseSpawnInterval = 1f;
        public float basePressureMax   = 1f;
        public float baseWeightShift   = 0.05f;
        
        [Header("Relief Mode")]
        [Tooltip("How much to shift difficulty when entering relief mode")]
        public float reliefWeightShift = 0.05f;
        [Tooltip("How relief mode affects score [(0-1) 0 = no change, 1 = 100% more]")]
        [Range(0f, 1f)] public float reliefScoreShift  = 0.25f;
        [Tooltip("How relief mode affects difficulty [(0-5) 1 = 100% more]")]
        [Range(0f, 5f)] public float reliefMultiplier  = 1.25f;
        [Tooltip("How relief mode affects difficulty [(0-1) 0 = no change, 1 = 100% more]")]
        [Range(0f, 1f)] public float reliefThreshold   = 0.75f;
        
        [Header("High Performance Adjustments")]
        public Vector2 spawnIntervalMultiplierHigh = new (0.1f, 10f);
        public Vector2 pressureMaxMultiplierHigh   = new (1f, 25f);
        public float b4WeightShiftHigh = 0.02f;
        public float b3WeightShiftHigh = 0.03f;

        [Header("Low Performance Adjustments")]
        public Vector2 spawnIntervalMultiplierLow = new Vector2(1.15f, 1.35f);
        public Vector2 pressureMaxMultiplierLow   = new Vector2(0.85f, 0.90f);
        public float b1WeightShiftLow = 0.05f;
        public float b2WeightShiftLow = 0.15f;
        public bool  blockB4WhenLow   = true;

        [Header("Relief Mode")]
        public float reliefEnterRatio   = 1.0f;  // CurrentPressure > Max
        public float reliefExitRatio    = 0.75f; // exit when < 75% Max
        public float reliefScoreBonus   = 1.25f; // +25% score
    }

}