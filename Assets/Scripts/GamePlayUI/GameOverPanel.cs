using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.Events;
using Gameplay.Managers;

namespace Gameplay.UI
{
    public class GameOverScreenHandler : MonoBehaviour
    {
        [Header("Score & Level")]
        [SerializeField] private TextMeshProUGUI scoreTMP;

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
            // ── FIX: Read from ScoreManager, NOT GameProgressManager ──
            //
            // GameProgressManager.Data.totalScore only updates inside CompleteLevel(),
            // which is never called on death — so it always reads the OLD value.
            //
            // ScoreManager.LevelScore is the live score the player sees on the HUD.
            // It updates every hit/destroy event and is exactly what we want here.
            //
            // Re-cache in case of scene reload
            if (_scoreManager == null)
                _scoreManager = FindObjectOfType<ScoreManager>();

            _score = _scoreManager != null ? _scoreManager.LevelScore : 0;
            _baseCoins = 0;   
            _baseGems = 0;   

            int playerLevel = GameProgressManager.Instance?.Data?.playerLevel ?? 1;
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
            XPManager.Instance?.AddXP(_baseXP);

            // EconomyManager.Instance?.AddCoins(_baseCoins);
            // EconomyManager.Instance?.AddGems(_baseGems);

            RefreshUI();
        }
    }
}
