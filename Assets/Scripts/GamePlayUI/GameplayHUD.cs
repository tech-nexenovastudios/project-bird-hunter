//using DG.Tweening;
//using Gameplay.Events;
//using Gameplay.Levels;
//using Gameplay.Managers;
//using Gameplay.UI;
//using TMPro;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.UI;

//namespace Gameplay.UI
//{
//    public class GameplayHUD : MonoBehaviour
//    {
//        //[Header("Currency")]
//        //[SerializeField] private TextMeshProUGUI coinText;
//        //[SerializeField] private TextMeshProUGUI gemsText;

//        //[Header("XP")]
//        //[SerializeField] private TextMeshProUGUI totalXPText;
//        //[SerializeField] private TextMeshProUGUI xpAmountAddedText;

//        [Header("Level")]
//        [SerializeField] private TextMeshProUGUI currentLevelText;
//        [SerializeField] private TextMeshProUGUI totalLevelText;
//        [SerializeField] private TextMeshProUGUI totalScoreText;

//        [Header("Health")]
//        [SerializeField] private Slider cannonHealthSlider;
//        [SerializeField] private TextMeshProUGUI cannonHealthText;

//        //[Header("Progress")]
//        //[SerializeField] private Slider levelProgressSlider;
//        //[SerializeField] private TextMeshProUGUI levelProgressText;

//        [Header("HUD Buttons")]
//        [SerializeField] private Button backToMenuButton;
//        [SerializeField] private Button pauseButton;

//        [Header("Pause Panel")]
//        [SerializeField] private GameObject pausePanel;        
//        [SerializeField] private Button continueButton;  
//        private bool _isPaused = false;

//        private const int TotalLevels = 20;

//        // ───────── lifecycle ─────────
//        private void OnEnable()
//        {
//            //GameEvents.OnPlayerCoinsUpdated += OnCoinsUpdated;
//            GameEvents.OnGameLevelUpdated += UpdateGameLevelUI;
//          //  XPManager.Instance.OnXPAdded += UpdateXPUI;

//            pauseButton.onClick.AddListener(OnPauseClicked);
//            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
//            continueButton.onClick.AddListener(OnContinueClicked);
//        }

//        private void OnDisable()
//        {
//           // GameEvents.OnPlayerCoinsUpdated -= OnCoinsUpdated;
//            GameEvents.OnGameLevelUpdated -= UpdateGameLevelUI;
//           // XPManager.Instance.OnXPAdded -= UpdateXPUI;

//            pauseButton.onClick.RemoveListener(OnPauseClicked);
//            backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
//            continueButton.onClick.RemoveListener(OnContinueClicked);
//        }

//        // ───────── pause ─────────
//        private void OnPauseClicked()
//        {
//            PauseGame();
//        }

//        private void PauseGame()
//        {
//            _isPaused = true;
//            Time.timeScale = 0f;

//            pausePanel.SetActive(true);
//            pausePanel.transform
//                .DOScale(Vector3.one, 0.25f)
//                .From(Vector3.zero)
//                .SetEase(Ease.OutBack)
//                .SetUpdate(true);   // SetUpdate(true) so DOTween ignores timeScale=0

//            GameEvents.FirePauseToggled(true);
//        }

//        // ───────── continue ─────────
//        private void OnContinueClicked()
//        {
//            ResumeGame();
//        }

//        private void ResumeGame()
//        {
//            pausePanel.transform
//                .DOScale(Vector3.zero, 0.2f)
//                .SetEase(Ease.InBack)
//                .SetUpdate(true)
//                .OnComplete(() => pausePanel.SetActive(false));

//            _isPaused = false;
//            Time.timeScale = 1f;

//            GameEvents.FirePauseToggled(false);
//        }

//        // ───────── back to menu ─────────
//        private void OnBackToMenuClicked()
//        {
//            if (_isPaused) ResumeGame();
//            GameEvents.FireBackToMenu();
//        }

//        // ───────── UI updates ─────────
//        //private void OnCoinsUpdated(int total)
//        //    => coinText.text = $"{total}";

//        //private void UpdateXPUI(int xp, int amountAdded)
//        //{
//        //    totalXPText.text = $"{xp}";
//        //    xpAmountAddedText.text = $"+{amountAdded}";
//        //}

//        private void UpdateGameLevelUI(LevelProfile profile, int levelIndex)
//        {
//            currentLevelText.text = levelIndex.ToString();
//            totalLevelText.text = $"{TotalLevels}";
//            //levelProgressSlider.value = levelIndex / (float)TotalLevels;
//            //levelProgressText.text = $"{levelIndex}/{TotalLevels}";
//        }
//    }
//}
using System.ComponentModel;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Gameplay.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Slider cannonHealthSlider;
       // [SerializeField] private TextMeshProUGUI cannonHealthText;

        [Header("HUD Buttons")]
        //[SerializeField] private Button backToMenuButton;
        [SerializeField] private Button pauseButton;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button continueButton;

        [Header("Level Detail Popup")]
        [SerializeField] private RectTransform levelDetailRect;
        [SerializeField] private TextMeshProUGUI levelPopupText;   // shows "Level 18"

        private bool _isPaused = false;

        // ───────── lifecycle ─────────
        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnGameLevelUpdated;

            pauseButton.onClick.AddListener(OnPauseClicked);
         //   backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnGameLevelUpdated;

            pauseButton.onClick.RemoveListener(OnPauseClicked);
          //  backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        // ───────── level updated ─────────
        private void OnGameLevelUpdated(LevelProfile profile, int levelIndex)
        {
            ShowLevelDetailPopup();
        }

        // ───────── level detail popup ─────────
        public void ShowLevelDetailPopup()
        {
            if (levelDetailRect == null) return;

            if (levelPopupText != null)
                levelPopupText.text = $"Level {GameProgressManager.Instance.CurrentLevel}";

            levelDetailRect.DOKill();

            levelDetailRect.gameObject.SetActive(true);
            levelDetailRect
                .DOAnchorPosX(0, 0.2f)
                .From(new Vector2(-300, levelDetailRect.anchoredPosition.y))
                .SetEase(Ease.OutCubic);

            DOVirtual.DelayedCall(2f, () =>
                levelDetailRect
                    .DOAnchorPosX(-300, 0.2f)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() => levelDetailRect.gameObject.SetActive(false)));
        }

        // ───────── pause ─────────
        private void OnPauseClicked()
        {
            if (_isPaused) return;
            PauseGame();
        }

        private void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;

            pausePanel.SetActive(true);
            pausePanel.transform
                .DOScale(Vector3.one, 0.25f)
                .From(Vector3.zero)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            GameEvents.FirePauseToggled(true);
        }

        // ───────── continue ─────────
        private void OnContinueClicked()
        {
            if (!_isPaused) return;
            ResumeGame();
        }

        private void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            pausePanel.transform
                .DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() => pausePanel.SetActive(false));

            GameEvents.FirePauseToggled(false);
        }

        // ───────── back to menu ─────────
        //private void OnBackToMenuClicked()
        //{
        //    if (_isPaused) ResumeGame();
        //    GameEvents.FireBackToMenu();
        //}
    }
}
