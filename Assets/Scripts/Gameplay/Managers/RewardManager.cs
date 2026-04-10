using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Rewards;

namespace Gameplay.Managers
{
    /// <summary>
    /// Distributes rewards (coins, gems, power) based on GameplayRewardConfig tables.
    /// 
    /// Triggers at:
    /// - In-level: egg/bird destruction → coins + probabilistic gems
    /// - Level completion: base coins + performance bonus + first-clear gems + power refund
    /// 
    /// Integrates with GameProgress for persistence and broadcasts via GameEvents.
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        public static RewardManager Instance { get; private set; }

        [SerializeField] private GameplayRewardConfig rewardConfig;

        // Level tracking
        private int _sessionCoinsThisLevel;
        private int _sessionGemsThisLevel;
        private int _sessionPowerRefundsThisLevel;
        private bool _firstClearBonusAwardedThisLevel;

        // Events for UI/feedback
        public event Action<int> OnCoinsAwarded;              // (amount)
        public event Action<int> OnGemsAwarded;               // (amount)
        public event Action<int> OnPowerAwarded;              // (amount)
        public event Action<string, int> OnRewardBroadcast;   // (rewardType, amount)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnEggDestroyed += HandleEggDestroyed;
            GameEvents.OnBirdDestroyed += HandleBirdDestroyed;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnEggDestroyed -= HandleEggDestroyed;
            GameEvents.OnBirdDestroyed -= HandleBirdDestroyed;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        }

        // ────────────────────────────────────────────────────────────────────
        // SETUP / RESET
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reset counters for a new level. Called by GameManager.StartGameplay().
        /// </summary>
        public void ResetForNewLevel()
        {
            _sessionCoinsThisLevel = 0;
            _sessionGemsThisLevel = 0;
            _sessionPowerRefundsThisLevel = 0;
            _firstClearBonusAwardedThisLevel = false;
            Debug.Log("[RewardManager] Reset for new level");
        }

        // ────────────────────────────────────────────────────────────────────
        // IN-LEVEL REWARD DISTRIBUTION (Egg/Bird Destruction)
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when an egg is destroyed (from GameEvents.OnEggDestroyed).
        /// Awards coins + probabilistic gem drop based on chapter phase.
        /// </summary>
        private void HandleEggDestroyed(Interfaces.IDamageable egg, int unused, Vector3 position)
        {
            if (rewardConfig == null)
            {
                Debug.LogWarning("[RewardManager] No rewardConfig assigned!");
                return;
            }

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;

            // ── Coins from egg destroy ──
            int coins = rewardConfig.GetRandomCoinDrop(chapter);
            AwardCoins(coins, "egg_destroyed");

            // ── Probabilistic gem drop ──
            if (rewardConfig.ShouldDropGem(chapter))
            {
                int gems = rewardConfig.GetRandomGemDrop(chapter);
                AwardGems(gems, "egg_gem_drop");
            }
        }

        /// <summary>
        /// Called when a bird is destroyed (from GameEvents.OnBirdDestroyed).
        /// Awards smaller coin bonus.
        /// </summary>
        private void HandleBirdDestroyed(Interfaces.IDamageable bird, int unused, Vector3 position)
        {
            if (rewardConfig == null) return;

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;

            // Bird kill gets ~50% of egg drop
            int coinRange = rewardConfig.GetRandomCoinDrop(chapter);
            int coins = Mathf.RoundToInt(coinRange * 0.5f);

            AwardCoins(coins, "bird_destroyed");
        }

        // ────────────────────────────────────────────────────────────────────
        // LEVEL COMPLETION REWARDS
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when level completes (from GameEvents.OnLevelCompleted).
        /// Awards: base coins + performance bonus + first-clear gems + power refund.
        /// </summary>
        private void HandleLevelCompleted(int finalScore)
        {
            if (rewardConfig == null) return;

            var progressMgr = GameProgressManager.Instance;
            if (progressMgr?.Data == null) return;

            int chapter = progressMgr.CurrentChapter;
            var profile = progressMgr.GetCurrentLevelProfile();

            // ── Calculate performance ratio (score / target) ──
            float performanceRatio = 1f;
            if (profile != null && profile.targetScore > 0)
            {
                performanceRatio = (float)finalScore / profile.targetScore;
            }

            // ── Level completion coins (base + performance bonus) ──
            int completionCoins = rewardConfig.GetLevelCompletionCoins(chapter, performanceRatio);
            AwardCoins(completionCoins, "level_completion");

            // ── First-time clear gems (one-time per level) ──
            if (!_firstClearBonusAwardedThisLevel)
            {
                int firstClearGems = rewardConfig.GetFirstTimeClearGems();
                AwardGems(firstClearGems, "first_time_clear");
                _firstClearBonusAwardedThisLevel = true;
            }

            // ── Power refund (probabilistic) ──
            int powerRefund = rewardConfig.GetPowerRefund();
            if (powerRefund > 0)
            {
                AwardPower(powerRefund, "level_completion_refund");
            }

            // ── Summary ──
            Debug.Log($"[RewardManager] Level {chapter} Complete | " +
                      $"Coins: {completionCoins} | " +
                      $"Performance: {performanceRatio:P0} | " +
                      $"Power Refund: {powerRefund}");
        }

        // ────────────────────────────────────────────────────────────────────
        // REWARD AWARD METHODS (Track + Persist + Broadcast)
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Award coins to player: update progress, persist, and broadcast.
        /// </summary>
        private void AwardCoins(int amount, string reason = "")
        {
            if (amount <= 0) return;

            var progress = GameProgressManager.Instance?.Data;
            if (progress == null) return;

            progress.totalCoins += amount;
            _sessionCoinsThisLevel += amount;

            GameProgressManager.Instance.SaveProgress();
            OnCoinsAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("coins", amount);

            Debug.Log($"[RewardManager] +{amount} coins ({reason}) | Total: {progress.totalCoins}");
        }

        /// <summary>
        /// Award gems to player: update progress, persist, and broadcast.
        /// </summary>
        private void AwardGems(int amount, string reason = "")
        {
            if (amount <= 0) return;

            var progress = GameProgressManager.Instance?.Data;
            if (progress == null) return;

            progress.totalGems += amount;
            _sessionGemsThisLevel += amount;

            GameProgressManager.Instance.SaveProgress();
            OnGemsAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("gems", amount);

            Debug.Log($"[RewardManager] +{amount} gems ({reason}) | Total: {progress.totalGems}");
        }

        /// <summary>
        /// Award power (energy) to player: update progress, persist, and broadcast.
        /// </summary>
        private void AwardPower(int amount, string reason = "")
        {
            if (amount <= 0) return;

            var progress = GameProgressManager.Instance?.Data;
            if (progress == null) return;

            progress.totalPower += amount;
            _sessionPowerRefundsThisLevel += amount;

            GameProgressManager.Instance.SaveProgress();
            OnPowerAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("power", amount);

            Debug.Log($"[RewardManager] +{amount} power ({reason}) | Total: {progress.totalPower}");
        }

        // ────────────────────────────────────────────────────────────────────
        // PUBLIC QUERY METHODS (For UI / Debug)
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Get total coins earned this level (session, not persisted).
        /// </summary>
        public int GetSessionCoinsThisLevel() => _sessionCoinsThisLevel;

        /// <summary>
        /// Get total gems earned this level (session, not persisted).
        /// </summary>
        public int GetSessionGemsThisLevel() => _sessionGemsThisLevel;

        /// <summary>
        /// Get expected coin range for current chapter.
        /// </summary>
        public Vector2Int GetExpectedCoinDropRange()
        {
            if (rewardConfig == null) return Vector2Int.zero;
            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;
            return rewardConfig.GetCoinDropRange(chapter);
        }

        /// <summary>
        /// Estimate total coins for a level (expected drops + completion bonus).
        /// </summary>
        public int EstimateExpectedCoinsForLevel(float performanceRatio = 1f)
        {
            if (rewardConfig == null) return 0;

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;
            var dropRange = rewardConfig.GetCoinDropRange(chapter);
            int avgDrops = (dropRange.x + dropRange.y) / 2;

            // Rough estimate: 10-20 egg/bird kills per level → multiple of drops
            int estimatedDrops = avgDrops * 15;

            // Add completion bonus
            int completionBonus = rewardConfig.GetLevelCompletionCoins(chapter, performanceRatio);

            return estimatedDrops + completionBonus;
        }

        /// <summary>
        /// Manual reward for testing/admin.
        /// </summary>
        public void DebugAwardCoins(int amount)
        {
            AwardCoins(amount, "DEBUG");
        }

        /// <summary>
        /// Manual gem award for testing/admin.
        /// </summary>
        public void DebugAwardGems(int amount)
        {
            AwardGems(amount, "DEBUG");
        }

        /// <summary>
        /// Manual power award for testing/admin.
        /// </summary>
        public void DebugAwardPower(int amount)
        {
            AwardPower(amount, "DEBUG");
        }
    }
}
