//using System;
//using UnityEngine;
//using UnityEngine.UI;
//using DG.Tweening;
//using Gameplay.Events;
//using Gameplay.Levels;
//using Gameplay.Managers;
//using Gameplay.PowerUps;
//using TMPro;

//namespace Gameplay.UI
//{
//    public class GameplayUIHandler : MonoBehaviour
//    {
//        public static GameplayUIHandler Instance;

//        [Header("HUD")]
//        [SerializeField] private CanvasGroup hudCanvas;
//        [SerializeField] private CanvasGroup startGameCanvas;

//        [Header("Slot")]
//        [SerializeField] private SlotMachineScreen slotMachineScreen;

//        [Header("Level Detail")]
//        [SerializeField] private LevelDetailPopup levelDetailPopup;

//        private void Awake()
//        {
//            Instance = this;
//        }

//        private void OnEnable()
//        {
//            GameEvents.OnSpinTriggered += slotMachineScreen.ShowSlot;
//            GameEvents.OnPlayerConfirmedSpin += OnPlayerConfirmedSpin;



//            GameProgressManager.OnLevelLoaded += ShowLevelLoadedSequence;
//            GameEvents.OnPowerupSelected += OnPowerUpSelected;
//        }

//        private void OnPlayerConfirmedSpin()
//        {
//            slotMachineScreen.HideSlot();
//            //ShowCurrentLevelMessage();

//            Managers.GameManager.Instance.StartGameplay();
//        }

//        private void ShowCurrentLevelMessage()
//        {
//            levelDetailPopup.SetLevel();
//        }

//        private void OnPowerUpSelected(PowerupConfig config)
//        {
//            var displayName = config.displayName;
//            var icon = config.icon;
//            var category = config.category;
//            var rarity = config.rarity;
//        }

//        private void ShowLevelLoadedSequence(LevelProfile profile)
//        {
//            hudCanvas.DOFade(1, 0.2f);

//        }


//    }

//    public class GameplayHUD : MonoBehaviour
//    {
//        [SerializeField] private TextMeshProUGUI coinText;
//        [SerializeField] private TextMeshProUGUI gemsText;
//        [SerializeField] private TextMeshProUGUI powerText;

//        [SerializeField] private TextMeshProUGUI currentLevelText;
//        [SerializeField] private TextMeshProUGUI totalLevelText;
//        [SerializeField] private TextMeshProUGUI totalXPText;
//        [SerializeField] private TextMeshProUGUI xpAmountAddedText;
//        [SerializeField] private TextMeshProUGUI totalScoreText;

//        [SerializeField] private Button backToMenuButton;
//        [SerializeField] private Button pauseButton;

//        [SerializeField] private Slider cannonHealthSlider;
//        [SerializeField] private TextMeshProUGUI cannonHealthText;

//        [SerializeField] private Slider levelProgressSlider;
//        [SerializeField] private TextMeshProUGUI levelProgressText;


//        private void OnEnable()
//        {
//            GameEvents.OnPlayerCoinsUpdated += (totalCoins) => coinText.text = $"{totalCoins}";
//            GameEvents.OnGameLevelUpdated += UpdateGameLevelUI;
//            XPManager.Instance.OnXPAdded += UpdateXPUI;
//        }

//        private void UpdateXPUI(int xp, int amountAdded)
//        {
//            totalXPText.text = $"{xp}";
//            xpAmountAddedText.text = $"+{amountAdded}";
//        }

//        private void UpdateGameLevelUI(LevelProfile profile, int levelIndex)
//        {
//            currentLevelText.text = levelIndex.ToString();
//            totalLevelText.text = "{20}";
//            levelProgressSlider.value = (levelIndex / 20f);
//            levelProgressText.text = $"{levelIndex}/{20}";
//        }
//    }

//    public class LevelDetailPopup : MonoBehaviour
//    {
//        [SerializeField] private RectTransform rectTransform;
//        [SerializeField] private TextMeshProUGUI levelName;
//       // [SerializeField] private TextMeshProUGUI levelDescription;

//        public void SetLevel()
//        {
//            var profile = GameProgressManager.Instance.GetCurrentLevelProfile();

//            rectTransform.DOAnchorPosX(0, 0.2f).OnStart(() => gameObject.SetActive(true));
//            DOVirtual.DelayedCall(5, () => rectTransform.DOAnchorPosX(-100, 0.2f)).OnComplete(() => gameObject.SetActive(false));
//        }
//    }
//}
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using Gameplay.PowerUps;
using TMPro;

namespace Gameplay.UI
{
    // ─────────────────────────────────────────────
    //  GAMEPLAY UI HANDLER  (Central UI Orchestrator)
    // ─────────────────────────────────────────────
    public class GameplayUIHandler : MonoBehaviour
    {
        public static GameplayUIHandler Instance { get; private set; }

        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup hudCanvas;
        [SerializeField] private CanvasGroup startGameCanvas;

        [Header("Screens & Popups")]
        [SerializeField] private SlotMachineScreen slotMachineScreen;
        [SerializeField] private LevelDetailPopup levelDetailPopup;

        [Header("Sub Components")]
        [SerializeField] private GameplayHUD gameplayHUD;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnSpinTriggered += HandleSpinTriggered;
            GameEvents.OnPlayerConfirmedSpin += HandlePlayerConfirmedSpin;
            GameEvents.OnPowerupSelected += HandlePowerUpSelected;
            GameProgressManager.OnLevelLoaded += HandleLevelLoaded;
        }

        private void OnDisable()
        {
            GameEvents.OnSpinTriggered -= HandleSpinTriggered;
            GameEvents.OnPlayerConfirmedSpin -= HandlePlayerConfirmedSpin;
            GameEvents.OnPowerupSelected -= HandlePowerUpSelected;
            GameProgressManager.OnLevelLoaded -= HandleLevelLoaded;
        }

        // ── Event Handlers ───────────────────────────────────

        private void HandleSpinTriggered()
        {
            slotMachineScreen?.ShowSlot();
        }

        private void HandlePlayerConfirmedSpin()
        {
            slotMachineScreen?.HideSlot();
            levelDetailPopup?.ShowPopup();
            GameManager.Instance.StartGameplay();
        }

        private void HandlePowerUpSelected(PowerupConfig config)
        {
            Debug.Log($"[UI] PowerUp Selected: {config.displayName} | Rarity: {config.rarity} | Category: {config.category}");
        }

        private void HandleLevelLoaded(LevelProfile profile)
        {
            ShowHUD();
            levelDetailPopup?.ShowPopup();
        }

        // ── HUD Visibility ───────────────────────────────────

        public void ShowHUD(float duration = 0.3f)
        {
            hudCanvas.DOFade(1f, duration).SetUpdate(true);
            hudCanvas.interactable = true;
            hudCanvas.blocksRaycasts = true;
        }

        public void HideHUD(float duration = 0.3f)
        {
            hudCanvas.DOFade(0f, duration).SetUpdate(true).OnComplete(() =>
            {
                hudCanvas.interactable = false;
                hudCanvas.blocksRaycasts = false;
            });
        }

        public void ShowStartGameCanvas(float duration = 0.3f)
        {
            startGameCanvas.DOFade(1f, duration).SetUpdate(true);
            startGameCanvas.interactable = true;
            startGameCanvas.blocksRaycasts = true;
        }

        public void HideStartGameCanvas(float duration = 0.3f)
        {
            startGameCanvas.DOFade(0f, duration).SetUpdate(true).OnComplete(() =>
            {
                startGameCanvas.interactable = false;
                startGameCanvas.blocksRaycasts = false;
            });
        }
    }


    // ─────────────────────────────────────────────
    //  GAMEPLAY HUD
    // ─────────────────────────────────────────────
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI powerText;

        [Header("Level Info")]
        [SerializeField] private TextMeshProUGUI currentLevelText;
        [SerializeField] private TextMeshProUGUI totalLevelText;
        [SerializeField] private Slider levelProgressSlider;
        [SerializeField] private TextMeshProUGUI levelProgressText;

        [Header("XP")]
        [SerializeField] private TextMeshProUGUI totalXPText;
        [SerializeField] private TextMeshProUGUI xpAmountAddedText;

        [Header("Score")]
        [SerializeField] private TextMeshProUGUI totalScoreText;

        [Header("Cannon Health")]
        [SerializeField] private Slider cannonHealthSlider;
        [SerializeField] private TextMeshProUGUI cannonHealthText;

        [Header("Buttons")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button pauseButton;

        private const int TotalLevels = 20;

        private void Awake()
        {
            backToMenuButton?.onClick.AddListener(OnBackToMenu);
            pauseButton?.onClick.AddListener(OnPause);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerCoinsUpdated += UpdateCoins;
            GameEvents.OnGameLevelUpdated += UpdateGameLevelUI;
            GameEvents.OnLevelScoreUpdated += UpdateScore;

            if (XPManager.Instance != null)
                XPManager.Instance.OnXPAdded += UpdateXPUI;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerCoinsUpdated -= UpdateCoins;
            GameEvents.OnGameLevelUpdated -= UpdateGameLevelUI;
            GameEvents.OnLevelScoreUpdated -= UpdateScore;

            if (XPManager.Instance != null)
                XPManager.Instance.OnXPAdded -= UpdateXPUI;
        }

        // ── Update Methods ───────────────────────────────────

        private void UpdateCoins(int totalCoins)
        {
            if (coinText != null)
                coinText.text = totalCoins.ToString("N0");
        }

        public void UpdateGems(int totalGems)
        {
            if (gemsText != null)
                gemsText.text = totalGems.ToString("N0");
        }

        public void UpdatePower(int totalPower)
        {
            if (powerText != null)
                powerText.text = totalPower.ToString();
        }

        private void UpdateGameLevelUI(LevelProfile profile, int levelIndex)
        {
            if (currentLevelText != null)
                currentLevelText.text = levelIndex.ToString();

            if (totalLevelText != null)
                totalLevelText.text = TotalLevels.ToString();

            float progress = Mathf.Clamp01(levelIndex / (float)TotalLevels);

            if (levelProgressSlider != null)
                levelProgressSlider.DOValue(progress, 0.4f).SetEase(Ease.OutCubic);

            if (levelProgressText != null)
                levelProgressText.text = $"{levelIndex}/{TotalLevels}";
        }

        private void UpdateXPUI(int totalXP, int amountAdded)
        {
            if (totalXPText != null)
                totalXPText.text = totalXP.ToString("N0");

            if (xpAmountAddedText != null)
            {
                xpAmountAddedText.text = $"+{amountAdded}";
                AnimateXPAdded();
            }
        }

        private void UpdateScore(int currentScore, int delta)
        {
            if (totalScoreText != null)
                totalScoreText.text = currentScore.ToString("N0");
        }

        public void UpdateCannonHealth(float current, float max)
        {
            if (cannonHealthSlider != null)
                cannonHealthSlider.DOValue(current / max, 0.3f);

            if (cannonHealthText != null)
                cannonHealthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        // ── Button Handlers ──────────────────────────────────

        private void OnBackToMenu() => GameEvents.FireBackToMenu();
        private void OnPause() => GameEvents.FirePauseGame();

        // ── Animations ───────────────────────────────────────

        private void AnimateXPAdded()
        {
            if (xpAmountAddedText == null) return;

            xpAmountAddedText.transform
                .DOScale(1.3f, 0.15f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => xpAmountAddedText.transform.DOScale(1f, 0.1f));
        }
    }


    // ─────────────────────────────────────────────
    //  LEVEL DETAIL POPUP
    // ─────────────────────────────────────────────
    public class LevelDetailPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
       // [SerializeField] private TextMeshProUGUI levelNameText;
       // [SerializeField] private TextMeshProUGUI levelDescriptionText;

        [Header("Animation Settings")]
        [SerializeField] private float slideInX = 0f;
        [SerializeField] private float slideOutX = -1200f;
        [SerializeField] private float slideInTime = 0.35f;
        [SerializeField] private float slideOutTime = 0.25f;
        [SerializeField] private float displayDuration = 4f;

        private Sequence _popupSequence;

        public void ShowPopup()
        {
            var profile = GameProgressManager.Instance?.GetCurrentLevelProfile();
            if (profile == null) return;

           /* if (levelNameText != null)
                levelNameText.text = profile.levelName;*/

           /* if (levelDescriptionText != null)
                levelDescriptionText.text = profile.levelDescription;*/

            _popupSequence?.Kill();
            gameObject.SetActive(true);
            rectTransform.anchoredPosition = new Vector2(slideOutX, rectTransform.anchoredPosition.y);

            _popupSequence = DOTween.Sequence()
                .Append(rectTransform.DOAnchorPosX(slideInX, slideInTime).SetEase(Ease.OutCubic))
                .AppendInterval(displayDuration)
                .Append(rectTransform.DOAnchorPosX(slideOutX, slideOutTime).SetEase(Ease.InCubic))
                .OnComplete(() => gameObject.SetActive(false));
        }

        public void HidePopupImmediate()
        {
            _popupSequence?.Kill();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _popupSequence?.Kill();
        }
    }
}
