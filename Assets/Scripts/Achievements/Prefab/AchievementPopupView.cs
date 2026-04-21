using BirdHunter.Achievement;

namespace DefaultNamespace
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public sealed class AchievementPopupView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        public void Initialize(IAchievement achievement)
        {
            _titleText.text = achievement.DisplayName;
            _descriptionText.text = achievement.Description;
        }
    }
}