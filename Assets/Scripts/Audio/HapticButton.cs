using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    /// <summary>
    /// Drop-in haptic feedback for any UnityEngine.UI.Button. Plays a single tap
    /// of the configured intensity on click, gated by the global haptics toggle.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HapticButton : MonoBehaviour
    {
        [SerializeField] private HapticsController.Intensity intensity = HapticsController.Intensity.Light;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            HapticsController.Tap(intensity);
        }
    }
}
