//using System;
//using UnityEngine;
//using Gameplay.Interfaces;

//namespace Gameplay.Events
//{
//    public static class GameEvents
//    {
//        // Egg events
//        public static event Action<IDamageable, int, Vector3> OnEggHit;
//        public static event Action<IDamageable, int, Vector3> OnEggDestroyed;

//        // Bird events
//        public static event Action<IDamageable, int, Vector3> OnBirdHit;
//        public static event Action<IDamageable, int, Vector3> OnBirdDestroyed;

//        // Cannon hit (for combo/feedback)
//        public static event Action<int> OnCannonHit;

//        // Score
//        public static event Action<int, int> OnLevelScoreUpdated;

//        // Meta-progression (XP / player level)
//        public static event Action<int> OnPlayerLevelUp;

//        // Level completion (score-based)
//        public static event Action<int> OnLevelCompleted;

//        // Player death
//        public static event Action OnPlayerDeath;
//        public static event Action OnAllEggsCleared;
//        public static void FireAllEggsCleared() => OnAllEggsCleared?.Invoke();

//        public static void FireEggHit(IDamageable egg, int damage, Vector3 hitPoint) =>
//            OnEggHit?.Invoke(egg, damage, hitPoint);

//        public static void FireEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position) =>
//            OnEggDestroyed?.Invoke(egg, scoreAwarded, position);

//        public static void FireBirdHit(IDamageable bird, int damage, Vector3 hitPoint) =>
//            OnBirdHit?.Invoke(bird, damage, hitPoint);

//        public static void FireBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position) =>
//            OnBirdDestroyed?.Invoke(bird, scoreAwarded, position);

//        public static void FireCannonHit(int damage) =>
//            OnCannonHit?.Invoke(damage);

//        public static void FireLevelScoreUpdated(int currentScore, int delta) =>
//            OnLevelScoreUpdated?.Invoke(currentScore, delta);

//        public static void FirePlayerLevelUp(int newLevel) =>
//            OnPlayerLevelUp?.Invoke(newLevel);

//        public static void FireLevelCompleted(int scoreAchieved) =>
//            OnLevelCompleted?.Invoke(scoreAchieved);

//        public static void FirePlayerDeath() =>
//            OnPlayerDeath?.Invoke();
//    }
//}


using System;
using System.Collections.Generic;
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

        // ───────── Spin / Powerup ─────────
        public static event Action<PowerupConfig[]> OnSpinTriggered;
        public static event Action<PowerupConfig> OnPowerupSelected;
        public static event Action OnPlayerConfirmedSpin;
     
        public static event Action<List<PowerupConfig>> OnSpinStarted;
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

        public static void FirePlayerConfirmedSpin()
            => OnPlayerConfirmedSpin?.Invoke();

        //public static void FireBackToMenu()
        //    => OnBackToMenu?.Invoke();

        public static void FirePauseToggled(bool isPaused)
            => OnPauseToggled?.Invoke(isPaused);


    }
}