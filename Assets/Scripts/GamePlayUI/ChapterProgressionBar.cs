using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Ensure DOTween is included

namespace Gameplay.UI
{
    public class ChapterProgressBar : MonoBehaviour
    {
        [Header("Slots (exactly 10)")]
        [SerializeField] private Image[] slots;

        [Header("Source Images")]
        [SerializeField] private Image fillType1Image;
        [SerializeField] private Image fillType2Image;
        [SerializeField] private Image emptyImage;

        [Header("Label")]
        [SerializeField] private TextMeshProUGUI levelLabel;

        [Header("Animations")]
        private float animationDuration = 0.5f;
        private float punchAmount = 0.25f;

        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnLevelUpdated;
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnLevelUpdated;
        }

        private void OnLevelUpdated(int globalLevelIndex)
        {
            var pm = GameProgressManager.Instance;
            Refresh(pm.CurrentLevel, pm.CurrentChapter);
        }

        private void Refresh(int currentLevel, int currentChapter)
        {
            // Logic: Type 2 fills from bottom, Type 1 stays on top of it
            int type2Count = currentLevel > 10 ? currentLevel - 10 : 0;
            int type1Count = currentLevel > 10 ? 10 - type2Count : currentLevel;

            for (int i = 0; i < slots.Length; i++)
            {
                int distanceFromBottom = (slots.Length - 1) - i;
                Image source;

                if (distanceFromBottom < type2Count)
                    source = fillType2Image;
                else if (distanceFromBottom < type2Count + type1Count)
                    source = fillType1Image;
                else
                    source = emptyImage;

                // Only trigger the "Pop" if the sprite is actually changing to a filled state
                if (slots[i].sprite != source.sprite && source != emptyImage)
                {
                    AnimateSlot(slots[i].transform);
                }

                UpdateSlotVisuals(slots[i], source);
            }

            if (levelLabel != null)
                levelLabel.text = $"{currentLevel}";
        }

        private void UpdateSlotVisuals(Image slot, Image source)
        {
            slot.sprite = source.sprite;
            slot.color = source.color;
            slot.material = source.material;
            slot.type = source.type;
            slot.preserveAspect = source.preserveAspect;
            slot.raycastTarget = false;
        }

        private void AnimateSlot(Transform slotTransform)
        {
            // Reset scale in case an animation was already running
            slotTransform.DOKill();
            slotTransform.localScale = Vector3.one;

            // Simple "Punch" effect to make it pop out and back
            slotTransform.DOPunchScale(Vector3.one * punchAmount, animationDuration, 6, 0.6f)
                         .SetEase(Ease.OutBack)
                         .SetUpdate(true); // Works even if game is paused
        }
    }
}