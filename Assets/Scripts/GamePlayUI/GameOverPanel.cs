using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gameplay;
using Gameplay.Events;
using Gameplay.Managers;

namespace Gameplay.UI
{
    public class GameOverScreenHandler : MonoBehaviour
    {
        [Header("Score & Level")]
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI targetScoreTMP;
        [SerializeField] private TextMeshProUGUI chapterLevelTMP;
        [SerializeField] private TextMeshProUGUI highScoreTMP;

        [Header("Rewards")]
        [SerializeField] private TextMeshProUGUI coinsTMP;
        [SerializeField] private TextMeshProUGUI gemsTMP;
        [SerializeField] private TextMeshProUGUI xpTMP;

        [Header("Multiplier Labels (x2 icons)")]
        [SerializeField] private GameObject coinsX2Label;
        [SerializeField] private GameObject gemsX2Label;
        [SerializeField] private GameObject xpX2Label;

        [Header("Buttons")]
        [SerializeField] private Button watchAdButton;

        // ─────────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────────

        private int _score;
        private int _targetScore;
        private int _chapter;
        private int _level;
        private int _highScore;
        private int _baseCoins;
        private int _baseGems;
        private int _baseXP;
        private bool _adRewardClaimed;

        // ScoreManager has no namespace — reference by class name directly.
        // Cached here to avoid repeated FindObjectOfType calls.
        private ScoreManager _scoreManager;

        // ─────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
         
            _scoreManager = FindObjectOfType<ScoreManager>();

            if (_scoreManager == null)
                Debug.LogError("[GameOverScreen] ScoreManager not found! Score will show 0.");
        }

        private void Start()
        {
            watchAdButton?.onClick.AddListener(OnClickWatchAd);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        }

        // ─────────────────────────────────────────────
        //  Death Handler
        // ─────────────────────────────────────────────

        private void HandlePlayerDeath()
        {
            _adRewardClaimed = false;
            SnapshotRunData();
            RefreshUI();
        }

        // ─────────────────────────────────────────────
        //  Data Snapshot
        // ─────────────────────────────────────────────

        private void SnapshotRunData()
        {
            // Re-cache in case of scene reload
            if (_scoreManager == null)
                _scoreManager = FindObjectOfType<ScoreManager>();

            _score = _scoreManager != null ? _scoreManager.LevelScore : 0;

            // Target score / chapter / level — chapter-progression authority is SpawnController.
            var spawn = SpawnController.Instance;
            _targetScore = spawn != null ? spawn.TargetScore : 0;

            var progress = GameProgressManager.Instance;
            _chapter = progress != null ? progress.CurrentChapter : 1;
            _level = progress != null ? progress.CurrentLevel : 1;
            _highScore = progress != null ? progress.HighScore : 0;

            // Session rewards — pulled from RewardManager, which tracks per-level accrual.
            var rewards = RewardManager.Instance;
            _baseCoins = rewards != null ? rewards.GetSessionCoinsThisLevel() : 0;
            _baseGems = rewards != null ? rewards.GetSessionGemsThisLevel() : 0;

            int playerLevel = progress?.Data?.playerLevel ?? 1;
            _baseXP = XPManager.Instance != null
                ? Mathf.RoundToInt(XPManager.Instance.GetXPToNextLevel(playerLevel) * 0.1f)
                : 0;
        }

        // ─────────────────────────────────────────────
        //  UI Refresh
        // ─────────────────────────────────────────────

        private void RefreshUI()
        {
            int multiplier = _adRewardClaimed ? 2 : 1;

            if (scoreTMP) scoreTMP.text = _score.ToString("000,000");
            if (targetScoreTMP) targetScoreTMP.text = _targetScore > 0 ? _targetScore.ToString("000,000") : "-";
            if (chapterLevelTMP) chapterLevelTMP.text = $"Chapter {_chapter}  •  Level {_level}";
            if (highScoreTMP) highScoreTMP.text = _highScore.ToString("000,000");

            if (coinsTMP) coinsTMP.text = (multiplier * _baseCoins).ToString("000");
            if (gemsTMP) gemsTMP.text = (multiplier * _baseGems).ToString("000");
            if (xpTMP) xpTMP.text = (multiplier * _baseXP).ToString("000");

            bool showBadges = !_adRewardClaimed;
            if (coinsX2Label) coinsX2Label.SetActive(showBadges);
            if (gemsX2Label) gemsX2Label.SetActive(showBadges);
            if (xpX2Label) xpX2Label.SetActive(showBadges);

            if (watchAdButton) watchAdButton.gameObject.SetActive(!_adRewardClaimed);
        }

        // ─────────────────────────────────────────────
        //  Watch Ad
        // ─────────────────────────────────────────────

        public void OnClickWatchAd()
        {
            if (_adRewardClaimed) return;

            // ── Swap stub for real AdManager call when ready: ──
            // AdManager.Instance.ShowRewarded("GameOver_x2Reward");
            // Then handle reward in HandleAdRewardGranted() subscribed to
            // AdManager.Instance.OnRewardGranted

            OnAdComplete(success: true); // stub for testing
        }

        private void OnAdComplete(bool success)
        {
            if (!success) return;

            _adRewardClaimed = true;

            // x2 multiplier: grant the *additional* copy of each reward on top of what
            // RewardManager already awarded during the level. Base amount was credited
            // live on egg/bird destroy; the ad reward is the second tier.
            XPManager.Instance?.AddXP(_baseXP);
            if (_baseCoins > 0) CurrencyManager.Instance?.AddGold(_baseCoins).Forget();
            if (_baseGems > 0) CurrencyManager.Instance?.AddGems(_baseGems).Forget();

            RefreshUI();
        }
    }
}
