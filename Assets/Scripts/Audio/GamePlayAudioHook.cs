using UnityEngine;
using Gameplay.Events;

public class GameplayAudioHook : MonoBehaviour
{
    private void OnEnable()
    {
        // Music
        GameEvents.OnGameLevelUpdated += HandleLevelUpdated;

        // Combat SFX
        GameEvents.OnEggHit += HandleEggHit;
        GameEvents.OnEggDestroyed += HandleEggDestroyed;
        GameEvents.OnBirdHit += HandleBirdHit;
        GameEvents.OnBirdDestroyed += HandleBirdDestroyed;
        GameEvents.OnCannonHit += HandleCannonHit;

        // Flow SFX
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
        GameEvents.OnPlayerCoinsUpdated += HandleCoinCollect;
        GameEvents.OnPowerupCommitted += HandlePowerupCommitted;

        // Pause
        GameEvents.OnPauseToggled += HandlePauseToggled;
    }

    private void OnDisable()
    {
        GameEvents.OnGameLevelUpdated -= HandleLevelUpdated;
        GameEvents.OnEggHit -= HandleEggHit;
        GameEvents.OnEggDestroyed -= HandleEggDestroyed;
        GameEvents.OnBirdHit -= HandleBirdHit;
        GameEvents.OnBirdDestroyed -= HandleBirdDestroyed;
        GameEvents.OnCannonHit -= HandleCannonHit;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnPlayerCoinsUpdated -= HandleCoinCollect;
        GameEvents.OnPowerupCommitted -= HandlePowerupCommitted;
        GameEvents.OnPauseToggled -= HandlePauseToggled;
    }

    // ── Music ────────────────────────────────────────────
    private void Start()
    {
        AudioManager.Instance?.PlayGameplayMusic();
    }

    private void HandleLevelUpdated(int index)
    {
        // Optionally swap music per level here, e.g. boss levels
        //AudioManager.Instance?.CrossfadeMusic(bossClip, 1f);
    }

    // ── Combat SFX ───────────────────────────────────────
    private void HandleEggHit(Gameplay.Interfaces.IDamageable egg, int dmg, Vector3 pos)
        => AudioManager.Instance?.PlayPlayerHit();   // reuse hit SFX for egg impact

    private void HandleEggDestroyed(Gameplay.Interfaces.IDamageable egg, int score, Vector3 pos)
        => AudioManager.Instance?.PlayExplosion();

    private void HandleBirdHit(Gameplay.Interfaces.IDamageable bird, int dmg, Vector3 pos)
        => AudioManager.Instance?.PlayPlayerHit();

    private void HandleBirdDestroyed(Gameplay.Interfaces.IDamageable bird, int score, Vector3 pos)
        => AudioManager.Instance?.PlayEnemyDeath();

    private void HandleCannonHit(int damage)
        => AudioManager.Instance?.PlayPlayerHit();

    // ── Flow SFX ─────────────────────────────────────────
    private void HandleLevelCompleted(int score)
        => AudioManager.Instance?.PlayLevelComplete();

    private void HandlePlayerDeath()
        => AudioManager.Instance?.PlayPlayerDeath();

    private void HandleCoinCollect(int total)
        => AudioManager.Instance?.PlayCoinCollect();

    private void HandlePowerupCommitted(Gameplay.PowerUps.PowerupConfig config)
        => AudioManager.Instance?.PlayPowerUp();

    // ── Pause ─────────────────────────────────────────────
    private void HandlePauseToggled(bool isPaused)
    {
        if (isPaused) AudioManager.Instance?.PauseMusic();
        else AudioManager.Instance?.ResumeMusic();
    }
}