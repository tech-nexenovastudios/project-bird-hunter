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

        // Per-run totals — accumulate across every level in a single play session and
        // only reset when a new run begins (GameManager.InitWithDelay / ResetGame).
        // Game Over UI reads these so the player sees their full run earnings, not
        // just the final level's contribution.
        private int _runCoins;
        private int _runGems;
        private int _runPower;

        // Prevention combo: consecutive birds killed before they could lay. Resets when an egg
        // successfully lands (FireEggSpawned) or a post-lay bird is killed. Triggers escalating
        // bonus payouts at milestone counts so a skilled player flexing on bird-prevention gets
        // visible "Combo x3 / x5 / x8" pulses with extra coins.
        private int _preventionStreak;
        private static readonly int[] ComboMilestones = { 3, 5, 8, 12, 18 };

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
            GameEvents.OnEggSpawned += HandleEggSpawned;
        }

        private void OnDisable()
        {
            GameEvents.OnEggDestroyed -= HandleEggDestroyed;
            GameEvents.OnBirdDestroyed -= HandleBirdDestroyed;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
            GameEvents.OnEggSpawned -= HandleEggSpawned;
        }

        public void ResetForNewLevel()
        {
            _sessionCoinsThisLevel = 0;
            _sessionGemsThisLevel = 0;
            _sessionPowerRefundsThisLevel = 0;
            _firstClearBonusAwardedThisLevel = false;
            _preventionStreak = 0;
            Debug.Log("[RewardManager] Reset for new level");
        }

        // Called when a fresh run starts (entering Gameplay scene). Does NOT fire between
        // levels — that's ResetForNewLevel's job.
        public void ResetForNewRun()
        {
            _runCoins = 0;
            _runGems = 0;
            _runPower = 0;
            Debug.Log("[RewardManager] Reset for new run");
        }

        // Combo break — a bird successfully laid an egg, so the prevention streak ends.
        private void HandleEggSpawned() => _preventionStreak = 0;

       

        private void HandleEggDestroyed(Interfaces.IDamageable egg, int unused, Vector3 worldPosition)
        {
            if (rewardConfig == null)
            {
                Debug.LogWarning("[RewardManager] No rewardConfig assigned!");
                return;
            }

            int chapter = GameProgressManager.Instance?.CurrentChapter ?? 1;
            Vector2 screenPos = WorldToCanvasScreenPos(worldPosition);

            int coins = rewardConfig.GetRandomCoinDrop(chapter);
            AwardCoins(coins, "egg_destroyed");
            GameEvent.CurrencyCollected(CurrencyType.Gold, screenPos, coins);

            if (rewardConfig.ShouldDropGem(chapter))
            {
                int gems = rewardConfig.GetRandomGemDrop(chapter);
                if (gems > 0)
                {
                    AwardGems(gems, "egg_gem_drop");
                    GameEvent.CurrencyCollected(CurrencyType.Gems, screenPos, gems);
                    // Gem drops are rare and worth surfacing — unlike the per-egg coin drip, which
                    // happens every kill and would spam the toast.
                    GameEvents.FireRewardNotification(new RewardNotification(
                        RewardKind.GemDrop, "Gem Found!", gems, "gems"));
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

            // One-lay model differentiates the kill:
            //   • Killed before lay → "prevention" — full coin (rewards saving an egg).
            //   • Killed after lay (during flee) → "chase bonus" — half coin on top of the egg.
            var baseBird = bird as Birds.BaseBird;
            bool preventedLay = baseBird != null && !baseBird.HasLaid;
            float multiplier = preventedLay ? 1.0f : 0.5f;
            string reason = preventedLay ? "bird_killed_prevention" : "bird_killed_chase";

            int coins = Mathf.RoundToInt(coinRange * multiplier);
            AwardCoins(coins, reason);

            if (preventedLay)
            {
                _preventionStreak++;
                GameEvents.FireRewardNotification(new RewardNotification(
                    RewardKind.BirdSaved, "Bird Saved", coins, "coins"));
                TryFireComboMilestone();
            }
            else
            {
                _preventionStreak = 0;
                GameEvents.FireRewardNotification(new RewardNotification(
                    RewardKind.ChaseBonus, "Chase Bonus", coins, "coins"));
            }
        }

        // Fires a ComboStreak notification + bonus coins when the prevention streak hits a milestone.
        // Each milestone awards 25 × streak coins so the bonus scales — a Combo x12 is meaningfully
        // bigger than a Combo x3 and rewards genuine skill flexes.
        private void TryFireComboMilestone()
        {
            for (int i = 0; i < ComboMilestones.Length; i++)
            {
                if (_preventionStreak == ComboMilestones[i])
                {
                    int bonus = 25 * _preventionStreak;
                    AwardCoins(bonus, $"combo_streak_x{_preventionStreak}");
                    GameEvents.FireRewardNotification(new RewardNotification(
                        RewardKind.ComboStreak, $"Combo x{_preventionStreak}!", bonus, "coins"));
                    return;
                }
            }
        }

        private void HandleLevelCompleted(int finalScore)
        {
            if (rewardConfig == null) return;

            var progressMgr = GameProgressManager.Instance;
            if (progressMgr?.Data == null) return;

            int chapter = progressMgr.CurrentChapter;

            int targetScore = SpawnController.Instance != null ? SpawnController.Instance.TargetScore : 0;
            float performanceRatio = targetScore > 0 ? (float)finalScore / targetScore : 1f;

            int completionCoins = rewardConfig.GetLevelCompletionCoins(chapter, performanceRatio);
            AwardCoins(completionCoins, "level_completion");
            GameEvents.FireRewardNotification(new RewardNotification(
                RewardKind.LevelComplete, "Level Complete!", completionCoins, "coins"));

            if (!_firstClearBonusAwardedThisLevel)
            {
                int firstClearGems = rewardConfig.GetFirstTimeClearGems();
                AwardGems(firstClearGems, "first_time_clear");
                _firstClearBonusAwardedThisLevel = true;
                if (firstClearGems > 0)
                {
                    GameEvents.FireRewardNotification(new RewardNotification(
                        RewardKind.FirstClear, "First Clear!", firstClearGems, "gems"));
                }
            }

            int powerRefund = rewardConfig.GetPowerRefund();
            if (powerRefund > 0)
            {
                AwardPower(powerRefund, "level_completion_refund");
                GameEvents.FireRewardNotification(new RewardNotification(
                    RewardKind.PowerRestored, "Power Restored", powerRefund, "power"));
            }

            Debug.Log($"[RewardManager] Level {chapter} Complete | " +
                      $"Coins: {completionCoins} | " +
                      $"Performance: {performanceRatio:P0} | " +
                      $"Power Refund: {powerRefund}");
        }

        // RewardManager.cs  ─  replace the three private Award methods

        // Counters are bumped only after the Economy credit confirms. Otherwise a failed
        // network call (which CurrencyManager logs but swallows) would leave Game Over
        // showing coins the player never actually banked.
        private async UniTaskVoid AwardCoins(int amount, string reason = "")
        {
            if (amount <= 0) return;

            bool credited = await CurrencyManager.Instance.AddGold(amount);
            if (!credited)
            {
                Debug.LogWarning($"[RewardManager] AddGold failed for {amount} ({reason}); counters not bumped");
                return;
            }

            _sessionCoinsThisLevel += amount;
            _runCoins += amount;

            OnCoinsAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("coins", amount);

            Debug.Log($"[RewardManager] +{amount} coins ({reason}) ");
        }

        private async UniTaskVoid AwardGems(int amount, string reason = "")
        {
            if (amount <= 0) return;

            bool credited = await CurrencyManager.Instance.AddGems(amount);
            if (!credited)
            {
                Debug.LogWarning($"[RewardManager] AddGems failed for {amount} ({reason}); counters not bumped");
                return;
            }

            _sessionGemsThisLevel += amount;
            _runGems += amount;

            OnGemsAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("gems", amount);

            Debug.Log($"[RewardManager] +{amount} gems ({reason}) ");
        }

        private async UniTaskVoid AwardPower(int amount, string reason = "")
        {
            if (amount <= 0) return;

            bool credited = await CurrencyManager.Instance.AddPower(amount);
            if (!credited)
            {
                Debug.LogWarning($"[RewardManager] AddPower failed for {amount} ({reason}); counters not bumped");
                return;
            }

            _sessionPowerRefundsThisLevel += amount;
            _runPower += amount;

            OnPowerAwarded?.Invoke(amount);
            OnRewardBroadcast?.Invoke("power", amount);

            Debug.Log($"[RewardManager] +{amount} power ({reason}) ");
        }

        public int GetSessionCoinsThisLevel() => _sessionCoinsThisLevel;

        public int GetSessionGemsThisLevel() => _sessionGemsThisLevel;
        public int GetSessionPowerThisLevel() => _sessionPowerRefundsThisLevel;

        public int GetRunCoins() => _runCoins;
        public int GetRunGems() => _runGems;
        public int GetRunPower() => _runPower;

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
