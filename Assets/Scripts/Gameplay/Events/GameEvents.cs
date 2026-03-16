using System;
using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Levels;
using Gameplay.PowerUps;

namespace Gameplay.Events
{
    public static class GameEvents
    {
        public static event Action<LevelProfile, int> OnGameLevelUpdated;
        public static event Action OnGameEnd;
        public static event Action OnGameReset;
        public static event Action OnGamePause;
        public static event Action OnGameResume;
        public static event Action OnGameRestart;
        public static event Action OnGameQuit;
        public static event Action OnGameOver;
        
        public static event Action OnSpinTriggered;
        public static event Action OnPlayerConfirmedSpin;
        public static event Action OnSpinComplete;
        public static event Action<PowerupConfig> OnPowerupSelected;
        
        public static event Action<int> OnProgressUpdated;
        
        public static event Action<int> OnLevelLoaded;
        
        public static event Action<int> OnLevelCleared;
        
        public static event Action<int> OnPlayerCoinsUpdated;
        public static event Action<int> OnPlayerGemsUpdated;
        public static event Action<int> OnPlayerXPUpdated;
        public static event Action<int> OnPlayerScoreUpdated;
        public static event Action<int> OnPlayerHighScoreUpdated;
        public static event Action<int> OnPlayerTotalScoreUpdated;
        
        
        
        
        // Egg events
        public static event Action<IDamageable, int, Vector3> OnEggHit;
        public static event Action<IDamageable, int, Vector3> OnEggDestroyed;

        // Bird events
        public static event Action<IDamageable, int, Vector3> OnBirdHit;
        public static event Action<IDamageable, int, Vector3> OnBirdDestroyed;

        // Cannon hit (for combo/feedback)
        public static event Action<int> OnCannonHit;

        // Score
        public static event Action<int, int> OnLevelScoreUpdated;

        // Meta-progression (XP / player level)
        public static event Action<int> OnPlayerLevelUp;

        // Level completion (score-based)
        public static event Action<int> OnLevelCompleted;

        // Player death
        public static event Action OnPlayerDeath;
        public static event Action OnAllEggsCleared;
        
        public static void FireGameLevelUpdated(LevelProfile profile, int levelIndex) => OnGameLevelUpdated?.Invoke(profile, levelIndex);
        public static void FireGameEnd() => OnGameEnd?.Invoke();
        public static void FireGameReset() => OnGameReset?.Invoke();
        public static void FireGamePause() => OnGamePause?.Invoke();
        public static void FireGameResume() => OnGameResume?.Invoke();
        public static void FireGameRestart() => OnGameRestart?.Invoke();
        
        public static void FireGameQuit() => OnGameQuit?.Invoke();
        public static void FireGameOver() => OnGameOver?.Invoke();
        
        public static void FireSpinTriggered() => OnSpinTriggered?.Invoke();
        public static void FireSpinComplete() => OnSpinComplete?.Invoke();
        public static void FirePowerupSelected(PowerupConfig powerupConfig) => OnPowerupSelected?.Invoke(powerupConfig);
        public static void FirePlayerConfirmedSpin() => OnPlayerConfirmedSpin?.Invoke();
        public static void FirePlayerCoinsUpdated(int coins) => OnPlayerCoinsUpdated?.Invoke(coins);
        public static void FireProgressUpdated(int progress) => OnProgressUpdated?.Invoke(progress);
        
        public static void FireLevelLoaded(int levelIndex) => OnLevelLoaded?.Invoke(levelIndex);
        public static void FireLevelCleared(int levelIndex) => OnLevelCleared?.Invoke(levelIndex);
        public static void FirePlayerGemsUpdated(int gems) => OnPlayerGemsUpdated?.Invoke(gems);
        public static void FirePlayerXPUpdated(int xp) => OnPlayerXPUpdated?.Invoke(xp);
        public static void FirePlayerScoreUpdated(int score) => OnPlayerScoreUpdated?.Invoke(score);
        public static void FirePlayerHighScoreUpdated(int highScore) => OnPlayerHighScoreUpdated?.Invoke(highScore);
        public static void FirePlayerTotalScoreUpdated(int totalScore) => OnPlayerTotalScoreUpdated?.Invoke(totalScore);
        
        public static void FireAllEggsCleared() => OnAllEggsCleared?.Invoke();

        public static void FireEggHit(IDamageable egg, int damage, Vector3 hitPoint) =>
            OnEggHit?.Invoke(egg, damage, hitPoint);

        public static void FireEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position) =>
            OnEggDestroyed?.Invoke(egg, scoreAwarded, position);

        public static void FireBirdHit(IDamageable bird, int damage, Vector3 hitPoint) =>
            OnBirdHit?.Invoke(bird, damage, hitPoint);

        public static void FireBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position) =>
            OnBirdDestroyed?.Invoke(bird, scoreAwarded, position);

        public static void FireCannonHit(int damage) =>
            OnCannonHit?.Invoke(damage);

        public static void FireLevelScoreUpdated(int currentScore, int delta) =>
            OnLevelScoreUpdated?.Invoke(currentScore, delta);

        public static void FirePlayerLevelUp(int newLevel) =>
            OnPlayerLevelUp?.Invoke(newLevel);

        public static void FireLevelCompleted(int scoreAchieved) =>
            OnLevelCompleted?.Invoke(scoreAchieved);

        public static void FirePlayerDeath() =>
            OnPlayerDeath?.Invoke();
    }
}
