using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class ChapterProgressBar : MonoBehaviour
    {
        [Header("Slots (exactly 10)")]
        [SerializeField] private Image[] slots;

        [Header("Source Images")]
        [SerializeField] private Image fillType1Image;   // levels 1-10
        [SerializeField] private Image fillType2Image;   // levels 11-20
        [SerializeField] private Image emptyImage;

        [Header("Label")]
        [SerializeField] private TextMeshProUGUI levelLabel;

        // ───────── lifecycle ─────────
        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnLevelUpdated;
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnLevelUpdated;
        }

        private void OnLevelUpdated(LevelProfile profile, int globalLevelIndex)
        {
            var pm = GameProgressManager.Instance;
            Refresh(pm.CurrentLevel, pm.CurrentChapter);
        }

        // ───────── core ─────────
        private void Refresh(int currentLevel, int currentChapter)
        {
            bool isSecondHalf = currentLevel > 10;
            int filledCount = isSecondHalf ? currentLevel - 10 : currentLevel;
            Image fillImgSource = isSecondHalf ? fillType2Image : fillType1Image;

            for (int i = 0; i < slots.Length; i++)
            {
                int distanceFromBottom = (slots.Length - 1) - i;
                bool isFilled = distanceFromBottom < filledCount;
                Image source = isFilled ? fillImgSource : emptyImage;

                slots[i].sprite = source.sprite;
                slots[i].color = source.color;
                slots[i].material = source.material;
                slots[i].type = source.type;
                slots[i].preserveAspect = source.preserveAspect;
                slots[i].raycastTarget = false;
            }

            if (levelLabel != null)
                levelLabel.text = $"{currentLevel}";
        }
    }
}