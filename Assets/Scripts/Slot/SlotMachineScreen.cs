//using DG.Tweening;
//using Gameplay.Events;
//using Gameplay.PowerUps;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//namespace Gameplay.UI
//{
//    public class SlotMachineScreen : MonoBehaviour
//    {
//        [SerializeField] private RectTransform rectTransform;
//        [SerializeField] private TextMeshProUGUI levelName;
//        [SerializeField] private TextMeshProUGUI levelDescription;

//        [SerializeField] private CanvasGroup slotCanvas;
//        [SerializeField] private RectTransform slotRect;

//        [SerializeField] private Button spinButton;
//        [SerializeField] private Button confirmButton;

//        private PowerupConfig _powerupConfig;

//        private void OnEnable()
//        {
//            spinButton.onClick.AddListener(Spin);
//            confirmButton.onClick.AddListener(ConfirmSpin);

//            spinButton.interactable = true;
//            confirmButton.interactable = false;

//            spinButton.gameObject.SetActive(true);
//            confirmButton.gameObject.SetActive(false);

//            GameEvents.OnPowerupSelected += config => _powerupConfig = config;
//        }

//        private void OnDisable()
//        {
//            spinButton.onClick.RemoveListener(Spin);
//            confirmButton.onClick.RemoveListener(ConfirmSpin);
//        }

//        public void ShowSlot()
//        {
//            slotCanvas.DOFade(1, 0.2f);
//            slotRect.DOPunchAnchorPos(new Vector2(0, 10), 0.2f, 10, 0.2f).SetEase(Ease.OutElastic);
//        }

//        public void HideSlot()
//        {
//            slotCanvas.DOFade(0, 0.2f);
//            slotRect.DOPunchAnchorPos(new Vector2(0, 10), 0.2f, 10, 0.2f).SetEase(Ease.OutElastic);
//        }

//        private void Spin()
//        {
//            spinButton.interactable = false;
//            confirmButton.interactable = true;
//        }

//        private void ConfirmSpin()
//        {
//            spinButton.interactable = true;
//            confirmButton.interactable = false;

//            GameEvents.FirePowerupSelected(_powerupConfig);
//        }
//    }
//}
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.PowerUps;
using Gameplay.Managers;
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

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI levelName;
        [SerializeField] private TextMeshProUGUI levelDescription;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button confirmButton;

        // ── NO SlotMachineController reference needed ──

        private PowerupConfig _selectedPowerup;
        private List<PowerupConfig> _currentOptions;

        private void OnEnable()
        {
            spinButton.onClick.AddListener(OnSpinClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
            GameEvents.OnPowerupSelected += OnPowerupHighlighted;
            ResetButtons();
        }

        private void OnDisable()
        {
            spinButton.onClick.RemoveListener(OnSpinClicked);
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            GameEvents.OnPowerupSelected -= OnPowerupHighlighted;
        }

        // ───────── show / hide ─────────
        public void ShowSlot(PowerupConfig[] options)
        {
            _currentOptions = options.ToList();
            _selectedPowerup = null;

            ResetButtons();

            gameObject.SetActive(true);
            slotCanvas.alpha = 0f;
            slotCanvas.DOFade(1f, 0.25f);
            slotRect.DOPunchAnchorPos(new Vector2(0, 10), 0.3f, 10, 0.2f)
                    .SetEase(Ease.OutElastic);
        }

        public void HideSlot()
        {
            slotCanvas.DOFade(0f, 0.2f)
                      .OnComplete(() => gameObject.SetActive(false));
        }

        // ───────── Step 1: Player taps SPIN ─────────
        private void OnSpinClicked()
        {
            spinButton.interactable = false;
            confirmButton.gameObject.SetActive(false);

            // Fire event — SlotMachineController listens and spins reels
            GameEvents.FireSpinStarted(_currentOptions);
        }

        // ───────── Step 2: Reel stops → powerup highlighted ─────────
        private void OnPowerupHighlighted(PowerupConfig config)
        {
            _selectedPowerup = config;
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
        }

        // ───────── Step 3: Player taps CONFIRM ─────────
        private void OnConfirmClicked()
        {
            if (_selectedPowerup == null)
            {
                Debug.LogWarning("⚠️ Confirm pressed but no powerup selected.");
                return;
            }

            confirmButton.interactable = false;
            GameProgressManager.Instance.PlayerSelectedPowerup(_selectedPowerup);
            GameEvents.FirePlayerConfirmedSpin();
        }

        private void ResetButtons()
        {
            spinButton.gameObject.SetActive(true);
            spinButton.interactable = true;
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }
    }
}