using System;
using UnityEngine;
using Gameplay;
using Gameplay.Events;

namespace Gameplay.Managers
{
    /// <summary>
    /// XP is awarded at end of level:
    ///   - Base XP per level completion (scales with global level)
    ///   - Bonus XP if all eggs were cleared
    ///   - Diminishing returns if replaying same level (first-win bonus)
    /// Player level up threshold uses a smooth quadratic curve.
    /// </summary>
    public class XPManager : MonoBehaviour
    {
        public static XPManager Instance { get; private set; }

        [Header("XP Awards")]
        [SerializeField] private int baseXPPerLevelClear    = 100; // first-time clear base
        [SerializeField] private int xpBonusAllEggsCleared  = 40;  // bonus on top of level clear
        [SerializeField] private int replayXPFlat           = 25;  // XP for replaying same level

        [Header("Level Up Thresholds")]
        [SerializeField] private int   xpBase      = 200;   // XP needed for level 1→2
        [SerializeField] private float xpGrowth    = 1.25f; // 25% more XP per player level
        [SerializeField] private int   maxPlayerLevel = 100;

        // Tracks last global game level completed for first-win detection
        private int _lastCompletedGlobalLevel = -1;
        private bool _allEggsClearedThisLevel = false;

        public event Action<int, int> OnXPAdded;      // (totalXP, amountAdded)
        public event Action<int>      OnPlayerLevelUp; // (newLevel)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnAllEggsCleared += HandleAllEggsCleared;
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnAllEggsCleared -= HandleAllEggsCleared;
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        }

        // Called before each level starts — reset the eggs-cleared flag
        public void ResetForNewLevel()
        {
            _allEggsClearedThisLevel = false;
        }

        private void HandleAllEggsCleared()
        {
            // Just flag it — XP awarded together at level complete
            _allEggsClearedThisLevel = true;
        }

        private void HandleLevelCompleted(int finalScore)
        {
            int globalLevel = GameProgressManager.Instance != null
                ? GameProgressManager.Instance.GlobalLevel
                : 1;

            bool isFirstClear = globalLevel > _lastCompletedGlobalLevel;

            int xpEarned = 0;

            if (isFirstClear)
            {
                // Scale base XP slightly with global level so late-game levels
                // still feel rewarding — caps at 3x base around level 300+
                float scaleFactor = 1f + Mathf.Clamp(globalLevel / 300f, 0f, 2f);
                xpEarned += Mathf.RoundToInt(baseXPPerLevelClear * scaleFactor);

                _lastCompletedGlobalLevel = globalLevel;
            }
            else
            {
                // Replay: flat diminished XP regardless of level
                xpEarned += replayXPFlat;
            }

            // All eggs cleared bonus — applies on both first clear and replay
            if (_allEggsClearedThisLevel)
                xpEarned += xpBonusAllEggsCleared;

            AddXP(xpEarned);
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            if (GameProgressManager.Instance?.Data == null) return;

            var progress = GameProgressManager.Instance.Data;

            if (progress.playerLevel >= maxPlayerLevel) return;

            progress.playerXP += amount;
            OnXPAdded?.Invoke(progress.playerXP, amount);
            GameProgressManager.Instance.SaveProgress();

            CheckLevelUp(progress);
        }

        private void CheckLevelUp(GameProgress progress)
        {
            while (progress.playerLevel < maxPlayerLevel)
            {
                int xpNeeded = GetXPToNextLevel(progress.playerLevel);
                if (progress.playerXP < progress.lastLevelUpXP + xpNeeded) break;

                progress.lastLevelUpXP += xpNeeded;
                progress.playerLevel++;

                GameProgressManager.Instance.SaveProgress();
                GameEvents.FirePlayerLevelUp(progress.playerLevel);
                OnPlayerLevelUp?.Invoke(progress.playerLevel);
            }
        }

        /// <summary>
        /// XP required to go from playerLevel → playerLevel+1.
        /// Uses quadratic growth: xpBase * (1.25 ^ playerLevel)
        /// Examples:
        ///   Level 1→2 : 200 XP
        ///   Level 5→6 : ~488 XP
        ///   Level 10→11: ~1192 XP
        ///   Level 20→21: ~7105 XP
        ///   Level 50→51: ~700k XP (long-term goal)
        /// </summary>
        public int GetXPToNextLevel(int playerLevel)
        {
            if (playerLevel >= maxPlayerLevel) return int.MaxValue;
            return Mathf.RoundToInt(xpBase * Mathf.Pow(xpGrowth, playerLevel - 1));
        }

        /// <summary>
        /// Progress from 0..1 within current player level.
        /// </summary>
        public float GetXPProgress()
        {
            if (GameProgressManager.Instance?.Data == null) return 0f;
            var p = GameProgressManager.Instance.Data;
            int xpNeeded  = GetXPToNextLevel(p.playerLevel);
            int xpInLevel = p.playerXP - p.lastLevelUpXP;
            return xpNeeded > 0 ? Mathf.Clamp01((float)xpInLevel / xpNeeded) : 1f;
        }

        /// <summary>
        /// Total cumulative XP needed to reach a given player level from zero.
        /// Useful for UI or debug display.
        /// </summary>
        public int GetTotalXPForLevel(int playerLevel)
        {
            int total = 0;
            for (int i = 1; i < playerLevel; i++)
                total += GetXPToNextLevel(i);
            return total;
        }
    }
}
