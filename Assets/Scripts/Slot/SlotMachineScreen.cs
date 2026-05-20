using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    /// <summary>
    /// Visual shell for the powerup slot machine: handles show/hide animations and the
    /// Confirm button state. Spin orchestration and reel logic live in SlotMachineController.
    /// </summary>
    public class SlotMachineScreen : MonoBehaviour
    {
        // ── Animation tuning ────────────────────────────────────────────────
        private const float FadeInDuration  = 0.25f;
        private const float FadeOutDuration = 0.20f;
        private const float PunchDuration   = 0.30f;
        private const float PunchElasticity = 0.20f;
        private const int   PunchVibrato    = 10;
        private static readonly Vector2 PunchStrength = new(0f, 10f);

        [Header("Animation")]
        [SerializeField] private CanvasGroup slotCanvas;
        [SerializeField] private RectTransform slotRect;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;

        private PowerupConfig _selectedPowerup;
        private List<PowerupConfig> _currentOptions;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            GameEvents.OnPowerupCommitted += OnPowerupCommitted;
            ResetButtons();
        }

        private void OnDisable()
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            GameEvents.OnPowerupCommitted -= OnPowerupCommitted;
        }

        // ─── Show / Hide ────────────────────────────────────────────────────

        public void ShowSlot(PowerupConfig[] options)
        {
            if(slotCanvas is null) slotRect.TryGetComponent(out slotCanvas);
            
            _currentOptions = options.ToList();
            _selectedPowerup = null;
            ResetButtons();

            gameObject.SetActive(true);
            slotCanvas.alpha = 0f;
            slotCanvas.DOFade(1f, FadeInDuration);
            slotRect.DOPunchAnchorPos(PunchStrength, PunchDuration, PunchVibrato, PunchElasticity)
                    .SetEase(Ease.OutElastic);

            // Reels start spinning the moment the panel opens — fade and punch run in parallel.
            GameEvents.FireSpinStarted(_currentOptions);
        }

        public void HideSlot()
        {
            slotCanvas.DOFade(0f, FadeOutDuration)
                      .OnComplete(() => gameObject.SetActive(false));
        }

        // ─── Selection wiring ───────────────────────────────────────────────

        private void OnPowerupCommitted(PowerupConfig committedPowerup)
        {
            _selectedPowerup = committedPowerup;
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
        }

        private void OnConfirmClicked()
        {
            if (_selectedPowerup == null)
            {
                Debug.LogWarning("[SlotMachineScreen] Confirm pressed but no powerup selected.");
                return;
            }

            confirmButton.interactable = false;
            // Final commit signal — SlotMachineController.CommitSelection (wired via the
            // Confirm button's UnityEvent) equips the powerup; this event lets other
            // systems (audio, analytics, etc.) react to the same beat.
            GameEvents.FirePowerupSelected(_selectedPowerup);
            HideSlot();
        }

        private void ResetButtons()
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }
    }
}
