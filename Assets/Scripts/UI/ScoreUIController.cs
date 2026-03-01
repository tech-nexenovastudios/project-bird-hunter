using UnityEngine;
using TMPro;
using Gameplay.Events;
using Gameplay.Managers;

namespace UI
{
    public class ScoreUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text deltaText;
        [SerializeField] private float deltaFadeDuration = 1f;

        private void OnEnable()
        {
            GameEvents.OnLevelScoreUpdated += OnLevelScoreUpdated;
            if (Gameplay.Managers.ScoreManager.Instance != null)
                UpdateScoreDisplay(Gameplay.Managers.ScoreManager.Instance.CurrentScore, 0);
        }

        private void OnDisable()
        {
            GameEvents.OnLevelScoreUpdated -= OnLevelScoreUpdated;
        }

        private void OnLevelScoreUpdated(int currentScore, int delta)
        {
            UpdateScoreDisplay(currentScore, delta);
        }

        private void UpdateScoreDisplay(int currentScore, int delta)
        {
            if (scoreText != null)
                scoreText.text = "Score: " + currentScore.ToString("0");

            if (deltaText != null && delta > 0)
            {
                deltaText.text = "+" + delta;
                deltaText.gameObject.SetActive(true);
                // Stub: Could add DOTween or coroutine to fade delta text
                Invoke(nameof(HideDeltaText), deltaFadeDuration);
            }
        }

        private void HideDeltaText()
        {
            if (deltaText != null)
                deltaText.gameObject.SetActive(false);
        }
    }
}
