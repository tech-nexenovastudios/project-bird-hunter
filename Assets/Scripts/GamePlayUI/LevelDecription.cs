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
    /// No new events added.
    /// </summary>
    public class LevelDetailPopup : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup popupCanvasGroup;
        [SerializeField] private GameObject popupPanel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI levelTitleText;   // "Level 3" / "Level 3 Complete!"
        [SerializeField] private TextMeshProUGUI countdownText;    // "Starting in 3..." / "Next level in 3..."

        [Header("Timing")]
        [SerializeField] private int countdownFrom = 3;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.25f;

        private Coroutine _activeRoutine;
        private int _currentLevel;

        // ───────── Lifecycle ─────────

        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnGameLevelUpdated;
            // Post-level "Level X Complete!" popup is intentionally skipped — the next-level
            // popup follows immediately and already shows "Level N+1 / Starting in 3..."; the
            // duplicate countdown was a 4.5s dead beat between levels.
            // Grace-time popup removed — replaced by the self-clear freeze (no UI countdown).
            GameEvents.OnChapterCompleted += OnChapterCompletedHandler;
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnGameLevelUpdated;
            GameEvents.OnChapterCompleted -= OnChapterCompletedHandler;
        }

        // The L20 "Starting in N..." countdown can still be running when the
        // chapter rolls over. Stop it so it doesn't overlap the chapter-end
        // animation; the next popup will appear with the new chapter's L1.
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
            _currentLevel = GameProgressManager.Instance.CurrentLevel;
            ShowPopup(
                title: $"Level {_currentLevel}",
                countdownPrefix: "Starting in"
            );
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
            // ── Setup ──
            levelTitleText.text = title;
            countdownText.text = string.Empty;

            popupPanel.SetActive(true);
            popupCanvasGroup.alpha = 0f;

            // ── Animate title in ──
            levelTitleText.transform.localScale = Vector3.one * 0.5f;
            levelTitleText.alpha = 0f;

            Sequence titleSeq = DOTween.Sequence();
            titleSeq.Append(popupCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic));
            titleSeq.Join(levelTitleText.transform.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack));
            titleSeq.Join(levelTitleText.DOFade(1f, 0.25f));
            titleSeq.Append(levelTitleText.transform.DOScale(1f, 0.15f).SetEase(Ease.InOutSine));
            titleSeq.SetUpdate(true);

            yield return titleSeq.WaitForCompletion();

            // ── Countdown ──
            // Only the level-START path gets the 3-2-1-Go SFX; the post-level "Next level in N"
            // popup ends in "Loading..." and uses no audio cue.
            bool isLevelStart = !countdownPrefix.Contains("Next");

            int count = countdownFrom;
            while (count > 0)
            {
                countdownText.text = $"{countdownPrefix} {count}...";
                PulseCountdown();
                if (isLevelStart) GameEvents.FireLevelCountdownTick();
                yield return new WaitForSecondsRealtime(1f);
                count--;
            }

            // ── Final beat ──
            string finalWord = isLevelStart ? "Go!" : "Loading...";
            countdownText.text = finalWord;
            PulseCountdown();
            if (isLevelStart) GameEvents.FireLevelCountdownGo();
            yield return new WaitForSecondsRealtime(0.6f);

            // ── Fade out ──
            Sequence fadeOut = DOTween.Sequence();
            fadeOut.Append(popupCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InCubic));
            fadeOut.Join(levelTitleText.transform.DOScale(0.85f, fadeOutDuration).SetEase(Ease.InBack));
            fadeOut.SetUpdate(true);
            fadeOut.OnComplete(HideInstant);

            yield return fadeOut.WaitForCompletion();
        }

        // ───────── Helpers ─────────

        private void PulseCountdown()
        {
            if (countdownText == null) return;
            countdownText.DOKill();
            countdownText.transform.localScale = Vector3.one;
            countdownText.transform
                .DOPunchScale(Vector3.one * 0.25f, 0.3f, 5, 0.5f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void HideInstant()
        {
            popupPanel.SetActive(false);
            popupCanvasGroup.alpha = 0f;
        }
    }
}
