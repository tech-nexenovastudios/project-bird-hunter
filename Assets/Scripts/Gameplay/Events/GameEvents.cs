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
using Gameplay.Birds;
using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.PowerUps;

namespace Gameplay.Events
{
    public static class GameEvents
    {
        // ── Egg Events ───────────────────────────────────────
        public static event Action<IDamageable, int, Vector3> OnEggHit;
        public static event Action<IDamageable, int, Vector3> OnEggDestroyed;

        // ── Bird Events ──────────────────────────────────────
        public static event Action<IDamageable, int, Vector3> OnBirdHit;
        public static event Action<IDamageable, int, Vector3> OnBirdDestroyed;

        // ── Cannon ───────────────────────────────────────────
        public static event Action<int> OnCannonHit;

        // ── Score ────────────────────────────────────────────
        public static event Action<int, int> OnLevelScoreUpdated;

        // ── Meta-Progression ─────────────────────────────────
        public static event Action<int> OnPlayerLevelUp;
        public static event Action<int> OnPlayerCoinsUpdated;

        // ── Level ────────────────────────────────────────────
        public static event Action<int> OnLevelCompleted;

        // ── Player ───────────────────────────────────────────
        public static event Action OnPlayerDeath;
        public static event Action OnAllEggsCleared;

        // ── Slot Machine ─────────────────────────────────────
        public static event Action OnSpinTriggered;
        public static event Action OnPlayerConfirmedSpin;

        // ── PowerUps ─────────────────────────────────────────
        public static event Action<PowerupConfig> OnPowerupSelected;

        // ── Navigation ───────────────────────────────────────
        public static event Action OnBackToMenu;
        public static event Action OnPauseGame;

        // ── Level UI ─────────────────────────────────────────
        public static event Action<Gameplay.Levels.LevelProfile, int> OnGameLevelUpdated;

        // ── Fire Methods ─────────────────────────────────────

        public static void FireAllEggsCleared() => OnAllEggsCleared?.Invoke();
        public static void FireEggHit(IDamageable egg, int damage, Vector3 hitPoint)
                                     => OnEggHit?.Invoke(egg, damage, hitPoint);
        public static void FireEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position)
                                                            => OnEggDestroyed?.Invoke(egg, scoreAwarded, position);
        public static void FireBirdHit(IDamageable bird, int damage, Vector3 hitPoint)
                                                            => OnBirdHit?.Invoke(bird, damage, hitPoint);
        public static void FireBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position)
                                                            => OnBirdDestroyed?.Invoke(bird, scoreAwarded, position);
        public static void FireCannonHit(int damage) => OnCannonHit?.Invoke(damage);
        public static void FireLevelScoreUpdated(int currentScore, int delta)
                                                            => OnLevelScoreUpdated?.Invoke(currentScore, delta);
        public static void FirePlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
        public static void FirePlayerCoinsUpdated(int total) => OnPlayerCoinsUpdated?.Invoke(total);
        public static void FireLevelCompleted(int score) => OnLevelCompleted?.Invoke(score);
        public static void FirePlayerDeath() => OnPlayerDeath?.Invoke();
        public static void FireSpinTriggered() => OnSpinTriggered?.Invoke();
        public static void FirePlayerConfirmedSpin() => OnPlayerConfirmedSpin?.Invoke();
        public static void FirePowerupSelected(PowerupConfig config)
                                                            => OnPowerupSelected?.Invoke(config);
        public static void FireBackToMenu() => OnBackToMenu?.Invoke();
        public static void FirePauseGame() => OnPauseGame?.Invoke();
        public static void FireGameLevelUpdated(Gameplay.Levels.LevelProfile profile, int levelIndex)
                                                            => OnGameLevelUpdated?.Invoke(profile, levelIndex);
        
        
        // ── Boss Events ───────────────────────────────────────────────────────────
 
        /// <summary>Fired by BossBird.InitBoss() when the boss enters the scene.</summary>
        public static event Action<BossBird> OnBossSpawned;
 
        /// <summary>Fired by BossBird.EnterPhase2() when HP crosses the phase threshold.</summary>
        public static event Action<BossBird> OnBossPhase2;
 
        /// <summary>Fired by BossBird.Die() when the boss is killed by the player.</summary>
        public static event Action<BossBird> OnBossDefeated;
 
        /// <summary>Fired by BossBird.TriggerBurstAttack().</summary>
        public static event Action<BossBird, int> OnBossBurstAttack; // (boss, eggCount)
 
        public static void FireBossSpawned(BossBird boss)        => OnBossSpawned?.Invoke(boss);
        public static void FireBossPhase2(BossBird boss)         => OnBossPhase2?.Invoke(boss);
        public static void FireBossDefeated(BossBird boss)       => OnBossDefeated?.Invoke(boss);
        public static void FireBossBurstAttack(BossBird boss, int count) => OnBossBurstAttack?.Invoke(boss, count);
 
// ── Attacking Bird Events ─────────────────────────────────────────────────
 
        /// <summary>Fired by AttackingBird.Die() when an attacking bird is killed by the player.</summary>
        public static event Action<AttackingBird, int> OnAttackingBirdDestroyed; // (bird, score)
 
        public static void FireAttackingBirdDestroyed(AttackingBird bird, int score)
            => OnAttackingBirdDestroyed?.Invoke(bird, score);
    }
}
