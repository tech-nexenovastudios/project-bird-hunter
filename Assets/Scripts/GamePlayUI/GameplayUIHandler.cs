using System;
using System.Collections;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class GameplayUIHandler : MonoBehaviour
    {
        public static GameplayUIHandler Instance;

        [Header("HUD Reference")]
        [SerializeField] private GameplayHUD gameplayHUD;

        [Header("Slot")]
        [SerializeField] private SlotMachineScreen slotMachineScreen;
        [SerializeField] private GameObject gameoverUI;
        [Tooltip("On spin levels (5/10/15) the spin fires the same frame as the 'Level X Complete!' " +
                 "popup + smoke VFX. Wait this long before opening the slot so they don't overlap. " +
                 "Initial and chapter-start spins ignore this and open immediately.")]
        [SerializeField] private float slotOpenDelayAfterComplete = 5f;

        [Header("All-Eggs-Cleared Celebration")]
        [SerializeField] private GameObject clearCelebrationVfxPrefab;
        [SerializeField] private Transform clearCelebrationAnchor;
        [SerializeField] private float clearCelebrationVfxLifetime = 2.5f;

        [Header("Finish-Now Wrap-Up")]
        [SerializeField] private CanvasGroup finishNowOverlay;
        [SerializeField] private TextMeshProUGUI finishNowToast;
        [SerializeField] private TextMeshProUGUI  finishNowCoinTally;
        [SerializeField] private Button finishNowButton;
        [SerializeField] private float finishNowDimDuration = 0.35f;
        [SerializeField] private float finishNowToastHold = 1.2f;
        [SerializeField] private float finishNowCoinTallyDuration = 0.9f;
        [SerializeField] private float finishNowOverlayAlpha = 0.6f;

        private Coroutine _finishNowRoutine;

        private static readonly string[] FinishNowToastPhrases =
        {
            "Wrap-up!",
            "That's a wrap!",
            "Show's over!",
            "Boom! Done.",
            "Mic drop.",
            "Level smashed!",
            "Nailed it!",
            "Birds, beware.",
        };

        private const string CoinTag = "<sprite=\"Nove SDF Sprites\" name=coin>";

        private static readonly string[] FinishNowCoinPhrases =
        {
            "Cha-ching! +{0} " + CoinTag,
            "Bagged +{0} " + CoinTag + "!",
            "Snagged +{0} " + CoinTag + "!",
            "Pocketed +{0} " + CoinTag,
            "Sweet loot! +{0} " + CoinTag,
            "Nice haul: +{0} " + CoinTag,
        };

        private void Awake()
        {
            Instance = this;
            if (finishNowOverlay != null)
            {
                finishNowOverlay.alpha = 0f;
                finishNowOverlay.blocksRaycasts = false;
                finishNowOverlay.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            finishNowButton.onClick.RemoveAllListeners();
            finishNowButton.onClick.AddListener(OnClickReturnToMenu);
        }

        private void OnEnable()
        {
            GameEvents.OnSpinTriggered += OnSpinTriggered;
            GameEvents.OnPowerupSelected += OnPowerupSelected;
            GameEvents.OnPlayerDeath += OnPlayerDeath;
            GameEvents.OnAllEggsCleared += OnAllEggsCleared;
            GameEvents.OnPlayerFinishedLevel += OnPlayerFinishedLevel;
        }

        private void OnDisable()
        {
            GameEvents.OnSpinTriggered -= OnSpinTriggered;
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnAllEggsCleared -= OnAllEggsCleared;
            GameEvents.OnPlayerFinishedLevel -= OnPlayerFinishedLevel;

            if (_pendingSlotRoutine != null)
            {
                StopCoroutine(_pendingSlotRoutine);
                _pendingSlotRoutine = null;
            }
        }

        // ───────── Spin / Powerup ─────────

        // For spin levels (5/10/15) the spin fires the same frame as the "Level X Complete!" popup
        // and smoke VFX, which would otherwise show the slot on top of them. If that popup is
        // playing, hold the slot for slotOpenDelayAfterComplete seconds so the completion beat
        // plays out first; otherwise (initial spin, chapter-start spin) open immediately.
        private Coroutine _pendingSlotRoutine;

        private void OnSpinTriggered(PowerupConfig[] options)
        {
            if (LevelDetailPopUp.IsCompletePopupPlaying)
            {
                if (_pendingSlotRoutine != null) StopCoroutine(_pendingSlotRoutine);
                _pendingSlotRoutine = StartCoroutine(ShowSlotAfterDelay(options));
            }
            else
            {
                slotMachineScreen.ShowSlot(options);
            }
        }

        private IEnumerator ShowSlotAfterDelay(PowerupConfig[] options)
        {
            yield return new WaitForSecondsRealtime(slotOpenDelayAfterComplete);
            slotMachineScreen.ShowSlot(options);
            _pendingSlotRoutine = null;
        }

        private void OnPowerupSelected(PowerupConfig config)
        {
            slotMachineScreen.HideSlot();
            GameManager.Instance.StartGameplay(fromSlot: true);
        }

        private void OnPlayerDeath()
        {
            gameoverUI.SetActive(true);
        }

        public void OnClickReturnToMenu()
        {
            Destroy(XPManager.Instance.gameObject);
            SceneManager.LoadScene("MainMenu");
        }

        // ───────── Clear Celebration (task #32 hook) ─────────

        private void OnAllEggsCleared() => PlayClearCelebration();

        public void PlayClearCelebration()
        {
            if (clearCelebrationVfxPrefab == null) return;

            Vector3 spawnPos = clearCelebrationAnchor != null
                ? clearCelebrationAnchor.position
                : transform.position;

            GameObject vfx = Instantiate(clearCelebrationVfxPrefab, spawnPos, Quaternion.identity);
            Destroy(vfx, clearCelebrationVfxLifetime);
        }

        // ───────── Finish-Now Wrap-Up (task #33 hook) ─────────

        private void OnPlayerFinishedLevel() => BeginFinishNowSequence();

        public void BeginFinishNowSequence()
        {
            if (_finishNowRoutine != null) return;
            _finishNowRoutine = StartCoroutine(FinishNowRoutine());
        }

        private IEnumerator FinishNowRoutine()
        {
            if (finishNowOverlay != null)
            {
                finishNowOverlay.gameObject.SetActive(true);
                finishNowOverlay.blocksRaycasts = true;
                finishNowOverlay.DOFade(finishNowOverlayAlpha, finishNowDimDuration).SetUpdate(true);
            }

            if (finishNowToast != null)
            {
                finishNowToast.text = FinishNowToastPhrases[UnityEngine.Random.Range(0, FinishNowToastPhrases.Length)];
                finishNowToast.transform.localScale = Vector3.zero;
                finishNowToast.gameObject.SetActive(true);
                finishNowToast.transform
                    .DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            int sessionCoins = RewardManager.Instance != null
                ? RewardManager.Instance.GetSessionCoinsThisLevel()
                : 0;

            if (finishNowCoinTally != null)
            {
                string phrase = FinishNowCoinPhrases[UnityEngine.Random.Range(0, FinishNowCoinPhrases.Length)];
                finishNowCoinTally.gameObject.SetActive(true);
                finishNowCoinTally.text = string.Format(phrase, 0);
                int displayed = 0;
                DOTween.To(() => displayed, v =>
                    {
                        displayed = v;
                        finishNowCoinTally.text = string.Format(phrase, v);
                    }, sessionCoins, finishNowCoinTallyDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            }

            yield return new WaitForSecondsRealtime(finishNowDimDuration + finishNowToastHold);

            if (finishNowToast != null)
            {
                finishNowToast.transform
                    .DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => finishNowToast.gameObject.SetActive(false));
            }

            if (finishNowCoinTally != null)
            {
                finishNowCoinTally.gameObject.SetActive(false);
            }

            if (finishNowOverlay != null)
            {
                finishNowOverlay.DOFade(0f, 0.25f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        finishNowOverlay.blocksRaycasts = false;
                        finishNowOverlay.gameObject.SetActive(false);
                    });
            }

            _finishNowRoutine = null;
        }
    }
}
