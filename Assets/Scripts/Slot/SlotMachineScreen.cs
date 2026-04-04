using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Gameplay.UI
{
    public class SlotMachineScreen : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private CanvasGroup slotCanvas;
        [SerializeField] private RectTransform slotRect;
        [Header("Auto-Spin Settings")]
        [SerializeField] private float autoSpinDelay = 0.5f;
        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        private PowerupConfig _selectedPowerup;
        private List<PowerupConfig> _currentOptions;
        private Coroutine _autoSpinCoroutine;
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
            if (_autoSpinCoroutine != null)
            {
                StopCoroutine(_autoSpinCoroutine);
                _autoSpinCoroutine = null;
            }
        }
        // ───────── Show Slot ─────────
        public void ShowSlot(PowerupConfig[] options)
        {
            _currentOptions = options.ToList();
            _selectedPowerup = null;
            ResetButtons();
            gameObject.SetActive(true);
            slotCanvas.alpha = 0f;
            slotCanvas.DOFade(1f, 0.25f);
            slotRect.DOPunchAnchorPos(new Vector2(0, 10), 0.3f, 10, 0.2f)
                    .SetEase(Ease.OutElastic)
                    .OnComplete(() =>
                    {
                        _autoSpinCoroutine = StartCoroutine(AutoSpinAfterDelay());
                    });
        }
        // ───────── Hide Slot ─────────
        public void HideSlot()
        {
            if (_autoSpinCoroutine != null)
            {
                StopCoroutine(_autoSpinCoroutine);
                _autoSpinCoroutine = null;
            }
            slotCanvas.DOFade(0f, 0.2f)
                      .OnComplete(() => gameObject.SetActive(false));
        }
        // ───────── Auto-Spin After Delay ─────────
        private IEnumerator AutoSpinAfterDelay()
        {
            yield return new WaitForSeconds(autoSpinDelay);
            GameEvents.FireSpinStarted(_currentOptions);
            _autoSpinCoroutine = null;
        }
        private void OnPowerupCommitted(PowerupConfig committedPowerup)
        {
            _selectedPowerup = committedPowerup;
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
        }
        // ───────── Step 2: Player taps CONFIRM ─────────
        private void OnConfirmClicked()
        {
            if (_selectedPowerup == null)
            {
                Debug.LogWarning("Confirm pressed but no powerup selected.");
                return;
            }
            confirmButton.interactable = false;
            // FIX: Do NOT call GameProgressManager.Instance.PlayerSelectedPowerup here.
            // SlotMachineController.CommitSelection is the single place that equips
            //// the powerup, triggered by FirePlayerConfirmedSpin below.
            GameEvents.FirePowerupSelected(_selectedPowerup);
            HideSlot();
            ///
        }
        private void ResetButtons()
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }
    }
}