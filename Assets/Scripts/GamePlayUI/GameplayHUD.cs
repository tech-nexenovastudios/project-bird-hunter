using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("HUD Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button continueButton;

        [Header("Self-Clear Finish")]
        [Tooltip("Appears 15s into self-clear so a stuck player can voluntarily end the level (forgoing remaining egg coins).")]
        [SerializeField] private Button finishButton;

        [Header("Reward Notification")]
        [Tooltip("Toast text for unique rewards (bird saved, combo, level complete). Per-egg drops do NOT route here.")]
        [SerializeField] private TextMeshProUGUI rewardText;
        [Tooltip("How long the toast stays fully visible before fading out, in seconds.")]
        [SerializeField] private float rewardHoldDuration = 1.0f;
        [Tooltip("Fade in/out duration, in seconds.")]
        [SerializeField] private float rewardFadeDuration = 0.18f;

        private readonly Queue<RewardNotification> _rewardQueue = new();
        private Coroutine _rewardRoutine;
        private bool _isPaused = false;

        // Color palette per reward kind — gives each notification a quick visual signature so
        // the player can read the type without parsing text. Tuned for legibility against the HUD.
        private static readonly Dictionary<RewardKind, Color> RewardColors = new()
        {
            { RewardKind.BirdSaved,     new Color(0.55f, 0.95f, 0.55f) }, // green — prevention
            { RewardKind.ChaseBonus,    new Color(0.95f, 0.78f, 0.30f) }, // amber — chase
            { RewardKind.AttackerDown,  new Color(0.95f, 0.40f, 0.40f) }, // red — threat down
            { RewardKind.GemDrop,       new Color(0.55f, 0.75f, 0.95f) }, // blue — gem
            { RewardKind.ComboStreak,   new Color(1.00f, 0.55f, 1.00f) }, // magenta — combo flex
            { RewardKind.FirstClear,    new Color(0.95f, 0.95f, 0.55f) }, // yellow — milestone
            { RewardKind.BossDefeated,  new Color(1.00f, 0.30f, 0.30f) }, // red — boss
            { RewardKind.LevelComplete, new Color(0.85f, 0.95f, 0.85f) }, // pale — completion
            { RewardKind.PowerRestored, new Color(0.70f, 0.55f, 0.95f) }, // violet — power
        };

        // ───────── Lifecycle ─────────
        private void OnEnable()
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
            continueButton.onClick.AddListener(OnContinueClicked);

            if (finishButton != null)
            {
                finishButton.onClick.AddListener(OnFinishClicked);
                finishButton.gameObject.SetActive(false);
            }
            GameEvents.OnSelfClearStarted += OnSelfClearStarted;
            GameEvents.OnSelfClearEnded += OnSelfClearEnded;
            GameEvents.OnFinishButtonReady += OnFinishButtonReady;
            GameEvents.OnRewardNotification += OnRewardNotification;

            if (rewardText != null)
            {
                var c = rewardText.color; c.a = 0f; rewardText.color = c;
                rewardText.transform.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            pauseButton.onClick.RemoveListener(OnPauseClicked);
            continueButton.onClick.RemoveListener(OnContinueClicked);

            if (finishButton != null)
                finishButton.onClick.RemoveListener(OnFinishClicked);
            GameEvents.OnSelfClearStarted -= OnSelfClearStarted;
            GameEvents.OnSelfClearEnded -= OnSelfClearEnded;
            GameEvents.OnFinishButtonReady -= OnFinishButtonReady;
            GameEvents.OnRewardNotification -= OnRewardNotification;
        }

        // ───────── Reward Toasts ─────────
        // Notifications are queued so a fast burst (combo + bird-saved firing within the same
        // frame) plays back sequentially instead of stacking on top of each other. A single
        // coroutine drains the queue; new notifications during playback just append.
        private void OnRewardNotification(RewardNotification n)
        {
            if (rewardText == null) return;
            _rewardQueue.Enqueue(n);
            if (_rewardRoutine == null)
                _rewardRoutine = StartCoroutine(DrainRewardQueue());
        }

        private IEnumerator DrainRewardQueue()
        {
            while (_rewardQueue.Count > 0)
            {
                var n = _rewardQueue.Dequeue();
                yield return PlayRewardToast(n);
            }
            _rewardRoutine = null;
        }

        private IEnumerator PlayRewardToast(RewardNotification n)
        {
            string amountText = n.amount > 0 ? $"  +{n.amount} {n.currency}" : string.Empty;
            rewardText.text = $"{n.headline}{amountText}";
            rewardText.color = RewardColors.TryGetValue(n.kind, out var c)
                ? new Color(c.r, c.g, c.b, 0f)
                : new Color(1f, 1f, 1f, 0f);

            // Scale + fade in, hold, fade out. Uses unscaled time so toasts still play during
            // brief gameplay pauses (e.g. level-complete moment with timeScale dip).
            rewardText.transform.localScale = Vector3.one * 0.7f;
            rewardText.DOFade(1f, rewardFadeDuration).SetUpdate(true);
            rewardText.transform.DOScale(1f, rewardFadeDuration).SetEase(Ease.OutBack).SetUpdate(true);

            yield return new WaitForSecondsRealtime(rewardFadeDuration + rewardHoldDuration);

            rewardText.DOFade(0f, rewardFadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(rewardFadeDuration);
        }

        // ───────── Self-Clear Finish ─────────
        private void OnSelfClearStarted()
        {
            // Hide stale button from a previous level — it shouldn't carry over.
            if (finishButton != null) finishButton.gameObject.SetActive(false);
        }

        private void OnFinishButtonReady()
        {
            if (finishButton == null) return;
            finishButton.gameObject.SetActive(true);
            finishButton.transform.localScale = Vector3.zero;
            finishButton.transform.DOPunchScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void OnSelfClearEnded()
        {
            if (finishButton != null) finishButton.gameObject.SetActive(false);
        }

        private void OnFinishClicked()
        {
            if (Gameplay.SpawnController.Instance != null)
                Gameplay.SpawnController.Instance.RequestPlayerFinish();
            if (finishButton != null) finishButton.gameObject.SetActive(false);
        }

        // ───────── Pause ─────────
        private void OnPauseClicked()
        {
            if (_isPaused) return;
            PauseGame();
        }

        private void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            pausePanel.transform
                .DOScale(Vector3.one, 0.25f)
                .From(Vector3.zero)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            GameEvents.FirePauseToggled(true);
        }

        // ───────── Continue ─────────
        private void OnContinueClicked()
        {
            if (!_isPaused) return;
            ResumeGame();
        }

        /// <summary>
        /// Public method — assign this to any Button's OnClick or
        /// a background Panel's OnClick via the Inspector.
        /// Safe to call even if the game is not currently paused.
        /// </summary>
        public void OnResumeClicked()
        {
            if (!_isPaused) return;
            ResumeGame();
        }

        private void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            pausePanel.transform
                .DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() => pausePanel.SetActive(false));
            GameEvents.FirePauseToggled(false);
        }
    }
}