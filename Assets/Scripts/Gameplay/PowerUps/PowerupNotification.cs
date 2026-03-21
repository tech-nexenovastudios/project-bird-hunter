using System;
using Gameplay.Events;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PowerupNotification : MonoBehaviour
{
    [SerializeField] private RectTransform _notificationPanel;
    [SerializeField] private Image _powerupImage;
    [SerializeField] private TextMeshProUGUI _powerupNameText;
    [SerializeField] private TextMeshProUGUI _powerupDescriptionText;


    private Vector2 _widthOfPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _widthOfPanel = _notificationPanel.sizeDelta;

        _notificationPanel.anchoredPosition = new Vector2(-_widthOfPanel.x, _notificationPanel.anchoredPosition.y);
    }

    private void OnEnable()
    {
        GameEvents.OnPowerupSelected += ShowPowerupNotification;
    }

    private void OnDisable()
    {
        GameEvents.OnPowerupSelected -= ShowPowerupNotification;
    }

    private void ShowPowerupNotification(PowerupConfig config)
    {
        _powerupNameText.text = config.displayName;
        _powerupDescriptionText.text = config.description;
        // Animate the panel in and out

        _notificationPanel.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            DOVirtual.DelayedCall(2f, () =>
            {
                _notificationPanel.DOAnchorPosX(-_widthOfPanel.x, 0.5f).SetEase(Ease.InBack);
            });
        });

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
