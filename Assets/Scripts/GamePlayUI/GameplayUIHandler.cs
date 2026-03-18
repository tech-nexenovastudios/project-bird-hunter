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
//            ShowCurrentLevelMessage();

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
//        // [SerializeField] private TextMeshProUGUI levelDescription;

//        public void SetLevel()
//        {
//            var profile = GameProgressManager.Instance.GetCurrentLevelProfile();

//            rectTransform.DOAnchorPosX(0, 0.2f).OnStart(() => gameObject.SetActive(true));
//            DOVirtual.DelayedCall(5, () => rectTransform.DOAnchorPosX(-100, 0.2f)).OnComplete(() => gameObject.SetActive(false));
//        }
//    }
//}
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.UI
{
    public class GameplayUIHandler : MonoBehaviour
    {
        public static GameplayUIHandler Instance;

        [Header("HUD Reference")]
        [SerializeField] private GameplayHUD gameplayHUD;

        [Header("Slot")]
        [SerializeField] private SlotMachineScreen slotMachineScreen;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnSpinTriggered += slotMachineScreen.ShowSlot;
            GameEvents.OnPlayerConfirmedSpin += OnPlayerConfirmedSpin;
            GameEvents.OnPowerupSelected += OnPowerupSelected;
        }

        private void OnDisable()
        {
            GameEvents.OnSpinTriggered -= slotMachineScreen.ShowSlot;
            GameEvents.OnPlayerConfirmedSpin -= OnPlayerConfirmedSpin;
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
        }

        // ───────── spin confirmed ─────────
        private void OnPlayerConfirmedSpin()
        {
            slotMachineScreen.HideSlot();
            gameplayHUD.ShowLevelDetailPopup();
            GameManager.Instance.StartGameplay();
        }

        // ───────── powerup selected ─────────
        private void OnPowerupSelected(PowerupConfig config)
        {
            // Use config.displayName / config.icon / config.rarity for UI preview
        }
    }
}