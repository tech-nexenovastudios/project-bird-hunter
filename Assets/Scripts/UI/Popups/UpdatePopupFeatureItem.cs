using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups
{
    public class UpdatePopupFeatureItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void SetDescription(string text)
        {
            if (descriptionText != null)
                descriptionText.text = text;
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon != null)
                icon.sprite = sprite;
        }
    }
}
