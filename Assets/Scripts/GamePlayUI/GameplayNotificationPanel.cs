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
        //the bg color will change to the power-up rarity color (deep) and the text/icon color uses the rarity accent (bright)
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

        // True while the persistent indicator is showing an equipped powerup. The indicator's
        // icon is the SAME Image as `powerUpIcon`, so non-powerup toasts (e.g. "Gem Found") must
        // not disable it while this is set, or the persistent powerup notification vanishes.
        private bool _persistentActive;

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

        // ── Rarity palette ──────────────────────────────────────────────────
        // Two-tone system: deep background (matches card frame) + bright accent
        // (matches card title text). White headline reads cleanly on every deep BG.

        // Deep panel backgrounds
        private static readonly Color BgCommon = Hex("2F6B3A");
        private static readonly Color BgRare = Hex("A86A1C");
        private static readonly Color BgEpic = Hex("5B2A8C");
        private static readonly Color BgLegendary = Hex("8B1F1F");

        // Bright accents for text, countdown ring, and countdown number
        private static readonly Color FgCommon = Hex("A8F0B4");
        private static readonly Color FgRare = Hex("FFD24A");
        private static readonly Color FgEpic = Hex("E0A8FF");
        private static readonly Color FgLegendary = Hex("FF6B5E");

        // Neutral palette for non-powerup notifications (rewards, generic messages).
        private static readonly Color DefaultBg = Hex("1A1A22");
        private static readonly Color DefaultFg = Color.white;

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
            GameEvents.OnPreBossRecoveryActivated += OnPreBossRecoveryActivated;
            GameEvents.OnRewardNotification += OnRewardNotification;
        }

        private void OnDisable()
        {
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
            GameEvents.OnPowerupCooldownStarted -= OnPowerupCooldownStarted;
            GameEvents.OnPowerupUnequipped -= OnPowerupUnequipped;
            GameEvents.OnPreBossRecoveryActivated -= OnPreBossRecoveryActivated;
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
            var (bg, fg) = RarityPalette(config.rarity);
            Enqueue(new NotificationRequest
            {
                kind = NotificationKind.PowerupSelected,
                headline = config.displayName,
                description = config.description,
                icon = config.icon,
                bgTint = bg,
                fgTint = fg,
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
                bgTint = DefaultBg,
                fgTint = DefaultFg,
                hasTint = true,
            });
        }

        private void OnPowerupCooldownStarted(float duration)
        {
            if (persistentIndicator != null && persistentIndicator.gameObject.activeInHierarchy)
                persistentIndicator.StartCooldown(duration);
        }

        private void OnPowerupUnequipped()
        {
            _persistentActive = false;
            if (persistentIndicator != null && persistentIndicator.gameObject.activeInHierarchy)
                persistentIndicator.Hide();
        }

        // Pre-Boss Recovery's deferred heal just fired (boss entered the level). Promote the
        // greyed pending indicator to its normal active look.
        private void OnPreBossRecoveryActivated()
        {
            if (persistentIndicator != null && persistentIndicator.gameObject.activeInHierarchy)
                persistentIndicator.ActivateFromPending();
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

            Color bg = req.hasTint ? req.bgTint : DefaultBg;
            Color fg = req.hasTint ? req.fgTint : DefaultFg;

            if (_background != null) _background.color = bg;
            if (notificationText != null) notificationText.color = fg;
            if (descriptionText != null) descriptionText.color = fg;

            bool isPowerup = req.kind == NotificationKind.PowerupSelected;
            if (powerUpIcon != null)
            {
                if (isPowerup)
                {
                    powerUpIcon.sprite = req.icon;
                    powerUpIcon.enabled = req.icon != null;
                    powerUpIcon.color = Color.white;
                }
                else if (!_persistentActive)
                {
                    // Non-powerup toast (e.g. "Gem Found") and no persistent powerup to preserve —
                    // safe to clear the shared icon.
                    powerUpIcon.sprite = req.icon;
                    powerUpIcon.enabled = false;
                }
                // else: a persistent powerup indicator owns this shared Image — leave it visible
                // so the powerup notification is restored once this reward toast slides out.
            }

            float countdownSeconds = 0f;
            if (isPowerup && req.payload is PowerupConfig cfg)
            {
                countdownSeconds = cfg.duration;
                ActivatePersistentIndicator(req, bg, fg);
            }

            ShowInPanelCountdown(countdownSeconds, fg);

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

        private void ShowInPanelCountdown(float seconds, Color accent)
        {
            _countdownTween?.Kill();
            bool active = seconds > 0f;
            if (countdownIconContainer != null) countdownIconContainer.gameObject.SetActive(active);
            if (countdownIcon != null) countdownIcon.color = accent;
            if (!active)
            {
                if (countdownText != null) countdownText.text = string.Empty;
                return;
            }
            if (countdownText != null)
            {
                countdownText.color = accent;
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
            //if (powerUpIcon != null) powerUpIcon.enabled = false;
        }

        // ── Persistent indicator (single in-scene instance, re-skinned per pick) ─

        private void ActivatePersistentIndicator(NotificationRequest req, Color bg, Color fg)
        {
            if (persistentIndicator == null) return;

            // Re-show and re-bind. PowerUpIndicator.Bind handles sprite, pulse, and spawn VFX.
            persistentIndicator.gameObject.SetActive(true);
            var bound = new NotificationRequest
            {
                kind = NotificationKind.PowerupSelected,
                icon = req.icon,
                bgTint = bg,
                fgTint = fg,
                hasTint = true,
                payload = req.payload,
            };

            // Pre-Boss Recovery is deferred: show it greyed/pending until the boss spawns
            // (OnPreBossRecoveryActivated promotes it), instead of the usual active look.
            if (req.payload is PowerupConfig cfg && cfg.effectType == PreBossHealModifier.ConfigEffectType)
                persistentIndicator.BindPending(bound);
            else
                persistentIndicator.Bind(bound);

            // A powerup now owns the shared icon — keep later reward toasts from hiding it.
            _persistentActive = true;
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

        private static (Color bg, Color fg) RarityPalette(PowerupRarity r) => r switch
        {
            PowerupRarity.Common => (BgCommon, FgCommon),
            PowerupRarity.Rare => (BgRare, FgRare),
            PowerupRarity.Epic => (BgEpic, FgEpic),
            PowerupRarity.Legendary => (BgLegendary, FgLegendary),
            _ => (DefaultBg, DefaultFg),
        };

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}