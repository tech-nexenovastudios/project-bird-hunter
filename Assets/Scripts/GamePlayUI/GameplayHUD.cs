using DG.Tweening;
using Gameplay.Events;
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

        private bool _isPaused = false;

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

            // Re-hydrate the pause panel's sliders/vibration knob from the latest
            // AudioManager + PlayerPrefs values. Without this they'd be stuck at
            // whatever they were when PausePanel.Start() first ran.
            Gameplay.UI.PausePanel.Instance?.RefreshSettings();

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