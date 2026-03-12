using System;
using UnityEngine;
using TMPro;
using Gameplay.Events;
using Gameplay.Interfaces;

namespace Gameplay.Managers
{
    /// <summary>
    /// Awards score on: egg hit, egg destroy, bird destroy.
    /// LevelScore resets each level. CurrentScore is cumulative session score.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private TMP_Text scoreText;

        
        //ToDo: Replace this with EggConfigData Score
        [Header("Score Values")]
        [SerializeField] private int scorePerEggHit     = 5;
        [SerializeField] private int scorePerEggDestroy = 20;
        [SerializeField] private int scorePerBirdDestroy = 50;

        private int _currentScore;  // cumulative this session
        private int _levelScore;    // resets each level

        public int CurrentScore => _currentScore;
        public int LevelScore   => _levelScore;

        public event Action<int, int> OnScoreChanged; // (totalScore, delta)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit       += OnEggHit;
            GameEvents.OnEggDestroyed += OnEggDestroyed;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit       -= OnEggHit;
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
            if (amount <= 0) return;
            _currentScore += amount;
            _levelScore   += amount;

            GameEvents.FireLevelScoreUpdated(_currentScore, amount);
            OnScoreChanged?.Invoke(_currentScore, amount);
            UpdateDisplay();
        }

        private void OnEggHit(IDamageable egg, int damage, Vector3 hitPoint)
        {
            AddScore(scorePerEggHit);
        }

        private void OnEggDestroyed(IDamageable egg, int unused, Vector3 position)
        {
            AddScore(scorePerEggDestroy);
        }

        private void OnBirdDestroyed(IDamageable bird, int unused, Vector3 position)
        {
            AddScore(scorePerBirdDestroy);
        }

        private void UpdateDisplay()
        {
            if (scoreText != null)
                scoreText.text = "Score: " + _levelScore.ToString("0");
        }
    }
}
