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

        // ── DPS sampling ──
        private const float DpsWindow = 1f;
        private int _damageThisWindow;   // damage dealt in the current sampling window
        private float _windowTimer;
        private int _levelDamageTotal;   // total damage this level (for avg dps)
        private float _levelElapsed;     // seconds of active combat this level
        private float _peakDps;
        private int _currentLevelIndex;

        // ── Cannon defense (for star rating) ──
        private int _cannonCurrentHp;    // latest cannon HP (tracked continuously, not reset per level)
        private int _cannonMaxHp;
        private int _hitsTakenThisLevel; // cannon hits taken this level
        private int _damageTakenThisLevel;

        public int CurrentScore => _currentScore;
        public int LevelScore => _levelScore;
        public int ChapterScore => _chapterScore;
        public int TargetScore => _targetScore;
        public int LastChapterScore { get; private set; }

        public event Action<int, int> OnScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit += OnEggHit;
            GameEvents.OnBirdHit += OnBirdHit;
            GameEvents.OnEggDestroyed += OnEggDestroyed;
            GameEvents.OnBirdDestroyed += OnBirdDestroyed;
            GameEvents.OnLevelCompletedEarly += OnLevelCompletedEarly;
            GameEvents.OnGameLevelUpdated += OnLevelStarted;
            GameEvents.OnAllEggsCleared += OnLevelEnded;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnCannonHealthChanged += OnCannonHealthChanged;
            GameEvents.OnCannonHit += OnCannonHit;
        }

        private void OnLevelCompletedEarly(float remainingTime)
        {
            Debug.Log($"[ScoreManager] Level completed early. Remaining time: {remainingTime}");
            //TODO: Implement early completion Rewards
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnEggHit;
            GameEvents.OnBirdHit -= OnBirdHit;
            GameEvents.OnEggDestroyed -= OnEggDestroyed;
            GameEvents.OnBirdDestroyed -= OnBirdDestroyed;
            GameEvents.OnLevelCompletedEarly -= OnLevelCompletedEarly;
            GameEvents.OnGameLevelUpdated -= OnLevelStarted;
            GameEvents.OnAllEggsCleared -= OnLevelEnded;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnCannonHealthChanged -= OnCannonHealthChanged;
            GameEvents.OnCannonHit -= OnCannonHit;
        }

        private void OnCannonHealthChanged(int currentHp, int maxHp)
        {
            _cannonCurrentHp = currentHp;
            _cannonMaxHp = maxHp;
        }

        private void OnCannonHit(int damage)
        {
            if (!_acceptingScore) return;
            _hitsTakenThisLevel++;
            _damageTakenThisLevel += Mathf.Max(0, damage);
        }

        // Level finished: stop accepting score, but keep the level UI showing the final value
        // through the completion popup. Reset happens in ResetLevel — at the popup countdown's
        // end for non-spin levels, and at completion (before the slot opens) for spin levels.
        private void OnLevelCompleted(int finalScore)
        {
            _acceptingScore = false;
            FlushDpsWindow();
            float avgDps = _levelElapsed > 0f ? _levelDamageTotal / _levelElapsed : 0f;
            int endHpPct = _cannonMaxHp > 0
                ? Mathf.RoundToInt(100f * _cannonCurrentHp / _cannonMaxHp)
                : -1; // -1 = health never reported (no cannon health event seen)
            CombatLog.Summary(_currentLevelIndex, finalScore, _levelDamageTotal, _peakDps, avgDps,
                _levelElapsed, endHpPct, _hitsTakenThisLevel, _damageTakenThisLevel);
        }

        private void OnLevelStarted(int levelIndex)
        {
            ResetAllScores();
            ResetCombatStats();
            _currentLevelIndex = levelIndex;
            _acceptingScore = true;
            GameEvents.FireLevelScoreUpdated(0, 0);
        }

        private void OnLevelEnded() => _acceptingScore = false;

        public void ResetLevel(int targetScore = 0)
        {
            ResetAllScores();
            _targetScore = targetScore;
            _acceptingScore = true;

            // Broadcast so HUD bar resets to 0 immediately
            GameEvents.FireLevelScoreUpdated(0, 0);
        }

        private void ResetAllScores()
        {
            _currentScore = 0;
            _levelScore = 0;
        }

        public void ResetChapterScore()
        {
            LastChapterScore = _chapterScore;
            _chapterScore = 0;
        }

        public void AddScore(int amount)
        {
            if (amount <= 0 || !_acceptingScore) return;
            _currentScore += amount;
            _chapterScore += amount;
            _levelScore += amount;

            GameEvents.FireLevelScoreUpdated(_levelScore, amount);
            OnScoreChanged?.Invoke(_currentScore, amount);
            CombatLog.Score(_levelScore, amount, "score_gain");
        }

        private void OnEggHit(IDamageable egg, int damage, Vector3 hitPoint)
        {
            AccumulateDamage(damage);
            int perDamage = (egg is EggHealth eh && eh.Config != null && eh.Config.scorePerHit > 0)
                ? eh.Config.scorePerHit
                : scorePerEggHit;
            AddScore(perDamage * Mathf.Max(1, damage));
        }

        private void OnBirdHit(IDamageable bird, int damage, Vector3 hitPoint)
            => AccumulateDamage(damage);

        private void OnEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position)
            => AddScore(scoreAwarded > 0 ? scoreAwarded : scorePerEggDestroy);

        private void OnBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position)
            => AddScore(scoreAwarded > 0 ? scoreAwarded : scorePerBirdDestroy);

        private void Update()
        {
            if (!_acceptingScore) return;

            _levelElapsed += Time.deltaTime;
            _windowTimer += Time.deltaTime;
            if (_windowTimer >= DpsWindow)
                FlushDpsWindow();
        }

        private void AccumulateDamage(int damage)
        {
            if (!_acceptingScore || damage <= 0) return;
            _damageThisWindow += damage;
            _levelDamageTotal += damage;
        }

        // Emit a DPS sample for the elapsed window and start a fresh one. Only logs when damage landed.
        private void FlushDpsWindow()
        {
            if (_windowTimer > 0f && _damageThisWindow > 0)
            {
                float dps = _damageThisWindow / _windowTimer;
                if (dps > _peakDps) _peakDps = dps;
                if (GameLogger.IsEnabled(LogCategory.Combat))
                    CombatLog.Dps(dps, _damageThisWindow, _windowTimer, _peakDps);
            }
            _damageThisWindow = 0;
            _windowTimer = 0f;
        }

        private void ResetCombatStats()
        {
            _damageThisWindow = 0;
            _windowTimer = 0f;
            _levelDamageTotal = 0;
            _levelElapsed = 0f;
            _peakDps = 0f;
            _hitsTakenThisLevel = 0;
            _damageTakenThisLevel = 0;
            // _cannonCurrentHp / _cannonMaxHp are NOT reset: cannon HP carries across levels in a run.
        }
    }
}