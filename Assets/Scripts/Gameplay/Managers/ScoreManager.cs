//using System;
//using UnityEngine;
//using TMPro;
//using Gameplay.Events;
//using Gameplay.Interfaces;

//namespace Gameplay.Managers
//{
//    /// <summary>
//    /// Awards score on: egg hit, egg destroy, bird destroy.
//    /// LevelScore resets each level. CurrentScore is cumulative session score.
//    /// </summary>
//    public class ScoreManager : MonoBehaviour
//    {
//        public static ScoreManager Instance { get; private set; }

//        [SerializeField] private TMP_Text scoreText;


//        //ToDo: Replace this with EggConfigData Score
//        [Header("Score Values")]
//        [SerializeField] private int scorePerEggHit     = 5;
//        [SerializeField] private int scorePerEggDestroy = 20;
//        [SerializeField] private int scorePerBirdDestroy = 50;

//        private int _currentScore;  // cumulative this session
//        private int _levelScore;    // resets each level

//        public int CurrentScore => _currentScore;
//        public int LevelScore   => _levelScore;

//        public event Action<int, int> OnScoreChanged; // (totalScore, delta)

//        private void Awake()
//        {
//            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//            Instance = this;
//        }

//        private void OnEnable()
//        {
//            GameEvents.OnEggHit       += OnEggHit;
//            GameEvents.OnEggDestroyed += OnEggDestroyed;
//            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
//        }

//        private void OnDisable()
//        {
//            GameEvents.OnEggHit       -= OnEggHit;
//            GameEvents.OnEggDestroyed -= OnEggDestroyed;
//            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
//        }

//        public void ResetLevel()
//        {
//            _levelScore = 0;
//            UpdateDisplay();
//        }

//        public void AddScore(int amount)
//        {
//            if (amount <= 0) return;
//            _currentScore += amount;
//            _levelScore   += amount;

//            GameEvents.FireLevelScoreUpdated(_currentScore, amount);
//            OnScoreChanged?.Invoke(_currentScore, amount);
//            UpdateDisplay();
//        }

//        private void OnEggHit(IDamageable egg, int damage, Vector3 hitPoint)
//        {
//            AddScore(scorePerEggHit);
//        }

//        private void OnEggDestroyed(IDamageable egg, int unused, Vector3 position)
//        {
//            AddScore(scorePerEggDestroy);
//        }

//        private void OnBirdDestroyed(IDamageable bird, int unused, Vector3 position)
//        {
//            AddScore(scorePerBirdDestroy);
//        }

//        private void UpdateDisplay()
//        {
//            if (scoreText != null)
//                scoreText.text = "Score  :" +_levelScore.ToString("0");
//        }
//    }
//}


using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Health;
using Gameplay.Interfaces;

namespace Gameplay.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Fallback Score Values (used when config/payload missing)")]
        [SerializeField] private int scorePerEggHit = 5;
        [SerializeField] private int scorePerEggDestroy = 20;
        [SerializeField] private int scorePerBirdDestroy = 50;

        private int _currentScore;
        private int _levelScore;
        private int _chapterScore;
        private int _targetScore;
        private bool _acceptingScore = true;
        private int _lastChapter = -1;

        public int CurrentScore => _currentScore;
        public int LevelScore => _levelScore;
        public int ChapterScore => _chapterScore;
        public int TargetScore => _targetScore;

        public event Action<int, int> OnScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit += OnEggHit;
            GameEvents.OnEggDestroyed += OnEggDestroyed;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
            GameEvents.OnLevelCompletedEarly += OnLevelCompletedEarly;
            GameEvents.OnGameLevelUpdated += OnLevelStarted;
            GameEvents.OnAllEggsCleared += OnLevelEnded;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
        }

        private void OnLevelCompletedEarly(float remainingTime)
        {
            Debug.Log($"[ScoreManager] Level completed early. Remaining time: {remainingTime}");
            //TODO: Implement early completion Rewards
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnEggHit;
            GameEvents.OnEggDestroyed -= OnEggDestroyed;
            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
            GameEvents.OnLevelCompletedEarly -= OnLevelCompletedEarly;
            GameEvents.OnGameLevelUpdated -= OnLevelStarted;
            GameEvents.OnAllEggsCleared -= OnLevelEnded;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
        }

        // Level finished: stop accepting score, but keep the level UI showing the final value
        // through the completion popup / slot screen. Reset happens later in ResetLevel, called
        // by GameManager.StartGameplay — which is deferred until the popup countdown ends
        // (non-spin levels) or the player picks a slot powerup (spin levels).
        private void OnLevelCompleted(int finalScore)
        {
            _acceptingScore = false;
        }

        private void OnLevelStarted(int levelIndex)
        {
            _levelScore = 0;
            _acceptingScore = true;

            int chapter = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentChapter : _lastChapter;
            if (chapter != _lastChapter)
            {
                _chapterScore = 0;
                _lastChapter = chapter;
            }

            GameEvents.FireLevelScoreUpdated(0, 0);
        }

        private void OnLevelEnded() => _acceptingScore = false;

        public void ResetLevel(int targetScore = 0)
        {
            _levelScore = 0;
            _targetScore = targetScore;
            _acceptingScore = true;

            // Broadcast so HUD bar resets to 0 immediately
            GameEvents.FireLevelScoreUpdated(0, 0);
        }

        public void AddScore(int amount)
        {
            if (amount <= 0 || !_acceptingScore) return;
            _currentScore += amount;
            _chapterScore += amount;
            _levelScore += amount;

            GameEvents.FireLevelScoreUpdated(_levelScore, amount);
            OnScoreChanged?.Invoke(_currentScore, amount);
        }

        private void OnEggHit(IDamageable egg, int damage, Vector3 hitPoint)
        {
            int perDamage = (egg is EggHealth eh && eh.Config != null && eh.Config.scorePerHit > 0)
                ? eh.Config.scorePerHit
                : scorePerEggHit;
            AddScore(perDamage * Mathf.Max(1, damage));
        }

        private void OnEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position)
            => AddScore(scoreAwarded > 0 ? scoreAwarded : scorePerEggDestroy);

        private void OnBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position)
            => AddScore(scoreAwarded > 0 ? scoreAwarded : scorePerBirdDestroy);
    }
}