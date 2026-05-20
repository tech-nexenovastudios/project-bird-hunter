using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class GameplayNotificationPanel : MonoBehaviour
    {
        //when open this panel will slide in from the left and stays there for 3 seconds then it will slide back out to the left
        //if notification is a power-up, it will show the icon and countdown if it has a countdown
        //the bg color will change to the power-up rarity color and the text color will adjust to the power-up rarity color
        //when panel slides out, power-up icon will stays there.

        public static GameplayNotificationPanel Instance { get; private set; }

        [Header("Notification Panel")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private float animationDuration = 0.25f;
        [SerializeField] private float defaultHoldDuration = 3f;

        private LayoutGroup _layoutGroup;
        private Image _background;
        private CanvasGroup _canvasGroup;
        private bool _isShowing;

        [Header("Power-Up Icons")]
        [SerializeField] private RectTransform powerUpIconContainer;
        [SerializeField] private RectTransform powerUpVFXContainer;
        [SerializeField] private Image powerUpIcon;
        [SerializeField] private RectTransform countdownIconContainer;
        [SerializeField] private Image countdownIcon;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Persistent Indicator")]
        [Tooltip("In-scene indicator re-skinned per selected powerup. Kept visible after the panel slides out. Receives OnPowerupCooldownStarted / OnPowerupUnequipped events.")]
        [SerializeField] private PowerUpIndicator persistentIndicator;

        // Rarity colours mirror PowerupLockView so the same visual language carries
        // from the slot-machine selection card to the in-game notification.
        private static readonly Color ColCommon = Hex("98F3AF");
        private static readonly Color ColRare = Hex("F8E64B");
        private static readonly Color ColEpic = Hex("EAB3FF");
        private static readonly Color ColLegendary = Hex("FF9B94");
        private static readonly Color DefaultTint = Color.white;

        private readonly Queue<NotificationRequest> _queue = new();

        private Vector2 _shownPos;
        private Vector2 _hiddenPos;
        private bool _positionsCached;

        private Tween _slideTween;
        private Tween _countdownTween;
        private Coroutine _holdRoutine;

        // ── Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (panel != null)
            {
                _background = panel.GetComponent<Image>();
                _canvasGroup = panel.GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
                _layoutGroup = panel.GetComponent<LayoutGroup>();
            }

            HideInPanelExtras();
            HidePanelImmediate();

            if (persistentIndicator != null) persistentIndicator.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnPowerupSelected += OnPowerupSelected;
            GameEvents.OnPowerupCooldownStarted += OnPowerupCooldownStarted;
            GameEvents.OnPowerupUnequipped += OnPowerupUnequipped;
            GameEvents.OnRewardNotification += OnRewardNotification;
        }

        private void OnDisable()
        {
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
            GameEvents.OnPowerupCooldownStarted -= OnPowerupCooldownStarted;
            GameEvents.OnPowerupUnequipped -= OnPowerupUnequipped;
            GameEvents.OnRewardNotification -= OnRewardNotification;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _slideTween?.Kill();
            _countdownTween?.Kill();
        }

        // ── Event handlers ──────────────────────────────────────────────────

        private void OnPowerupSelected(PowerupConfig config)
        {
            if (config == null) return;
            Enqueue(new NotificationRequest
            {
                kind = NotificationKind.PowerupSelected,
                headline = config.displayName,
                description = config.description,
                icon = config.icon,
                tint = RarityColour(config.rarity),
                hasTint = true,
                payload = config,
            });
        }

        private void OnRewardNotification(RewardNotification n)
        {
            Enqueue(new NotificationRequest
            {
                kind = NotificationKind.Reward,
                headline = n.headline,
                description = n.amount > 0 ? $"+{n.amount} {n.currency}" : string.Empty,
                amount = n.amount,
                currency = n.currency,
                hasTint = false,
            });
        }

        private void OnPowerupCooldownStarted(float duration)
        {
            if (persistentIndicator != null && persistentIndicator.gameObject.activeInHierarchy)
                persistentIndicator.StartCooldown(duration);
        }

        private void OnPowerupUnequipped()
        {
            if (persistentIndicator != null && persistentIndicator.gameObject.activeInHierarchy)
                persistentIndicator.Hide();
        }

        // ── Public API ──────────────────────────────────────────────────────

        public void Show(NotificationRequest request) => Enqueue(request);

        // ── Queue ───────────────────────────────────────────────────────────

        private void Enqueue(NotificationRequest req)
        {
            _queue.Enqueue(req);
            if (!_isShowing) ProcessNext();
        }

        private void ProcessNext()
        {
            if (_queue.Count == 0) { _isShowing = false; return; }
            ShowRequest(_queue.Dequeue());
        }

        // ── Show / Hide ─────────────────────────────────────────────────────

        private void ShowRequest(NotificationRequest req)
        {
            _isShowing = true;
            CachePositionsIfNeeded();

            if (notificationText != null) notificationText.text = req.headline ?? string.Empty;
            if (descriptionText != null) descriptionText.text = req.description ?? string.Empty;

            Color tint = req.hasTint ? req.tint : DefaultTint;
            if (_background != null) _background.color = tint;
            if (notificationText != null) notificationText.color = tint;
            if (descriptionText != null) descriptionText.color = tint;

            bool isPowerup = req.kind == NotificationKind.PowerupSelected;
            if (powerUpIcon != null)
            {
                powerUpIcon.sprite = req.icon;
                powerUpIcon.enabled = isPowerup && req.icon != null;
                if (isPowerup) powerUpIcon.color = Color.white;
            }

            float countdownSeconds = 0f;
            if (isPowerup && req.payload is PowerupConfig cfg)
            {
                countdownSeconds = cfg.duration;
                ActivatePersistentIndicator(req, tint);
            }

            ShowInPanelCountdown(countdownSeconds, tint);

            float hold = req.holdOverride > 0f
                ? req.holdOverride
                : (countdownSeconds > 0f ? countdownSeconds : defaultHoldDuration);

            _slideTween?.Kill();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
            }
            if (panel != null)
            {
                panel.anchoredPosition = _hiddenPos;
                _slideTween = panel.DOAnchorPos(_shownPos, animationDuration).SetEase(Ease.OutCubic);
            }

            if (_holdRoutine != null) StopCoroutine(_holdRoutine);
            _holdRoutine = StartCoroutine(HoldThenHide(hold));
        }

        private IEnumerator HoldThenHide(float hold)
        {
            yield return new WaitForSeconds(hold);

            _slideTween?.Kill();
            _countdownTween?.Kill();

            if (panel != null)
            {
                _slideTween = panel.DOAnchorPos(_hiddenPos, animationDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(FinishHide);
            }
            else
            {
                FinishHide();
            }
        }

        private void FinishHide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            HideInPanelExtras();
            _isShowing = false;
            ProcessNext();
        }

        private void HidePanelImmediate()
        {
            CachePositionsIfNeeded();
            if (panel != null) panel.anchoredPosition = _hiddenPos;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        // ── In-panel countdown (separate from the persistent indicator's
        //    radial cooldown fill). Shown only when the powerup has a duration. ─

        private void ShowInPanelCountdown(float seconds, Color tint)
        {
            _countdownTween?.Kill();
            bool active = seconds > 0f;
            if (countdownIconContainer != null) countdownIconContainer.gameObject.SetActive(active);
            if (countdownIcon != null) countdownIcon.color = tint;
            if (!active)
            {
                if (countdownText != null) countdownText.text = string.Empty;
                return;
            }
            if (countdownText != null)
            {
                countdownText.color = tint;
                countdownText.text = Mathf.CeilToInt(seconds).ToString();
            }
            float remaining = seconds;
            _countdownTween = DOTween.To(() => remaining, v =>
            {
                remaining = v;
                if (countdownText != null)
                    countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
            }, 0f, seconds).SetEase(Ease.Linear);
        }

        private void HideInPanelExtras()
        {
            if (countdownIconContainer != null) countdownIconContainer.gameObject.SetActive(false);
            if (powerUpIcon != null) powerUpIcon.enabled = false;
        }

        // ── Persistent indicator (single in-scene instance, re-skinned per pick) ─

        private void ActivatePersistentIndicator(NotificationRequest req, Color tint)
        {
            if (persistentIndicator == null) return;

            // Re-show and re-bind. PowerUpIndicator.Bind handles sprite, pulse, and spawn VFX.
            persistentIndicator.gameObject.SetActive(true);
            persistentIndicator.Bind(new NotificationRequest
            {
                kind = NotificationKind.PowerupSelected,
                icon = req.icon,
                tint = tint,
                hasTint = true,
                payload = req.payload,
            });
        }

        // ── Utilities ───────────────────────────────────────────────────────

        private void CachePositionsIfNeeded()
        {
            if (_positionsCached || panel == null) return;

            // Force layout so panel.rect.width is correct even on first frame.
            Canvas.ForceUpdateCanvases();
            if (_layoutGroup != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            _shownPos = panel.anchoredPosition;
            float width = Mathf.Max(panel.rect.width, 200f);
            _hiddenPos = new Vector2(_shownPos.x - width - 50f, _shownPos.y);
            _positionsCached = true;
        }

        private static Color RarityColour(PowerupRarity r) => r switch
        {
            PowerupRarity.Common => ColCommon,
            PowerupRarity.Rare => ColRare,
            PowerupRarity.Epic => ColEpic,
            PowerupRarity.Legendary => ColLegendary,
            _ => Color.white,
        };

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
