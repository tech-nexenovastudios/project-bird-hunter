using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.Levels;

namespace Gameplay.Managers
{
    /// <summary>
    /// Score-based level completion. Triggers when level score reaches LevelProfile.targetScore.
    /// </summary>
    public class LevelCompletionController : MonoBehaviour
    {
        public static LevelCompletionController Instance { get; private set; }

        [Header("Options")]
        [SerializeField] private bool requireTargetScore = true;
        [SerializeField] private int fallbackTargetScore = 1000;

        private bool _levelCompleteTriggered;

        public event Action<int> OnLevelCompleteConditionMet;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
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
        }

        private void OnScoreUpdated(int currentScore, int delta)
        {
            if (GameManager.Instance?.state != GameState.Gameplay) return;
            if (_levelCompleteTriggered) return;
            if (ScoreManager.Instance == null) return;

            int levelScore = ScoreManager.Instance.LevelScore;
            int targetScore = GetTargetScore();

            if (levelScore >= targetScore)
            {
                _levelCompleteTriggered = true;
                OnLevelCompleteConditionMet?.Invoke(levelScore);
                GameEvents.FireLevelCompleted(levelScore);
                GameManager.Instance.CompleteCurrentLevel(levelScore);
            }
        }

        private int GetTargetScore()
        {
            var profile = GetCurrentLevelProfile();
            if (profile != null && requireTargetScore)
                return Mathf.Max(1, profile.targetScore);
            return fallbackTargetScore;
        }

        private LevelProfile GetCurrentLevelProfile()
        {
            if (SpawnController.Instance?.levelProfile != null)
                return SpawnController.Instance.levelProfile;
            return GameProgressManager.Instance?.GetCurrentLevelProfile();
        }

        /// <summary>
        /// Get progress toward level completion (0..1).
        /// </summary>
        public float GetProgress()
        {
            int levelScore = ScoreManager.Instance != null ? ScoreManager.Instance.LevelScore : 0;
            int targetScore = GetTargetScore();
            return targetScore > 0 ? Mathf.Clamp01((float)levelScore / targetScore) : 0f;
        }

        public int GetTargetScoreValue() => GetTargetScore();
    }
}
