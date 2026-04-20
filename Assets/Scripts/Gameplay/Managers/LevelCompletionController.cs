using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.Levels;

namespace Gameplay.Managers
{
    /// <summary>
    /// Level completes when BOTH conditions are met in order:
    /// 1. LevelScore >= targetScore → triggers SpawnEngine.StartDrain()
    /// 2. All active eggs cleared   → fires level completed
    /// </summary>
    public class LevelCompletionController : MonoBehaviour
    {
        public static LevelCompletionController Instance { get; private set; }

        [Header("Fallback")]
        [SerializeField] private int fallbackTargetScore = 1000;

        private bool _levelCompleteTriggered;
        private bool _targetScoreReached;

        public event Action<int> OnLevelCompleteConditionMet;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelScoreUpdated += OnScoreUpdated;
            GameEvents.OnAllEggsCleared    += OnAllEggsCleared;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelScoreUpdated -= OnScoreUpdated;
            GameEvents.OnAllEggsCleared    -= OnAllEggsCleared;
        }

        public void ResetForNewLevel()
        {
            _levelCompleteTriggered = false;
            _targetScoreReached     = false;
        }

        private void OnScoreUpdated(int currentScore, int delta)
        {
            if (GameManager.Instance?.state != GameState.Gameplay) return;
            if (_targetScoreReached) return;
            if (ScoreManager.Instance == null) return;

            int levelScore  = ScoreManager.Instance.LevelScore;
            int targetScore = GetTargetScore();

            if (levelScore >= targetScore)
            {
                _targetScoreReached = true;
                Debug.Log($"[LevelCompletion] Target score {targetScore} reached — starting drain.");
                GameManager.Instance?.spawnEngine?.StartDrain();
            }
        }

        private void OnAllEggsCleared()
        {
            if (GameManager.Instance?.state != GameState.Gameplay) return;
            if (!_targetScoreReached) return;

            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.LevelScore : 0;
            Debug.Log("[LevelCompletion] All eggs cleared — completing level.");
            TryCompleteLevel(finalScore);
        }

        private void TryCompleteLevel(int finalScore)
        {
            if (_levelCompleteTriggered) return;
            _levelCompleteTriggered = true;

            OnLevelCompleteConditionMet?.Invoke(finalScore);
            GameManager.Instance.CompleteCurrentLevel(finalScore);
        }

        private int GetTargetScore()
        {
            var profile = GameManager.Instance?.spawnEngine?.levelProfile
                          ?? GameProgressManager.Instance?.GetCurrentLevelProfile();
            return profile != null ? Mathf.Max(1, profile.targetScore) : fallbackTargetScore;
        }

        public float GetScoreProgress()
        {
            int levelScore  = ScoreManager.Instance != null ? ScoreManager.Instance.LevelScore : 0;
            int targetScore = GetTargetScore();
            return targetScore > 0 ? Mathf.Clamp01((float)levelScore / targetScore) : 0f;
        }

        public int GetTargetScoreValue() => GetTargetScore();
    }
}
