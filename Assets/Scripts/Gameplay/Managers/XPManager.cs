using System;
using UnityEngine;
using Gameplay;
using Gameplay.Events;

namespace Gameplay.Managers
{
    /// <summary>
    /// Awards XP when:
    ///   - All eggs are cleared from screen (OnAllEggsCleared)
    ///   - Level target score is reached (OnLevelCompleted)
    /// XP is meta-progression — persists across sessions via GameProgressManager.
    /// </summary>
    public class XPManager : MonoBehaviour
    {
        public static XPManager Instance { get; private set; }

        [Header("XP Awards")]
        [SerializeField] private int xpOnAllEggsCleared = 50;
        [SerializeField] private int xpOnTargetScoreReached = 100;

        [Header("Level Up Formula")]
        [SerializeField] private int baseXPPerLevel = 100;
        [SerializeField] private float levelExponent = 1.5f;

        public event Action<int, int> OnXPAdded;       // (totalXP, amountAdded)
        public event Action<int>      OnPlayerLevelUp;  // (newLevel)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnAllEggsCleared  += HandleAllEggsCleared;
            GameEvents.OnLevelCompleted  += HandleLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnAllEggsCleared  -= HandleAllEggsCleared;
            GameEvents.OnLevelCompleted  -= HandleLevelCompleted;
        }

        private void HandleAllEggsCleared()
        {
            AddXP(xpOnAllEggsCleared);
        }

        private void HandleLevelCompleted(int finalScore)
        {
            AddXP(xpOnTargetScoreReached);
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            if (GameProgressManager.Instance?.Data == null) return;

            var progress = GameProgressManager.Instance.Data;
            progress.playerXP += amount;

            OnXPAdded?.Invoke(progress.playerXP, amount);
            GameProgressManager.Instance.SaveProgress();

            CheckLevelUp(progress);
        }

        private void CheckLevelUp(GameProgress progress)
        {
            while (true)
            {
                int nextLevelXP = GetXPForLevel(progress.playerLevel + 1);
                if (progress.playerXP < nextLevelXP) break;

                progress.playerLevel++;
                progress.lastLevelUpXP = nextLevelXP;

                GameProgressManager.Instance.SaveProgress();
                GameEvents.FirePlayerLevelUp(progress.playerLevel);
                OnPlayerLevelUp?.Invoke(progress.playerLevel);
            }
        }

        public int GetXPForLevel(int level)
        {
            if (level <= 1) return 0;
            return Mathf.RoundToInt(baseXPPerLevel * Mathf.Pow(level - 1, levelExponent));
        }

        public float GetXPProgress()
        {
            if (GameProgressManager.Instance?.Data == null) return 0f;
            var p = GameProgressManager.Instance.Data;
            int current = p.playerXP - GetXPForLevel(p.playerLevel);
            int needed  = GetXPForLevel(p.playerLevel + 1) - GetXPForLevel(p.playerLevel);
            return needed > 0 ? Mathf.Clamp01((float)current / needed) : 1f;
        }
    }
}
