using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Rewards;
using Cysharp.Threading.Tasks;

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
        [SerializeField] private Canvas hudCanvas; // assign in Inspector

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

       

        private void HandleEggDestroyed(Interfaces.IDamageable egg, int unused, Vector3 worldPosition)
        {
            if (rewardConfig == null)
            {
                Debug.LogWarning("[RewardManager] No rewardConfig assigned!");
                return;
            }

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;

            // Eggs killed via grace-expiry force-destroy pay reduced drops.
            float multiplier = (SpawnController.Instance != null && SpawnController.Instance.IsForceDestroying)
                ? Mathf.Clamp01(rewardConfig.forceDestroyRewardMultiplier)
                : 1f;

            // ── World → screen conversion ─────────────────────────────────
            // The egg lives in 3D world space; CoinFlowManager expects a
            // screen-space Vector2.  We project through the main camera first,
            // then optionally remap for Screen Space - Camera canvases.
            Vector2 screenPos = WorldToCanvasScreenPos(worldPosition);

            // ── Coins ─────────────────────────────────────────────────────
            int coins = Mathf.RoundToInt(rewardConfig.GetRandomCoinDrop(chapter) * multiplier);
            AwardCoins(coins, multiplier < 1f ? "egg_destroyed_grace" : "egg_destroyed");
            GameEvent.CurrencyCollected(CurrencyType.Gold, screenPos, coins);

            // ── Gems (chance-based) ───────────────────────────────────────
            if (rewardConfig.ShouldDropGem(chapter))
            {
                int gems = Mathf.RoundToInt(rewardConfig.GetRandomGemDrop(chapter) * multiplier);
                if (gems > 0)
                {
                    AwardGems(gems, multiplier < 1f ? "egg_gem_drop_grace" : "egg_gem_drop");
                    GameEvent.CurrencyCollected(CurrencyType.Gems, screenPos, gems);
                }
            }
        }

        //helper
        private Vector2 WorldToCanvasScreenPos(Vector3 worldPos)
        {
            return Camera.main.WorldToScreenPoint(worldPos);
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

            int targetScore = SpawnController.Instance != null ? SpawnController.Instance.TargetScore : 0;
            float performanceRatio = targetScore > 0 ? (float)finalScore / targetScore : 1f;

            bool forceDestroyed = SpawnController.Instance != null
                && SpawnController.Instance.LastCompletionWasForceDestroy;
            float completionMultiplier = forceDestroyed
                ? Mathf.Clamp01(rewardConfig.forceDestroyRewardMultiplier)
                : 1f;

            int completionCoins = Mathf.RoundToInt(
                rewardConfig.GetLevelCompletionCoins(chapter, performanceRatio) * completionMultiplier);
            AwardCoins(completionCoins, forceDestroyed ? "level_completion_grace" : "level_completion");

            // Suppress the first-clear gem bonus if the level was force-completed and the config
            // says so — handing the player a flagship reward they didn't actually clear feels wrong.
            bool skipFirstClear = forceDestroyed && rewardConfig.suppressFirstClearGemsOnForceDestroy;
            if (!_firstClearBonusAwardedThisLevel && !skipFirstClear)
            {
                int firstClearGems = rewardConfig.GetFirstTimeClearGems();
                AwardGems(firstClearGems, "first_time_clear");
                _firstClearBonusAwardedThisLevel = true;
            }

            bool skipPowerRefund = forceDestroyed && rewardConfig.suppressPowerRefundOnForceDestroy;
            int powerRefund = skipPowerRefund ? 0 : rewardConfig.GetPowerRefund();
            if (powerRefund > 0)
            {
                AwardPower(powerRefund, "level_completion_refund");
            }

            Debug.Log($"[RewardManager] Level {chapter} Complete | " +
                      $"Coins: {completionCoins} | " +
                      $"Performance: {performanceRatio:P0} | " +
                      $"Power Refund: {powerRefund} | " +
                      $"ForceDestroyed: {forceDestroyed}");
        }

        // RewardManager.cs  ─  replace the three private Award methods

        private void AwardCoins(int amount, string reason = "")
        {
            if (amount <= 0) return;


            _sessionCoinsThisLevel += amount;

        

            // ── Persist to Unity Economy + fire OnCurrencyChanged in real-time ─
            CurrencyManager.Instance.AddGold(amount).Forget();

            OnCoinsAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("coins", amount);

            Debug.Log($"[RewardManager] +{amount} coins ({reason}) ");
        }

        private void AwardGems(int amount, string reason = "")
        {
            if (amount <= 0) return;

            _sessionGemsThisLevel += amount;

    

            CurrencyManager.Instance.AddGems(amount).Forget();

            OnGemsAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("gems", amount);

            Debug.Log($"[RewardManager] +{amount} gems ({reason}) ");
        }

        private void AwardPower(int amount, string reason = "")
        {
            if (amount <= 0) return;

            _sessionPowerRefundsThisLevel += amount;

         

            CurrencyManager.Instance.AddPower(amount).Forget();

            OnPowerAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("power", amount);

            Debug.Log($"[RewardManager] +{amount} power ({reason}) ");
        }

        public int GetSessionCoinsThisLevel() => _sessionCoinsThisLevel;

        public int GetSessionGemsThisLevel() => _sessionGemsThisLevel;
        public int GetSessionPowerThisLevel() => _sessionPowerRefundsThisLevel;

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
