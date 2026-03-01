using System;
using UnityEngine;
using TMPro;
using Gameplay.Events;
using Gameplay.Interfaces;

namespace Gameplay.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private TMP_Text scoreText;

        private int _currentScore;
        private int _levelScore;

        public int CurrentScore => _currentScore;
        public int LevelScore => _levelScore;

        public event Action<int, int> OnScoreChanged;

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
            GameEvents.OnEggDestroyed += OnEggDestroyed;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnEggDestroyed -= OnEggDestroyed;
            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
        }

        public void ResetLevel()
        {
            _levelScore = 0;
            UpdateDisplay();
        }

        public void AddScore(int amount)
        {
            _currentScore += amount;
            _levelScore += amount;

            GameEvents.FireLevelScoreUpdated(_currentScore, amount);
            OnScoreChanged?.Invoke(_currentScore, amount);
            UpdateDisplay();
        }

        private void OnEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position)
        {
            AddScore(scoreAwarded);
        }

        private void OnBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position)
        {
            AddScore(scoreAwarded);
        }

        private void UpdateDisplay()
        {
            if (scoreText != null)
                scoreText.text = "Score: " + _currentScore.ToString("0");
        }
    }
}
