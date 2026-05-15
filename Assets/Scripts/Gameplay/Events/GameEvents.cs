using System;
using System.Collections.Generic;
using BirdHunter.Inventory.Stats;
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
        // Fires when a bird successfully lays an egg into the playfield. Used by the reward
        // combo tracker to detect when a "prevention streak" is broken — even if the player
        // doesn't shoot the bird, the egg landing breaks the streak.
        public static event Action OnEggSpawned;
        public static void FireEggSpawned() => OnEggSpawned?.Invoke();

        // Fires every time an egg launches off the ground. Used by the SFX bridge to play a
        // bounce sound at the impact point. Per-egg, so this fires frequently — only routed
        // through SFXController which throttles via AudioSource voice limits.
        public static event Action<Vector3> OnEggBounced;
        public static void FireEggBounced(Vector3 pos) => OnEggBounced?.Invoke(pos);

        // ───────── Bird ─────────
        public static event Action<IDamageable, int, Vector3> OnBirdHit;
        public static event Action<IDamageable, int, Vector3> OnBirdDestroyed;

        // Per-bird flap loop lifecycle. Routed through SFXController which owns the clip and
        // attaches a looping AudioSource to the bird while fly_N is the active Spine animation.
        public static event Action<NormalBird> OnBirdFlapStart;
        public static event Action<NormalBird> OnBirdFlapStop;
        public static void FireBirdFlapStart(NormalBird bird) => OnBirdFlapStart?.Invoke(bird);
        public static void FireBirdFlapStop(NormalBird bird) => OnBirdFlapStop?.Invoke(bird);

        // ───────── Cannon ─────────
        public static event Action<int> OnCannonHit;
        public static event Action OnCannonShoot;
     

        public static void FireCannonShoot() => OnCannonShoot?.Invoke();

        // ───────── Score ─────────
        public static event Action<int, int> OnLevelScoreUpdated;
        public static event Action<int> OnPlayerCoinsUpdated;

        // ───────── XP / Level ─────────
        public static event Action<int> OnPlayerLevelUp;
        public static event Action<int> OnGameLevelUpdated;

        // ───────── Level Flow ─────────
        public static event Action<int> OnLevelCompleted;
        public static event Action OnAllEggsCleared;
        // Level-start "3-2-1-Go!" countdown beats (UI shown in LevelDetailPopup). Tick fires
        // 3 times (at 3, 2, 1); Go fires once on the final "Go!" beat. Neither fires for the
        // post-level "Next level in N..." popup.
        public static event Action OnLevelCountdownTick;
        public static event Action OnLevelCountdownGo;

        // ───────── Player / Cannon─────────
        public static event Action OnPlayerDeath;
        // Fires the moment the cannon's destroy animation begins (before OnPlayerDeath,
        // which only fires once the death sequence finishes and the GameOver UI is shown).
        public static event Action OnCannonDestroyStarted;
        


        //public static event Action OnSlotPanelClosed ;

        // ───────── Spin / Powerup ─────────
        public static event Action<PowerupConfig[]> OnSpinTriggered;
        public static event Action<PowerupConfig> OnPowerupSelected;
        public static event Action<PowerupConfig> OnPowerupCommitted;
        //public static event Action<PowerupConfig> OnPlayerConfirmedSpin;

        public static event Action<List<PowerupConfig>> OnSpinStarted;

        // Fired when all reels finish their visual spin animation (before player makes a selection).
        public static event Action OnSpinAnimationCompleted;

        //notification panel
        //public static void FireSlotPanelClosed()
        //    => OnSlotPanelClosed?.Invoke();
        public static void FireSpinStarted(List<PowerupConfig> options)
            => OnSpinStarted?.Invoke(options);

        public static void FireSpinAnimationCompleted()
            => OnSpinAnimationCompleted?.Invoke();
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

        public static void FireGameLevelUpdated(int index)
            => OnGameLevelUpdated?.Invoke(index);

        public static void FireLevelCompleted(int score)
            => OnLevelCompleted?.Invoke(score);

        public static void FireAllEggsCleared()
            => OnAllEggsCleared?.Invoke();

        public static void FireLevelCountdownTick()
            => OnLevelCountdownTick?.Invoke();

        public static void FireLevelCountdownGo()
            => OnLevelCountdownGo?.Invoke();

        public static void FirePlayerDeath()
            => OnPlayerDeath?.Invoke();

        public static void FireCannonDestroyStarted()
            => OnCannonDestroyStarted?.Invoke();

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


        /// <summary>Fired by BossBird.InitBoss() when the boss enters the scene.</summary>
        public static event Action<BossBird> OnBossSpawned;

        /// <summary>Fired by BossBird.EnterPhase2() when HP crosses the phase threshold.</summary>
        public static event Action<BossBird> OnBossPhase2;

        /// <summary>Fired by BossBird.Die() when the boss is killed by the player.</summary>
        public static event Action<BossBird> OnBossDefeated;

        /// <summary>Fired by BossBird.TriggerBurstAttack().</summary>
        public static event Action<BossBird, int> OnBossBurstAttack; // (boss, eggCount)

        public static void FireBossSpawned(BossBird boss) => OnBossSpawned?.Invoke(boss);
        public static void FireBossPhase2(BossBird boss) => OnBossPhase2?.Invoke(boss);
        public static void FireBossDefeated(BossBird boss) => OnBossDefeated?.Invoke(boss);
        public static void FireBossBurstAttack(BossBird boss, int count) => OnBossBurstAttack?.Invoke(boss, count);

        // ── Attacking Bird Events ─────────────────────────────────────────────────

        /// <summary>Fired by AttackingBird.Die() when an attacking bird is killed by the player.</summary>
        public static event Action<AttackingBird, int> OnAttackingBirdDestroyed; // (bird, score)

        public static void FireAttackingBirdDestroyed(AttackingBird bird, int score)
            => OnAttackingBirdDestroyed?.Invoke(bird, score);

        public static void FireCannonHealthChanged(int currentHp, int maxHp) => OnCannonHealthChanged?.Invoke(currentHp, maxHp);
        public static event Action<int, int> OnCannonHealthChanged;

        public static void FireCannonStatsUpdated(StatSheet statSheet) => OnCannonStatsUpdated?.Invoke(statSheet);

        internal static void FirePowerupCommitted(PowerupConfig results) => OnPowerupCommitted?.Invoke(results);

        public static event Action<StatSheet> OnCannonStatsUpdated;

        public static void FireLevelCompletedEarly(float remaining) => OnLevelCompletedEarly?.Invoke(remaining);
        public static event Action<float> OnLevelCompletedEarly;

        // ───────── Reward Notifications ─────────
        // Per-egg coin drops are intentionally NOT routed through this channel — they fire too
        // often and would spam the HUD. Only "unique" events (a bird saved, a chase bonus, a gem
        // drop, level/boss completion, a combo streak) trigger a notification toast.
        public static event Action<RewardNotification> OnRewardNotification;
        public static void FireRewardNotification(RewardNotification n) => OnRewardNotification?.Invoke(n);

        // ───────── Level Self-Clear ─────────
        // Fired after target score is hit and the min-duration gate elapsed. Bird/egg spawning
        // stops, remaining eggs freeze at apex, and the player mops them up at their own pace
        // for normal coin rewards. Ends when the screen clears or the player taps Finish.
        public static event Action OnSelfClearStarted;
        public static event Action OnSelfClearEnded;
        public static event Action OnFinishButtonReady;    // fires after 15s of self-clear
        public static event Action OnPlayerFinishedLevel;  // fires when player taps Finish

        public static void FireSelfClearStarted() => OnSelfClearStarted?.Invoke();
        public static void FireSelfClearEnded() => OnSelfClearEnded?.Invoke();
        public static void FireFinishButtonReady() => OnFinishButtonReady?.Invoke();
        public static void FirePlayerFinishedLevel() => OnPlayerFinishedLevel?.Invoke();
        // ───────── Powerup Cooldown ─────────
        public static event Action<float> OnPowerupCooldownStarted;

        public static void FirePowerupCooldownStarted(float duration)
            => OnPowerupCooldownStarted?.Invoke(duration);
        public static event Action OnPowerupUnequipped;
        public static void FirePowerupUnequipped() => OnPowerupUnequipped?.Invoke();
        public static event Action<int> OnChapterCompleted;  // int = new chapter number
        public static void FireChapterCompleted(int newChapter)
            => OnChapterCompleted?.Invoke(newChapter);

        // Fires once the chapter-end animation has finished playing (or was
        // skipped because references were missing). Listeners can use this to
        // sequence follow-up UI like the chapter-start spin.
        public static event Action OnChapterTransitionFinished;
        public static void FireChapterTransitionFinished()
            => OnChapterTransitionFinished?.Invoke();
    }

    public enum RewardKind
    {
        BirdSaved,       // killed a bird before it laid — prevented an egg
        ChaseBonus,      // killed a bird after it laid, during its flee
        AttackerDown,    // destroyed an attacking bird
        GemDrop,         // rare gem dropped from an egg
        ComboStreak,     // consecutive bird preventions (skill flex)
        FirstClear,      // first-time clear of the level
        BossDefeated,    // boss bird killed
        LevelComplete,   // level completion coin bonus
        PowerRestored,   // power refund on completion
    }

    public readonly struct RewardNotification
    {
        public readonly RewardKind kind;
        public readonly string headline;   // e.g. "Bird Saved", "Combo x3"
        public readonly int amount;        // coins/gems/power amount (0 if non-numeric)
        public readonly string currency;   // "coins" | "gems" | "power" | ""

        public RewardNotification(RewardKind kind, string headline, int amount = 0, string currency = "coins")
        {
            this.kind = kind;
            this.headline = headline;
            this.amount = amount;
            this.currency = currency;
        }
    }
}