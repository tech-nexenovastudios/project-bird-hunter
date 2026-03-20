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



using DG.Tweening;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("HUD Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button continueButton;

        [Header("Level Popup")]
        [SerializeField] private TextMeshProUGUI levelPopupText;

        [Header("Level Progress Bar")]
        [SerializeField] private Image progressBarFill;   // Image Type: Filled, Horizontal
       // [SerializeField] private TextMeshProUGUI ScoreText; 

        private bool _isPaused = false;
        private int _targetScore = 0;

        // ───────── lifecycle ─────────
        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnGameLevelUpdated;
            GameEvents.OnLevelScoreUpdated += OnLevelScoreUpdated;
            pauseButton.onClick.AddListener(OnPauseClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnGameLevelUpdated;
            GameEvents.OnLevelScoreUpdated -= OnLevelScoreUpdated;
            pauseButton.onClick.RemoveListener(OnPauseClicked);
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        // ───────── level started ─────────
        private void OnGameLevelUpdated(LevelProfile profile, int levelIndex)
        {
            _targetScore = profile != null ? profile.targetScore : 0;

            // Reset bar instantly to 0 on new level
            if (progressBarFill != null)
            {
                progressBarFill.DOKill();
                progressBarFill.fillAmount = 0f;
            }

            //if (ScoreText != null)
            //    ScoreText.text = $"0 / {_targetScore}";

            ShowLevelPopup();
        }

        // ───────── score updated ─────────
        private void OnLevelScoreUpdated(int levelScore, int delta)
        {
            UpdateProgressBar(levelScore);
        }

        // ───────── progress bar ─────────
        private void UpdateProgressBar(int levelScore)
        {
            if (progressBarFill == null) return;

            float fill = _targetScore > 0
                ? Mathf.Clamp01((float)levelScore / _targetScore)
                : 0f;

            progressBarFill
                .DOFillAmount(fill, 0.3f)
                .SetEase(Ease.OutCubic);

            //if (progressScoreText != null)
            //    progressScoreText.text = _targetScore > 0
            //        ? $"{levelScore} / {_targetScore}"
            //        : $"{levelScore}";
        }

        // ───────── level popup ─────────
        public void ShowLevelPopup()
        {
            if (levelPopupText == null) return;

            levelPopupText.text = $"Level {GameProgressManager.Instance.CurrentLevel}";
            levelPopupText.DOKill();

            levelPopupText.gameObject.SetActive(true);
            levelPopupText.alpha = 0f;
            levelPopupText.transform.localScale = Vector3.one * 0.5f;

            DG.Tweening.Sequence popIn = DOTween.Sequence();
            popIn.Append(levelPopupText.transform
                    .DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
            popIn.Join(levelPopupText
                    .DOFade(1f, 0.2f));
            popIn.Append(levelPopupText.transform
                    .DOScale(1f, 0.1f).SetEase(Ease.InOutSine));
            popIn.AppendInterval(1.5f);
            popIn.Append(levelPopupText
                    .DOFade(0f, 0.3f).SetEase(Ease.InCubic));
            popIn.Join(levelPopupText.transform
                    .DOScale(0.5f, 0.3f).SetEase(Ease.InBack));
            popIn.OnComplete(() => levelPopupText.gameObject.SetActive(false));
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
    }
}
