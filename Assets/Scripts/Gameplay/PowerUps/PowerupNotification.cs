using DG.Tweening;
using Gameplay.Events;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerupNotification : MonoBehaviour
{
    [Header("Notification Panel (slides in/out)")]
    [SerializeField] private RectTransform _notificationPanel;
    [SerializeField] private Image _powerupImage;
    [SerializeField] private TextMeshProUGUI _powerupNameText;
    [SerializeField] private TextMeshProUGUI _powerupDescriptionText;

    [Header("Persistent Icon (stays on screen)")]
    [SerializeField] private RectTransform _persistentIconRoot;
    [SerializeField] private Image _persistentIconSprite;   // powerup icon (behind)
    [SerializeField] private Image _persistentIconFill;     // Filled, Radial 360 (on top)

    [Header("Active Pulse Settings")]
    [SerializeField] private float _pulseScale = 1.1f;
    [SerializeField] private float _pulseDuration = 0.6f;

    [Header("Cooldown Visuals")]
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("Notification Timing")]
    [SerializeField] private float _slideDuration = 0.5f;
    [SerializeField] private float _holdDuration = 2f;

    private Vector2 _hiddenPos;
    private Tween _pulseTween;
    private Tween _cooldownTween;
    private PowerupConfig _currentConfig;
    private bool _cooldownActive;
    private float _pendingCooldownDuration;
    private float _pendingCooldownStartTime;

    private void Start()
    {
        _hiddenPos = new Vector2(-_notificationPanel.sizeDelta.x, _notificationPanel.anchoredPosition.y);
        _notificationPanel.anchoredPosition = _hiddenPos;

        if (_persistentIconRoot != null)
            _persistentIconRoot.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnPowerupSelected += ShowPowerupNotification;
        GameEvents.OnPowerupCooldownStarted += HandleCooldownStarted;
        GameEvents.OnPowerupUnequipped += HidePersistentIcon;
    }

    private void OnDisable()
    {
        GameEvents.OnPowerupSelected -= ShowPowerupNotification;
        GameEvents.OnPowerupCooldownStarted -= HandleCooldownStarted;
        GameEvents.OnPowerupUnequipped -= HidePersistentIcon;
    }

    private void HandleCooldownStarted(float duration) => StartCooldown(duration);

    // ─────────────────────────────────────────────
    private void ShowPowerupNotification(PowerupConfig config)
    {
        if (config == null) return;
        _currentConfig = config;

        // New powerup selected — clear any cooldown state from a previous powerup.
        _cooldownActive = false;
        _pendingCooldownDuration = 0f;
        _cooldownTween?.Kill();

        _notificationPanel.DOKill();

        _powerupNameText.text = config.displayName;
        _powerupDescriptionText.text = config.description;
        _powerupImage.sprite = config.icon;

        _notificationPanel.anchoredPosition = _hiddenPos;
        _notificationPanel
            .DOAnchorPosX(0f, _slideDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
                DOVirtual.DelayedCall(_holdDuration, () =>
                    _notificationPanel.DOAnchorPosX(_hiddenPos.x, _slideDuration)
                        .SetEase(Ease.InBack)
                        .OnComplete(ShowPersistentIcon)));
    }

    // ─────────────────────────────────────────────
    private void ShowPersistentIcon()
    {
        if (_persistentIconRoot == null || _currentConfig == null) return;

        _persistentIconRoot.gameObject.SetActive(true);
        _persistentIconSprite.sprite = _currentConfig.icon;
        _persistentIconFill.sprite = _currentConfig.icon;   // fill uses same sprite so it "reveals" the icon

        // If a cooldown was started while the slide-in panel was still playing,
        // resume it now from where it would naturally be — don't reset to active visuals.
        if (_pendingCooldownDuration > 0f)
        {
            float elapsed = Time.time - _pendingCooldownStartTime;
            float remaining = _pendingCooldownDuration - elapsed;
            float startFill = Mathf.Clamp01(elapsed / _pendingCooldownDuration);
            _pendingCooldownDuration = 0f;

            if (remaining > 0f)
            {
                StartCooldownVisual(remaining, startFill);
                return;
            }
        }

        SetActiveVisual();
        StartPulse();
    }

    private void SetActiveVisual()
    {
        _persistentIconSprite.color = _activeColor;

        if (_persistentIconFill != null)
        {
            _persistentIconFill.fillAmount = 1f;
            _persistentIconFill.color = _activeColor;
        }
    }

    private void StartPulse()
    {
        _pulseTween?.Kill();
        _persistentIconRoot.localScale = Vector3.one;

        _pulseTween = _persistentIconRoot
            .DOScale(_pulseScale, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulse()
    {
        _pulseTween?.Kill();
        if (_persistentIconRoot != null)
            _persistentIconRoot.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────
    public void StartCooldown(float cooldownDuration)
    {
        if (_persistentIconRoot == null || cooldownDuration <= 0f) return;

        // Cooldown can start during the slide-in panel animation, before the
        // persistent icon is visible. Defer the visual until ShowPersistentIcon runs.
        if (!_persistentIconRoot.gameObject.activeSelf)
        {
            _pendingCooldownDuration = cooldownDuration;
            _pendingCooldownStartTime = Time.time;
            return;
        }

        StartCooldownVisual(cooldownDuration, 0f);
    }

    private void StartCooldownVisual(float remainingDuration, float startFill)
    {
        StopPulse();
        _cooldownTween?.Kill();
        _cooldownActive = true;

        _persistentIconSprite.color = _cooldownColor;

        if (_persistentIconFill != null)
        {
            _persistentIconFill.fillAmount = startFill;
            _persistentIconFill.color = _activeColor;

            _cooldownTween = _persistentIconFill
                .DOFillAmount(1f, remainingDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _cooldownActive = false;
                    SetActiveVisual();
                    StartPulse();
                });
        }
    }

    public void EndCooldown()
    {
        _cooldownTween?.Kill();
        _cooldownActive = false;
        _pendingCooldownDuration = 0f;
        SetActiveVisual();
        StartPulse();
    }

    public void HidePersistentIcon()
    {
        StopPulse();
        _cooldownTween?.Kill();
        _cooldownActive = false;
        _pendingCooldownDuration = 0f;
        if (_persistentIconRoot != null)
            _persistentIconRoot.gameObject.SetActive(false);
        _currentConfig = null;
    }

    private void OnDestroy()
    {
        _pulseTween?.Kill();
        _cooldownTween?.Kill();
        _notificationPanel?.DOKill();
    }
}