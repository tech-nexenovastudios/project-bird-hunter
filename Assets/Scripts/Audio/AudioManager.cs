using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Source-of-truth for on/off — same keys SettingPanelController writes, so
    // boot honors the saved toggle even when the settings panel object is inactive.
    private const string PREF_MUSIC_ENABLED = "MusicEnabled";
    private const string PREF_SOUND_ENABLED = "SoundEnabled";
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
    [SerializeField] private AudioClip buttonClickPanelOpenSFX;
    [SerializeField] private AudioClip buttonHoverSFX;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private AudioClip panelCloseSFX;
    [SerializeField] private AudioClip upgradeSFX;
    [SerializeField] private AudioClip uiSuccessSFX;
    [SerializeField] private AudioClip uiFailSFX;

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
    [Range(0f, 1f)][SerializeField] private float upgradeVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float uiSuccessVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float uiFailVolume = 1f;
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

    // Stash so callers (e.g. boss-music handler) can temporarily swap to a new clip
    // and restore the previous one when done.
    private AudioClip _stashedClip;
    private bool _hasStashedClip;

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
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayMusic called with null clip — no-op.");
            return;
        }
        if (musicSource == null)
        {
            Debug.LogWarning("[AudioManager] musicSource is not assigned in the inspector.");
            return;
        }
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        // Cancel any in-flight crossfade so it doesn't overwrite volume mid-play.
        StopAllCoroutines();
        musicSource.volume = PlayerPrefs.GetInt(PREF_MUSIC_ENABLED, 1) == 1 ? musicDefaultVolume : 0f;
        musicSource.Stop();
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

    /// <summary>
    /// Save the currently-playing clip, then play a new one. Use PopMusic() to restore.
    /// Used for boss-music intervals where we return to gameplay music when the boss leaves.
    /// </summary>
    public void PushMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        _stashedClip = musicSource.clip;
        _hasStashedClip = _stashedClip != null;
        PlayMusic(clip);
    }

    /// <summary>Restore the clip saved by PushMusic. No-op if nothing was stashed.</summary>
    public void PopMusic()
    {
        if (!_hasStashedClip || _stashedClip == null)
        {
            _hasStashedClip = false;
            _stashedClip = null;
            return;
        }
        var toRestore = _stashedClip;
        _stashedClip = null;
        _hasStashedClip = false;
        PlayMusic(toRestore);
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    // Temporarily dip the music volume so a transient SFX (toast, celebration, finish-now)
    // reads cleanly without fighting the music bed. Volume returns automatically.
    public void DuckMusic(float duration, float dipFactor = 0.4f)
    {
        if (musicSource == null) return;
        StartCoroutine(DuckMusicRoutine(duration, Mathf.Clamp01(dipFactor)));
    }

    private Coroutine _duckRoutine;
    private IEnumerator DuckMusicRoutine(float duration, float dipFactor)
    {
        if (_duckRoutine != null) StopCoroutine(_duckRoutine);

        float originalVolume = PlayerPrefs.GetFloat(PREF_MUSIC, musicDefaultVolume);
        float duckedVolume = originalVolume * dipFactor;
        const float fadeTime = 0.15f;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(originalVolume, duckedVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = duckedVolume;

        float hold = Mathf.Max(0f, duration - fadeTime * 2f);
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(duckedVolume, originalVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = originalVolume;
        _duckRoutine = null;
    }

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
    // Use deterministic stop+play instead of crossfade so the main-menu clip
    // reliably stops when we enter gameplay (crossfade can fail silently if
    // the routine gets interrupted).
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic);
    // Add more scenes: public void PlayBossMusic() => PlayMusic(bossMusic);

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
    public void PlayButtonClickPanelOpen(float volumeScale = 1f) => PlaySFX(buttonClickPanelOpenSFX, volumeScale);
    public void PlayUpgradeSound() => PlaySFX(upgradeSFX, upgradeVolume);
    public void PlayUISuccess() => PlaySFX(uiSuccessSFX, uiSuccessVolume);
    public void PlayUIFail() => PlaySFX(uiFailSFX, uiFailVolume);

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
        bool musicEnabled = PlayerPrefs.GetInt(PREF_MUSIC_ENABLED, 1) == 1;
        bool soundEnabled = PlayerPrefs.GetInt(PREF_SOUND_ENABLED, 1) == 1;
        // Volume=0 is our only silencing mechanism — don't also flip AudioSource.mute
        // or isSFXMuted here, because SetMusicVolume/SetSFXVolume won't clear those
        // when the user toggles audio back on, leaving the source permanently silenced.
        musicSource.volume = musicEnabled ? musicDefaultVolume : 0f;
        sfxSource.volume = soundEnabled ? sfxDefaultVolume : 0f;
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
