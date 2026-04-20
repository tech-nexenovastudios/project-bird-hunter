using System;
using System.Collections.Generic;
using Gameplay.Birds;
using Gameplay.Interfaces;
using Gameplay.Levels;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.Events
{
    public static class GameEvents
    {
        // ───────── Egg ─────────
        public static event Action<IDamageable, int, Vector3> OnEggHit;
        public static event Action<IDamageable, int, Vector3> OnEggDestroyed;

        // ───────── Bird ─────────
        public static event Action<IDamageable, int, Vector3> OnBirdHit;
        public static event Action<IDamageable, int, Vector3> OnBirdDestroyed;

        // ───────── Cannon ─────────
        public static event Action<int> OnCannonHit;

        // ───────── Score ─────────
        public static event Action<int, int> OnLevelScoreUpdated;
        public static event Action<int> OnPlayerCoinsUpdated;

        // ───────── XP / Level ─────────
        public static event Action<int> OnPlayerLevelUp;
        public static event Action<LevelProfile, int> OnGameLevelUpdated;

        // ───────── Level Flow ─────────
        public static event Action<int> OnLevelCompleted;
        public static event Action OnAllEggsCleared;

        // ───────── Player ─────────
        public static event Action OnPlayerDeath;


        //public static event Action OnSlotPanelClosed ;

        // ───────── Spin / Powerup ─────────
        public static event Action<PowerupConfig[]> OnSpinTriggered;
        public static event Action<PowerupConfig> OnPowerupSelected;
        public static event Action<PowerupConfig> OnPowerupCommitted;
        //public static event Action<PowerupConfig> OnPlayerConfirmedSpin;

        public static event Action<List<PowerupConfig>> OnSpinStarted;

        //notification panel
        //public static void FireSlotPanelClosed()
        //    => OnSlotPanelClosed?.Invoke();
        public static void FireSpinStarted(List<PowerupConfig> options)
            => OnSpinStarted?.Invoke(options);
        // ───────── UI ─────────
        // public static event Action OnBackToMenu;
        public static event Action<bool> OnPauseToggled;   // true = paused

        // ───────── Fire helpers ─────────
        public static void FireEggHit(IDamageable egg, int damage, Vector3 pos)
            => OnEggHit?.Invoke(egg, damage, pos);

        public static void FireEggDestroyed(IDamageable egg, int score, Vector3 pos)
            => OnEggDestroyed?.Invoke(egg, score, pos);

        public static void FireBirdHit(IDamageable bird, int damage, Vector3 pos)
            => OnBirdHit?.Invoke(bird, damage, pos);

        public static void FireBirdDestroyed(IDamageable bird, int score, Vector3 pos)
            => OnBirdDestroyed?.Invoke(bird, score, pos);

        public static void FireCannonHit(int damage)
            => OnCannonHit?.Invoke(damage);

        public static void FireLevelScoreUpdated(int current, int delta)
            => OnLevelScoreUpdated?.Invoke(current, delta);

        public static void FirePlayerCoinsUpdated(int total)
            => OnPlayerCoinsUpdated?.Invoke(total);

        public static void FirePlayerLevelUp(int newLevel)
            => OnPlayerLevelUp?.Invoke(newLevel);

        public static void FireGameLevelUpdated(LevelProfile profile, int index)
            => OnGameLevelUpdated?.Invoke(profile, index);

        public static void FireLevelCompleted(int score)
            => OnLevelCompleted?.Invoke(score);

        public static void FireAllEggsCleared()
            => OnAllEggsCleared?.Invoke();

        public static void FirePlayerDeath()
            => OnPlayerDeath?.Invoke();

        public static void FireSpinTriggered(PowerupConfig[] options)
            => OnSpinTriggered?.Invoke(options);

        public static void FirePowerupSelected(PowerupConfig config)
            => OnPowerupSelected?.Invoke(config);

        //public static void FirePlayerConfirmedSpin(PowerupConfig config)
        //    => OnPlayerConfirmedSpin?.Invoke(config);

        //public static void FireBackToMenu()
        //    => OnBackToMenu?.Invoke();

        public static void FirePauseToggled(bool isPaused)
            => OnPauseToggled?.Invoke(isPaused);


        // ── Boss events moved to BossEventBus (ScriptableObject). See Gameplay.BossBirds.Core.BossEventBus. ──

        // ── Attacking Bird Events ─────────────────────────────────────────────────

        /// <summary>Fired by AttackingBird.Die() when an attacking bird is killed by the player.</summary>
        public static event Action<AttackingBird, int> OnAttackingBirdDestroyed; // (bird, score)

        public static void FireAttackingBirdDestroyed(AttackingBird bird, int score)
            => OnAttackingBirdDestroyed?.Invoke(bird, score);

        public static void FireCannonHealthChanged(int currentHp, int maxHp) => OnCannonHealthChanged?.Invoke(currentHp, maxHp);
        public static event Action<int, int> OnCannonHealthChanged;

        public static void FireCannonStatsUpdated(CannonStats cannonStats) => OnCannonStatsUpdated?.Invoke(cannonStats);

        internal static void FirePowerupCommitted(PowerupConfig results) => OnPowerupCommitted?.Invoke(results);

        public static event Action<CannonStats> OnCannonStatsUpdated;
        // ───────── Powerup Cooldown ─────────
        public static event Action<float> OnPowerupCooldownStarted;

        public static void FirePowerupCooldownStarted(float duration)
            => OnPowerupCooldownStarted?.Invoke(duration);
        public static event Action OnPowerupUnequipped;
        public static void FirePowerupUnequipped() => OnPowerupUnequipped?.Invoke();
        public static void FireLevelCompletedEarly(float remaining) => OnLevelCompletedEarly?.Invoke(remaining);
        public static event Action<float> OnLevelCompletedEarly;

        public static event Action<int> OnChapterCompleted;  // int = new chapter number
        public static void FireChapterCompleted(int newChapter)
            => OnChapterCompleted?.Invoke(newChapter);
    }
}