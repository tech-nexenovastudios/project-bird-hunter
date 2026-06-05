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
        [SerializeField] private TextMeshProUGUI powerTMP;

        //[Header("Multiplier Labels (x2 icons)")]
        //[SerializeField] private GameObject coinsX2Label;
        //[SerializeField] private GameObject gemsX2Label;
        //[SerializeField] private GameObject xpX2Label;

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
        private int _basePower;
        private int _baseXP;
        private bool _adRewardClaimed;
        private bool _watchAdInProgress;
        private bool _adRewardGrantedThisAttempt;

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
            //GameEvents.OnPlayerDeath += HandlePlayerDeath;
            _adRewardClaimed = false;
            _watchAdInProgress = false;
            _adRewardGrantedThisAttempt = false;
            SnapshotRunData();
            RefreshUI();
        }

        private void OnDisable()
        {
            UnsubscribeAdEvents();
        }

        //private void OnDisable()
        //{
        //    GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        //}

        // ─────────────────────────────────────────────
        //  Death Handler
        // ─────────────────────────────────────────────

        //private void HandlePlayerDeath()
        //{
        //    _adRewardClaimed = false;
        //    SnapshotRunData();
        //    RefreshUI();
        //}

        // ─────────────────────────────────────────────
        //  Data Snapshot
        // ─────────────────────────────────────────────

        private void SnapshotRunData()
        {
            // Re-cache in case of scene reload
            if (_scoreManager == null)
                _scoreManager = FindObjectOfType<ScoreManager>();

            _score = _scoreManager != null ? _scoreManager.ChapterScore : 0;

            // Target score / chapter / level — chapter-progression authority is SpawnController.
            var spawn = SpawnController.Instance;
            _targetScore = spawn != null ? spawn.TargetScore : 0;

            var progress = GameProgressManager.Instance;
            _chapter = progress != null ? progress.CurrentChapter : 1;
            _level = progress != null ? progress.CurrentLevel : 1;

            // Read the previous best BEFORE submitting this run, otherwise SubmitRunScore
            // overwrites cp.highScore with _score on a record run and both fields show the same value.
            var cp = progress != null ? progress.GetChapterProgress(_chapter) : null;
            _highScore = cp != null ? cp.highScore : 0;

            progress?.SubmitRunScore(_score);
            progress?.RegisterChapterDeath();

            // Run rewards — RewardManager accumulates across every level the player cleared
            // this run, so the Game Over panel reflects the full run, not just the level
            // they died on.
            var rewards = RewardManager.Instance;
            _baseCoins = rewards != null ? rewards.GetRunCoins() : 0;
            _baseGems = rewards != null ? rewards.GetRunGems() : 0;
            _basePower = rewards != null ? rewards.GetRunPower() : 0;

            int playerLevel = progress?.Data?.playerLevel ?? 1;
     
        }

        // ─────────────────────────────────────────────
        //  UI Refresh
        // ─────────────────────────────────────────────

        private void RefreshUI()
        {
            int multiplier = _adRewardClaimed ? 2 : 1;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (scoreTMP) scoreTMP.text = _score.ToString("N0", inv);
            if (targetScoreTMP) targetScoreTMP.text = _targetScore > 0 ? _targetScore.ToString("N0", inv) : "-";
            if (chapterLevelTMP) chapterLevelTMP.text = $"Chapter {_chapter}  •  Level {_level}";
            if (highScoreTMP) highScoreTMP.text = _highScore.ToString("N0", inv);

            if (coinsTMP) coinsTMP.text = (multiplier * _baseCoins).ToString();
            if (gemsTMP) gemsTMP.text = (multiplier * _baseGems).ToString();
            if (powerTMP) powerTMP.text = (multiplier * _basePower).ToString();

            //bool showBadges = !_adRewardClaimed;
            //if (coinsX2Label) coinsX2Label.SetActive(showBadges);
            //if (gemsX2Label) gemsX2Label.SetActive(showBadges);
            //if (xpX2Label) xpX2Label.SetActive(showBadges);

            if (watchAdButton) watchAdButton.gameObject.SetActive(!_adRewardClaimed);
        }

        // ─────────────────────────────────────────────
        //  Watch Ad
        // ─────────────────────────────────────────────

        public void OnClickWatchAd()
        {
            if (_adRewardClaimed || _watchAdInProgress) return;

            var ads = AdManager.Instance;
            if (ads == null || !ads.IsRewardedReady)
            {
                Debug.LogWarning("[GameOverScreen] Rewarded ad not ready.");
                return;
            }

            // Disable immediately so the player can't double-tap while the ad is loading/showing.
            _watchAdInProgress = true;
            _adRewardGrantedThisAttempt = false;
            if (watchAdButton) watchAdButton.interactable = false;

            ads.OnRewardGranted += HandleAdRewardGranted;
            ads.OnRewardedDismissed += HandleAdRewardedDismissed;
            ads.OnRewardedUnavailable += HandleAdRewardedUnavailable;

            bool shown = ads.ShowRewarded("GameOver_x2Reward");
            if (!shown)
            {
                UnsubscribeAdEvents();
                _watchAdInProgress = false;
                if (watchAdButton) watchAdButton.interactable = true;
            }
        }

        private void HandleAdRewardGranted(string rewardName, int amount)
        {
            // Granted fires before Dismissed. Stash and finalize in Dismissed so the
            // ad has fully closed before we tween reward UI.
            _adRewardGrantedThisAttempt = true;
        }

        private void HandleAdRewardedDismissed()
        {
            bool granted = _adRewardGrantedThisAttempt;
            UnsubscribeAdEvents();
            _watchAdInProgress = false;

            if (granted)
            {
                OnAdComplete(success: true);
            }
            else
            {
                // User closed without earning the reward — allow a retry.
                if (watchAdButton && !_adRewardClaimed)
                    watchAdButton.interactable = true;
            }
        }

        private void HandleAdRewardedUnavailable()
        {
            // Display failed or ad expired — let the player try again.
            UnsubscribeAdEvents();
            _watchAdInProgress = false;
            if (watchAdButton && !_adRewardClaimed)
                watchAdButton.interactable = true;
        }

        private void UnsubscribeAdEvents()
        {
            var ads = AdManager.Instance;
            if (ads == null) return;
            ads.OnRewardGranted -= HandleAdRewardGranted;
            ads.OnRewardedDismissed -= HandleAdRewardedDismissed;
            ads.OnRewardedUnavailable -= HandleAdRewardedUnavailable;
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
            if (_basePower > 0) CurrencyManager.Instance?.AddPower(_basePower).Forget();

            RefreshUI();
        }
    }
}
