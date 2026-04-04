using DG.Tweening;
using Gameplay.Events;
using Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class LevelProgressBar : MonoBehaviour
    {
        [Header("Progress Bar")]
        [SerializeField] private Image progressBarFill;

        [Header("Progress Bar Icon")]
        [SerializeField] private Image EggImage;
        [SerializeField] private Image BirdImage;
        [SerializeField] private int[] bossLevels = { 10, 20 };

        private int _targetScore = 0;

        // ───────── Lifecycle ─────────
        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnGameLevelUpdated;
            GameEvents.OnLevelScoreUpdated += OnLevelScoreUpdated;
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnGameLevelUpdated;
            GameEvents.OnLevelScoreUpdated -= OnLevelScoreUpdated;
        }

        // ───────── Level Started ─────────
        private void OnGameLevelUpdated(LevelProfile profile, int levelIndex)
        {
            _targetScore = profile != null ? profile.targetScore : 0;

            if (progressBarFill != null)
            {
                progressBarFill.DOKill();
                progressBarFill.fillAmount = 0f;
            }

            UpdateProgressBarIcon(levelIndex);
        }

        // ───────── Icon Swap ─────────
        private void UpdateProgressBarIcon(int currentLevel)
        {
            if (EggImage == null || BirdImage == null) return;

            bool isBossLevel = System.Array.IndexOf(bossLevels, currentLevel) >= 0;

            EggImage.gameObject.SetActive(!isBossLevel);
            BirdImage.gameObject.SetActive(isBossLevel);

            Image active = isBossLevel ? BirdImage : EggImage;
            active.transform
                .DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        // ───────── Score Updated ─────────
        private void OnLevelScoreUpdated(int levelScore, int delta)
        {
            UpdateProgressBar(levelScore);
        }

        // ───────── Progress Bar Fill ─────────
        private void UpdateProgressBar(int levelScore)
        {
            if (progressBarFill == null) return;

            float fill = _targetScore > 0
                ? Mathf.Clamp01((float)levelScore / _targetScore)
                : 0f;

            progressBarFill
                .DOFillAmount(fill, 0.3f)
                .SetEase(Ease.OutCubic);
        }
    }
}