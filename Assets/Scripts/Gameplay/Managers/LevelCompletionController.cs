using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.Levels;

namespace Gameplay.Managers
{
    /// <summary>
    /// Monitors two win conditions independently:
    ///   1. LevelScore >= targetScore  → fires OnTargetScoreReached + may complete level
    ///   2. All active eggs cleared    → fires OnAllEggsCleared + may complete level
    /// Level is completed when EITHER condition is met first.
    /// </summary>
    public class LevelCompletionController : MonoBehaviour
    {
        public static LevelCompletionController Instance { get; private set; }

        [Header("Fallback")]
        [SerializeField] private int fallbackTargetScore = 1000;

        private bool _levelCompleteTriggered;
        private bool _targetScoreReached;
        private bool _allEggsCleared;

        public event Action<int> OnLevelCompleteConditionMet;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnLevelScoreUpdated += OnScoreUpdated;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelScoreUpdated -= OnScoreUpdated;
        }

        public void ResetForNewLevel()
        {
            _levelCompleteTriggered = false;
            _targetScoreReached     = false;
            _allEggsCleared         = false;
        }

        private void OnScoreUpdated(int currentScore, int delta)
        {
            if (GameManager.Instance?.state != GameState.Gameplay) return;
            if (_targetScoreReached) return;

            int levelScore  = ScoreManager.Instance != null ? ScoreManager.Instance.LevelScore : 0;
            int targetScore = GetTargetScore();

            if (levelScore >= targetScore)
            {
                _targetScoreReached = true;
                Debug.Log($"[LevelCompletion] Target score {targetScore} reached!");
                TryCompleteLevel(levelScore);
            }
        }


        private void OnAllEggsCleared()
        {
            if (GameManager.Instance?.state != GameState.Gameplay) return;
            if (_allEggsCleared) return;

            _allEggsCleared = true;
            Debug.Log("[LevelCompletion] All eggs cleared!");
            // XPManager listens to OnAllEggsCleared directly — no need to relay here
            TryCompleteLevel(ScoreManager.Instance != null ? ScoreManager.Instance.LevelScore : 0);
        }

        private void TryCompleteLevel(int finalScore)
        {
            if (_levelCompleteTriggered) return;
            _levelCompleteTriggered = true;

            OnLevelCompleteConditionMet?.Invoke(finalScore);
            GameEvents.FireLevelCompleted(finalScore);
            GameManager.Instance.CompleteCurrentLevel(finalScore);
        }

        private int GetTargetScore()
        {
            var profile = SpawnController.Instance?.levelProfile
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
