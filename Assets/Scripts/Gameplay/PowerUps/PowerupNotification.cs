using DG.Tweening;
using Gameplay.Events;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerupNotification : MonoBehaviour
{
    [SerializeField] private RectTransform _notificationPanel;
    [SerializeField] private Image _powerupImage;
    [SerializeField] private TextMeshProUGUI _powerupNameText;
    [SerializeField] private TextMeshProUGUI _powerupDescriptionText;

    private Vector2 _hiddenPos;

    private void Start()
    {
        _hiddenPos = new Vector2(-_notificationPanel.sizeDelta.x, _notificationPanel.anchoredPosition.y);
        _notificationPanel.anchoredPosition = _hiddenPos;
    }

    private void OnEnable() => GameEvents.OnPowerupSelected += ShowPowerupNotification;
    private void OnDisable() => GameEvents.OnPowerupSelected -= ShowPowerupNotification;

    private void ShowPowerupNotification(PowerupConfig config)
    {
        // Kill any in-progress animation before starting fresh
        _notificationPanel.DOKill();

        _powerupNameText.text = config.displayName;
        _powerupDescriptionText.text = config.description;
        _powerupImage.sprite = config.icon;

        // Slide in → hold 2 s → slide out
        _notificationPanel.anchoredPosition = _hiddenPos;
        _notificationPanel
            .DOAnchorPosX(0f, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
                DOVirtual.DelayedCall(2f, () =>
                    _notificationPanel.DOAnchorPosX(_hiddenPos.x, 0.5f).SetEase(Ease.InBack)));
    }
}