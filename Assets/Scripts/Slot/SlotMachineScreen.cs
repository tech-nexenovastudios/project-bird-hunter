using DG.Tweening;
using Gameplay.Events;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class SlotMachineScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI levelName;
        [SerializeField] private TextMeshProUGUI levelDescription;
        
        [SerializeField] private CanvasGroup slotCanvas;
        [SerializeField] private RectTransform slotRect;
        
        [SerializeField] private Button spinButton;
        [SerializeField] private Button confirmButton;

        private PowerupConfig _powerupConfig;
        
        private void OnEnable()
        {
            spinButton.onClick.AddListener(Spin);
            confirmButton.onClick.AddListener(ConfirmSpin);
            
            spinButton.interactable = true;
            confirmButton.interactable = false;
            
            spinButton.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(false);
            
            GameEvents.OnPowerupSelected += config => _powerupConfig = config;
        }
        
        private void OnDisable()
        {
            spinButton.onClick.RemoveListener(Spin);
            confirmButton.onClick.RemoveListener(ConfirmSpin);
        }

        public void ShowSlot()
        {
            slotCanvas.DOFade(1, 0.2f);
            slotRect.DOPunchAnchorPos(new Vector2(0, 10), 0.2f, 10, 0.2f).SetEase(Ease.OutElastic);
        }
        
        public void HideSlot()
        {
            slotCanvas.DOFade(0, 0.2f);
            slotRect.DOPunchAnchorPos(new Vector2(0, 10), 0.2f, 10, 0.2f).SetEase(Ease.OutElastic);
        }

        private void Spin()
        {
            spinButton.interactable = false;
            confirmButton.interactable = true;
        }

        private void ConfirmSpin()
        {
            spinButton.interactable = true;
            confirmButton.interactable = false;
            
            GameEvents.FirePowerupSelected(_powerupConfig);
        }
    }
}