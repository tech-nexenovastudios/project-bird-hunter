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
            // Show the LEVEL score (resets each level), matching OnLevelScoreUpdated — not the
            // run total (CurrentScore), which would surface the previous level's accumulated
            // score when the HUD re-enables right after a level reset.
            if (Gameplay.Managers.ScoreManager.Instance != null)
                UpdateScoreDisplay(Gameplay.Managers.ScoreManager.Instance.LevelScore, 0);
        }

        private void OnDisable()
        {
            GameEvents.OnLevelScoreUpdated -= OnLevelScoreUpdated;
        }

        private void OnLevelScoreUpdated(int levelScore, int delta)
        {
            UpdateScoreDisplay(levelScore, delta);
        }

        private void UpdateScoreDisplay(int levelScore, int delta)
        {
            if (scoreText != null)
            {
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                scoreText.text = levelScore.ToString("N0", ci);
            }

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
