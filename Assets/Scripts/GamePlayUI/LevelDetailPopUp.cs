using System.Collections;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using TMPro;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    /// Standalone popup that appears:
    ///   • On level START  → "Level 3"       + countdown ("Starting in 3…2…1…Go!")
    ///   • On level COMPLETE → "Level 3 Complete!" + countdown ("Next level in 3…2…1…")
    ///
    /// Listens to:  GameEvents.OnGameLevelUpdated  (level start)
    ///              GameEvents.OnLevelCompleted     (level complete)

    public class LevelDetailPopUp : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup popupCanvasGroup;
        [SerializeField] private GameObject popupPanel;

        [Header("Texts")]
        // Single text: first shows "Level 3" for titleHoldDuration, then counts
        // down "Starting in 3 / 2 / 1" (one per second) in the SAME text.
        [SerializeField] private TextMeshProUGUI levelTitleText;

        [Header("Timing")]
        [SerializeField] private int countdownFrom = 3;
        [SerializeField] private float titleHoldDuration = 0.5f;   // how long "Level N" stays before the countdown
        [Tooltip("Pause between the 'Level Complete' popup and the next-level 'Starting in' countdown. Level-start only; doesn't delay the smoke/level-start VFX, which fires when this popup appears.")]
        [SerializeField] private float nextLevelCountdownDelay = 1.5f;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        public ParticleSystem levelStartSFX;
        private Coroutine _activeRoutine;
        private int _currentLevel;

        // True only after OnLevelCompleted fires — reset on every new level start.
        // Prevents "Level X Complete!" showing again if the player restarts mid-session.
        private bool _levelCompleted = false;

        // ───────── Lifecycle ─────────

        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnGameLevelUpdated;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnChapterCompleted += OnChapterCompletedHandler;
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnGameLevelUpdated;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnChapterCompleted -= OnChapterCompletedHandler;
        }

        // Stop the in-flight "Starting in N..." popup when the chapter rolls
        // over so it doesn't overlap the chapter-end animation.
        private void OnChapterCompletedHandler(int newChapterNumber)
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
            if (levelTitleText != null) levelTitleText.transform.DOKill();
            if (levelTitleText != null) levelTitleText.DOKill();
            if (popupCanvasGroup != null) popupCanvasGroup.DOKill();
            HideInstant();
        }

        private void Awake()
        {
            HideInstant();
        }

        // ───────── Event Handlers ─────────

        private void OnGameLevelUpdated(int levelIndex)
        {
            _levelCompleted = false;  // reset — this is a fresh level start or restart
            _currentLevel = GameProgressManager.Instance.CurrentLevel;
            ShowPopup(
                title: $"Level {_currentLevel}",
                countdownPrefix: "Starting in"
            );
            if (_currentLevel != 1)
            {
                levelStartSFX.Play();
            }
        }

        private void OnLevelCompleted(int score)
        {
            // Guard: only show complete popup if the level actually ran this session.
            // Prevents firing again when a restart triggers OnGameLevelUpdated.
            if (_levelCompleted) return;
            _levelCompleted = true;

            ShowPopup(
                title: $"Level {_currentLevel} Complete!",
                countdownPrefix: "Next level in"
            );
            if(_currentLevel !=1 )
            {
                levelStartSFX.Play();
            }
        }

        // ───────── Core Display ─────────

        private void ShowPopup(string title, string countdownPrefix)
        {
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            _activeRoutine = StartCoroutine(PopupRoutine(title, countdownPrefix));
        }

        private IEnumerator PopupRoutine(string title, string countdownPrefix)
        {
            // Only the level-START path gets the 3-2-1-Go SFX; the post-level
            // "Next level in N" popup uses no audio cue.
            bool isLevelStart = !countdownPrefix.Contains("Next");

            // ── Phase 1: show the title ("Level N") for titleHoldDuration ──
            levelTitleText.text = title;

            popupPanel.SetActive(true);
            popupCanvasGroup.alpha = 0f;

            levelTitleText.transform.localScale = Vector3.one * 0.5f;
            levelTitleText.alpha = 0f;

            Sequence titleSeq = DOTween.Sequence();
            titleSeq.Append(popupCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic));
            titleSeq.Join(levelTitleText.transform.DOScale(1f, fadeInDuration).SetEase(Ease.OutBack));
            titleSeq.Join(levelTitleText.DOFade(1f, fadeInDuration));
            titleSeq.SetUpdate(true);
            yield return titleSeq.WaitForCompletion();

            // Hold the remainder so "Level N" is on screen for the full titleHoldDuration.
            float remainingHold = titleHoldDuration - fadeInDuration;
            if (remainingHold > 0f) yield return WaitPausableRealtime(remainingHold);

            // 1.5s beat between the "Level Complete" popup and the next-level "Starting in"
            // countdown. Level-start path only — the popup (and any smoke/level-start VFX fired
            // when it appeared) is already on screen, so this delays only the countdown.
            if (isLevelStart && nextLevelCountdownDelay > 0f)
                yield return WaitPausableRealtime(nextLevelCountdownDelay);

            // ── Phase 2: countdown in the SAME text ("Starting in 3 / 2 / 1") ──
            int count = countdownFrom;
            while (count > 0)
            {
                levelTitleText.text = $"{countdownPrefix} {count}";
                PulseTitle();
                if (isLevelStart) GameEvents.FireLevelCountdownTick();
                yield return WaitPausableRealtime(1f);
                count--;
            }

            if (isLevelStart) GameEvents.FireLevelCountdownGo();

            // ── Fade out ──
            Sequence fadeOut = DOTween.Sequence();
            fadeOut.Append(popupCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InCubic));
            fadeOut.Join(levelTitleText.transform.DOScale(0.85f, fadeOutDuration).SetEase(Ease.InBack));
            fadeOut.SetUpdate(true);
            fadeOut.OnComplete(HideInstant);

            yield return fadeOut.WaitForCompletion();
        }

        // ───────── Helpers ─────────

        private void PulseTitle()
        {
            if (levelTitleText == null) return;
            levelTitleText.transform.DOKill();
            levelTitleText.transform.localScale = Vector3.one;
            levelTitleText.transform
                .DOPunchScale(Vector3.one * 0.25f, 0.3f, 5, 0.5f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void HideInstant()
        {
            popupPanel.SetActive(false);
            popupCanvasGroup.alpha = 0f;
        }

        // Unscaled wait that freezes while the game is paused. We can't use
        // WaitForSeconds (gets stuck forever at timeScale=0) or WaitForSecondsRealtime
        // (keeps ticking through pause, firing fresh countdown SFX). Pause is signalled
        // by Time.timeScale == 0 via GameplayHUD.PauseGame.
        private IEnumerator WaitPausableRealtime(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (Time.timeScale > 0f)
                    elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}

