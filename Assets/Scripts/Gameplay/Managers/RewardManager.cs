using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Rewards;

namespace Gameplay.Managers
{
    public class RewardManager : MonoBehaviour
    {
        public static RewardManager Instance { get; private set; }

        [SerializeField] private GameplayRewardConfig rewardConfig;

        private int _sessionCoinsThisLevel;
        private int _sessionGemsThisLevel;
        private int _sessionPowerRefundsThisLevel;
        private bool _firstClearBonusAwardedThisLevel;

        public event Action<int> OnCoinsAwarded;
        public event Action<int> OnGemsAwarded;
        public event Action<int> OnPowerAwarded;
        public event Action<string, int> OnRewardBroadcast;

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

        public void ResetForNewLevel()
        {
            _sessionCoinsThisLevel = 0;
            _sessionGemsThisLevel = 0;
            _sessionPowerRefundsThisLevel = 0;
            _firstClearBonusAwardedThisLevel = false;
            Debug.Log("[RewardManager] Reset for new level");
        }

        private void HandleEggDestroyed(Interfaces.IDamageable egg, int unused, Vector3 position)
        {
            if (rewardConfig == null)
            {
                Debug.LogWarning("[RewardManager] No rewardConfig assigned!");
                return;
            }

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;

            int coins = rewardConfig.GetRandomCoinDrop(chapter);
            AwardCoins(coins, "egg_destroyed");

            if (rewardConfig.ShouldDropGem(chapter))
            {
                int gems = rewardConfig.GetRandomGemDrop(chapter);
                AwardGems(gems, "egg_gem_drop");
            }
        }

        private void HandleBirdDestroyed(Interfaces.IDamageable bird, int unused, Vector3 position)
        {
            if (rewardConfig == null) return;

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;

            int coinRange = rewardConfig.GetRandomCoinDrop(chapter);
            int coins = Mathf.RoundToInt(coinRange * 0.5f);

            AwardCoins(coins, "bird_destroyed");
        }

        private void HandleLevelCompleted(int finalScore)
        {
            if (rewardConfig == null) return;

            var progressMgr = GameProgressManager.Instance;
            if (progressMgr?.Data == null) return;

            int chapter = progressMgr.CurrentChapter;
            var profile = progressMgr.GetCurrentLevelProfile();

            float performanceRatio = 1f;
            if (profile != null && profile.targetScore > 0)
            {
                performanceRatio = (float)finalScore / profile.targetScore;
            }

            int completionCoins = rewardConfig.GetLevelCompletionCoins(chapter, performanceRatio);
            AwardCoins(completionCoins, "level_completion");

            if (!_firstClearBonusAwardedThisLevel)
            {
                int firstClearGems = rewardConfig.GetFirstTimeClearGems();
                AwardGems(firstClearGems, "first_time_clear");
                _firstClearBonusAwardedThisLevel = true;
            }

            int powerRefund = rewardConfig.GetPowerRefund();
            if (powerRefund > 0)
            {
                AwardPower(powerRefund, "level_completion_refund");
            }

            Debug.Log($"[RewardManager] Level {chapter} Complete | " +
                      $"Coins: {completionCoins} | " +
                      $"Performance: {performanceRatio:P0} | " +
                      $"Power Refund: {powerRefund}");
        }

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

        public int GetSessionCoinsThisLevel() => _sessionCoinsThisLevel;

        public int GetSessionGemsThisLevel() => _sessionGemsThisLevel;

        public Vector2Int GetExpectedCoinDropRange()
        {
            if (rewardConfig == null) return Vector2Int.zero;
            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;
            return rewardConfig.GetCoinDropRange(chapter);
        }

        public int EstimateExpectedCoinsForLevel(float performanceRatio = 1f)
        {
            if (rewardConfig == null) return 0;

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;
            var dropRange = rewardConfig.GetCoinDropRange(chapter);
            int avgDrops = (dropRange.x + dropRange.y) / 2;
            int estimatedDrops = avgDrops * 15;
            int completionBonus = rewardConfig.GetLevelCompletionCoins(chapter, performanceRatio);

            return estimatedDrops + completionBonus;
        }

        public void DebugAwardCoins(int amount)
        {
            AwardCoins(amount, "DEBUG");
        }

        public void DebugAwardGems(int amount)
        {
            AwardGems(amount, "DEBUG");
        }

        public void DebugAwardPower(int amount)
        {
            AwardPower(amount, "DEBUG");
        }
    }
}
