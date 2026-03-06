using System;
using UnityEngine;
using Gameplay;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.Eggs;

namespace Gameplay.Managers
{
    public class XPManager : MonoBehaviour
    {
        public static XPManager Instance { get; private set; }

        [Header("XP Formula")]
        [SerializeField] private int baseXPPerLevel = 100;
        [SerializeField] private float levelExponent = 1.5f;

        public event Action<int, int> OnXPAdded;
        public event Action<int> OnPlayerLevelUp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // FIXED: only subscribe to OnEggHit — XP earned only during active combo on egg hits
            GameEvents.OnEggHit += OnEggHit;
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnEggHit;
        }

        private void OnEggHit(IDamageable egg, int damage, Vector3 hitPoint)
        {
            var config = GetEggConfig(egg);
            if (config == null) return;

            int xp = config.scorePerHit;
            if (xp <= 0) return;

            float multiplier = ComboController.Instance != null ? ComboController.Instance.ComboMultiplier : 1f;
            AddXP(Mathf.RoundToInt(xp * multiplier)); // AddXP will reject if combo is inactive
        }


        private EggTierConfig GetEggConfig(IDamageable damageable)
        {
            if (damageable is not MonoBehaviour mb) return null;
            var egg = mb.GetComponent<Egg>();
            return egg?.config;
        }

        public void AddXP(int amount)
        {
            // Guard: only add XP if combo is active
            if (ComboController.Instance == null) return;
            if (ComboController.Instance.ComboCount <= 0) return;

            if (GameProgressManager.Instance == null || GameProgressManager.Instance.Data == null) return;

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
            int needed = GetXPForLevel(p.playerLevel + 1) - GetXPForLevel(p.playerLevel);
            return needed > 0 ? Mathf.Clamp01((float)current / needed) : 1f;
        }
    }
}
