using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    /// <summary>
    /// Production-grade countdown display: breathing halo, twin depleting radial rings,
    /// scale-punch on each tick, color phase-shift in the critical band, particle bursts.
    /// Drive via <see cref="Play"/>. Self-contained — does not subscribe to gameplay events.
    /// </summary>
    public class CountdownDisplay : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform root;
        [SerializeField] private Image halo;
        [SerializeField] private Image outerRing;
        [SerializeField] private Image innerRing;
        [SerializeField] private Image plate;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private Transform particleAnchor;

        [Header("Optional FX prefabs (Resources or assigned)")]
        [SerializeField] private GameObject tickBurstVfxPrefab;
        [SerializeField] private GameObject finalBurstVfxPrefab;

        [Header("Breathing (idle halo)")]
        [SerializeField] private float breathePeriod = 1.6f;
        [SerializeField] private float breatheScaleMin = 0.92f;
        [SerializeField] private float breatheScaleMax = 1.12f;
        [SerializeField] private float breatheAlphaMin = 0.45f;
        [SerializeField] private float breatheAlphaMax = 1.00f;

        [Header("Tick punch (number)")]
        [SerializeField] private float tickPunchScale = 0.45f;
        [SerializeField] private float tickPunchDuration = 0.38f;
        [SerializeField] private int   tickPunchVibrato = 6;
        [SerializeField] private float finalPunchMultiplier = 2.0f;

        [Header("Critical phase (last seconds)")]
        [SerializeField] private int   criticalThreshold = 3;
        [SerializeField] private Color normalColor   = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.30f, 0.30f, 1f);
        [SerializeField] private Color goColor       = new Color(0.50f, 1f, 0.50f, 1f);
        [SerializeField] private float criticalBreathSpeedUp = 1.8f;

        [Header("Fade")]
        [SerializeField] private float fadeInDuration  = 0.30f;
        [SerializeField] private float fadeOutDuration = 0.30f;

        private Coroutine _routine;
        private Tween _breatheScaleTween;
        private Tween _breatheAlphaTween;
        private bool _isCritical;
        private float _activeBreathePeriod;

        // ─────────────────────────── Public API ───────────────────────────

        public void Play(int seconds, string finalLabel = "GO!", Action<int> onTick = null, Action onComplete = null)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlayRoutine(seconds, finalLabel, onTick, onComplete));
        }

        public void Stop()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            KillBreathing();
            if (root != null) root.gameObject.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        // ─────────────────────────── Lifecycle ────────────────────────────

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (root != null) root.gameObject.SetActive(false);
            _activeBreathePeriod = breathePeriod;
        }

        private void OnDestroy() => Stop();

        // ─────────────────────────── Routine ──────────────────────────────

        private IEnumerator PlayRoutine(int seconds, string finalLabel, Action<int> onTick, Action onComplete)
        {
            if (seconds <= 0) { onComplete?.Invoke(); yield break; }

            if (root != null) root.gameObject.SetActive(true);
            ApplyPhase(critical: false);
            SetRingFill(1f);
            if (numberText != null) numberText.text = seconds.ToString();

            // Fade in + entrance pop on the whole rig.
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
            }
            if (root != null)
            {
                root.localScale = Vector3.one * 0.7f;
                root.DOScale(1f, fadeInDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }

            StartBreathing();

            int remaining = seconds;
            while (remaining > 0)
            {
                if (numberText != null) numberText.text = remaining.ToString();
                ApplyPhase(critical: remaining <= criticalThreshold);
                PunchNumber(strong: false);
                SpawnVfx(tickBurstVfxPrefab);
                onTick?.Invoke(remaining);

                AnimateRingsDeplete(remaining, seconds, 1f);
                yield return WaitPausableRealtime(1f);
                remaining--;
            }

            // Final beat — "GO!"
            if (numberText != null)
            {
                numberText.text = finalLabel;
                numberText.color = goColor;
            }
            if (outerRing != null) outerRing.DOColor(goColor, 0.15f).SetUpdate(true);
            if (innerRing != null) innerRing.DOColor(WithAlpha(goColor, 0.6f), 0.15f).SetUpdate(true);
            if (halo != null)      halo.DOColor(WithAlpha(goColor, halo.color.a), 0.15f).SetUpdate(true);
            PunchNumber(strong: true);
            SpawnVfx(finalBurstVfxPrefab);
            onComplete?.Invoke();

            yield return WaitPausableRealtime(0.65f);

            // Fade out + zoom-out
            if (canvasGroup != null)
                canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InCubic).SetUpdate(true);
            if (root != null)
                root.DOScale(0.7f, fadeOutDuration).SetEase(Ease.InBack).SetUpdate(true)
                    .OnComplete(() => root.gameObject.SetActive(false));

            yield return WaitPausableRealtime(fadeOutDuration);
            KillBreathing();
            _routine = null;
        }

        // ─────────────────────────── Visuals ──────────────────────────────

        private void StartBreathing()
        {
            KillBreathing();
            if (halo == null) return;

            halo.transform.localScale = Vector3.one * breatheScaleMin;
            halo.color = WithAlpha(halo.color, breatheAlphaMin);

            _breatheScaleTween = halo.transform
                .DOScale(breatheScaleMax, _activeBreathePeriod * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);

            _breatheAlphaTween = halo
                .DOFade(breatheAlphaMax, _activeBreathePeriod * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void KillBreathing()
        {
            _breatheScaleTween?.Kill();  _breatheScaleTween = null;
            _breatheAlphaTween?.Kill();  _breatheAlphaTween = null;
        }

        private void ApplyPhase(bool critical)
        {
            if (critical == _isCritical) return;
            _isCritical = critical;

            Color target = critical ? criticalColor : normalColor;
            if (numberText != null) numberText.DOColor(target, 0.2f).SetUpdate(true);
            if (outerRing != null)  outerRing.DOColor(target, 0.2f).SetUpdate(true);
            if (innerRing != null)  innerRing.DOColor(WithAlpha(target, 0.6f), 0.2f).SetUpdate(true);
            if (halo != null)       halo.DOColor(WithAlpha(target, halo.color.a), 0.2f).SetUpdate(true);

            // Speed the breathing up when critical — visual tension.
            _activeBreathePeriod = critical ? breathePeriod / criticalBreathSpeedUp : breathePeriod;
            StartBreathing();
        }

        private void PunchNumber(bool strong)
        {
            if (numberText == null) return;
            var t = numberText.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            float magnitude = strong ? tickPunchScale * finalPunchMultiplier : tickPunchScale;
            t.DOPunchScale(Vector3.one * magnitude, tickPunchDuration, tickPunchVibrato, 0.6f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        private void AnimateRingsDeplete(int remaining, int total, float duration)
        {
            if (total <= 0) return;
            float start = (float)remaining / total;
            float end   = Mathf.Max(0f, (float)(remaining - 1) / total);

            if (outerRing != null)
            {
                outerRing.DOKill();
                outerRing.fillAmount = start;
                outerRing.DOFillAmount(end, duration).SetEase(Ease.Linear).SetUpdate(true);
            }
            if (innerRing != null)
            {
                innerRing.DOKill();
                innerRing.fillAmount = start;
                // Slight delay creates a layered "trailing" feel.
                innerRing.DOFillAmount(end, duration * 1.08f).SetEase(Ease.Linear).SetUpdate(true);
            }
        }

        private void SetRingFill(float amount)
        {
            if (outerRing != null) { outerRing.DOKill(); outerRing.fillAmount = amount; }
            if (innerRing != null) { innerRing.DOKill(); innerRing.fillAmount = amount; }
        }

        private void SpawnVfx(GameObject prefab)
        {
            if (prefab == null || particleAnchor == null) return;
            var go = Instantiate(prefab, particleAnchor.position, Quaternion.identity, particleAnchor);
            Destroy(go, 3f);
        }

        // ─────────────────────────── Helpers ──────────────────────────────

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // Pausable real-time wait — counts only when Time.timeScale > 0, matching
        // LevelDetailPopUp.WaitPausableRealtime so pause halts the countdown.
        private IEnumerator WaitPausableRealtime(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (Time.timeScale > 0f)
                    elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
