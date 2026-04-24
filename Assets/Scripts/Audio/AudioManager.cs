using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string PREF_MUSIC = "MusicVolume";   // was "MusicVol"
    private const string PREF_SOUND = "SoundVolume";   // was "SFXVol"
    // ─────────────────────────────────────────────
    // AUDIO SOURCES
    // ─────────────────────────────────────────────
    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // ─────────────────────────────────────────────
    // MUSIC CLIPS
    // ─────────────────────────────────────────────
    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    // Add more music clips here as your game grows

    // ─────────────────────────────────────────────
    // SFX CLIPS — Main Menu
    // ─────────────────────────────────────────────
    [Header("Main Menu SFX")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private AudioClip panelCloseSFX;

    // ─────────────────────────────────────────────
    // SFX CLIPS — Gameplay
    // ─────────────────────────────────────────────
    [Header("Gameplay SFX")]
    [SerializeField] private AudioClip coinCollectSFX;
    //[SerializeField] private AudioClip playerJumpSFX;
    [SerializeField] private AudioClip playerHitSFX;
    [SerializeField] private AudioClip playerDeathSFX;
    [SerializeField] private AudioClip enemyDeathSFX;
    [SerializeField] private AudioClip explosionSFX;
    [SerializeField] private AudioClip powerUpSFX;
    [SerializeField] private AudioClip levelCompleteSFX;
   // [SerializeField] private AudioClip levelFailSFX;
    // Add more gameplay SFX here as needed

    // ─────────────────────────────────────────────
    // VOLUME DEFAULTS (tweak in Inspector or here)
    // ─────────────────────────────────────────────
    [Header("Default Volume Scales")]
    [Range(0f, 1f)][SerializeField] private float musicDefaultVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float sfxDefaultVolume = 1f;

    // Per-clip default scales — easy to tune without touching call sites
    [Range(0f, 1f)][SerializeField] private float buttonClickVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float buttonHoverVolume = 0.4f;
    [Range(0f, 1f)][SerializeField] private float panelOpenVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float panelCloseVolume = 0.5f;
    [Range(0f, 1f)][SerializeField] private float coinCollectVolume = 0.9f;
    [Range(0f, 1f)][SerializeField] private float playerJumpVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float playerHitVolume = 1.0f;
    [Range(0f, 1f)][SerializeField] private float playerDeathVolume = 1.0f;
    [Range(0f, 1f)][SerializeField] private float enemyDeathVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float explosionVolume = 1.0f;
    [Range(0f, 1f)][SerializeField] private float powerUpVolume = 0.85f;
    [Range(0f, 1f)][SerializeField] private float levelCompleteVolume = 1.0f;
    [Range(0f, 1f)][SerializeField] private float levelFailVolume = 0.9f;

    // ─────────────────────────────────────────────
    // MUTE STATE
    // ─────────────────────────────────────────────
    private bool isMusicMuted = false;
    private bool isSFXMuted = false;

    // ─────────────────────────────────────────────
    // SINGLETON SETUP
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    // ══════════════════════════════════════════════
    // MUSIC — PUBLIC API
    // ══════════════════════════════════════════════

    /// <summary>Play any music clip. Skips restart if already playing the same clip.</summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>Crossfade to a new music clip over a given duration.</summary>
    public void CrossfadeMusic(AudioClip newClip, float duration = 1f)
    {
        if (newClip == null) return;
        StartCoroutine(CrossfadeRoutine(newClip, duration));
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    public void SetMusicVolume(float vol)
    {
        musicSource.volume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("MusicVol", musicSource.volume);
    }

    public void ToggleMusicMute()
    {
        isMusicMuted = !isMusicMuted;
        musicSource.mute = isMusicMuted;
        PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
    }

    // ─────── Scene-specific music shortcuts ───────

    public void PlayMainMenuMusic() => PlayMusic(mainMenuMusic);
    public void PlayGameplayMusic() => CrossfadeMusic(gameplayMusic, 1.5f);
    // Add more scenes: public void PlayBossMusic() => CrossfadeMusic(bossMusic, 0.8f);

    // ══════════════════════════════════════════════
    // SFX — PUBLIC API
    // ══════════════════════════════════════════════

    /// <summary>Play any SFX clip with an optional volume scale override.</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || isSFXMuted) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void SetSFXVolume(float vol)
    {
        sfxSource.volume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("SFXVol", sfxSource.volume);
    }

    public void ToggleSFXMute()
    {
        isSFXMuted = !isSFXMuted;
        PlayerPrefs.SetInt("SFXMuted", isSFXMuted ? 1 : 0);
    }

    // ─────── Main Menu SFX shortcuts ───────

    public void PlayButtonClick() => PlaySFX(buttonClickSFX, buttonClickVolume);
    public void PlayButtonHover() => PlaySFX(buttonHoverSFX, buttonHoverVolume);
    public void PlayPanelOpen() => PlaySFX(panelOpenSFX, panelOpenVolume);
    public void PlayPanelClose() => PlaySFX(panelCloseSFX, panelCloseVolume);

    // ─────── Gameplay SFX shortcuts ───────

    public void PlayCoinCollect() => PlaySFX(coinCollectSFX, coinCollectVolume);
    //public void PlayPlayerJump() => PlaySFX(playerJumpSFX, playerJumpVolume);
    public void PlayPlayerHit() => PlaySFX(playerHitSFX, playerHitVolume);
    public void PlayPlayerDeath() => PlaySFX(playerDeathSFX, playerDeathVolume);
    public void PlayEnemyDeath() => PlaySFX(enemyDeathSFX, enemyDeathVolume);
    public void PlayExplosion() => PlaySFX(explosionSFX, explosionVolume);
    public void PlayPowerUp() => PlaySFX(powerUpSFX, powerUpVolume);
    public void PlayLevelComplete() => PlaySFX(levelCompleteSFX, levelCompleteVolume);
    //public void PlayLevelFail() => PlaySFX(levelFailSFX, levelFailVolume);

    // ══════════════════════════════════════════════
    // SETTINGS PERSISTENCE
    // ══════════════════════════════════════════════

    private void LoadSettings()
    {
        musicSource.volume = PlayerPrefs.GetFloat(PREF_MUSIC, musicDefaultVolume);
        sfxSource.volume = PlayerPrefs.GetFloat(PREF_SOUND, sfxDefaultVolume);
        isMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        musicSource.mute = isMusicMuted;
    }

    // ══════════════════════════════════════════════
    // INTERNAL HELPERS
    // ══════════════════════════════════════════════

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        float startVolume = musicSource.volume;

        // Fade out
        float timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2f));
            yield return null;
        }

        // Swap clip
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        // Fade in
        timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVolume, timer / (duration / 2f));
            yield return null;
        }

        musicSource.volume = startVolume;
    }
}