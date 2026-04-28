using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.Managers;
using UnityEngine.SceneManagement;

namespace Gameplay.UI
{
    public class PausePanel : MonoBehaviour
    {
        public static PausePanel Instance;



        [Header("Panel Root")]
        [SerializeField] private GameObject pausePanelRoot;

        [Header("Music")]
        [SerializeField] private Slider musicSlider;
     

        [Header("Sound")]
        [SerializeField] private Slider soundSlider;
      
        [Header("Vibration")]
        [SerializeField] private Button vibrationToggleButton;
       [SerializeField] private RectTransform vibrationKnob;
        [SerializeField] private RectTransform vibrationBarRect;
       [SerializeField] private GameObject vibrationBarOn;
        [SerializeField] private GameObject vibrationBarOff;

        [Header("Vibration Animation")]
         private float slideSpeed = 15f;

        // In PausePanel, change these:
        private const string PREF_MUSIC = "MusicVol";    
        private const string PREF_SOUND = "SFXVol";     
        private const string PREF_VIBRATION = "VibrationOn";

        private bool vibrationOn = true;
        private float knobOnX;
        private float knobOffX;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            CalculateKnobPositions();
            LoadSettings();
            RegisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
        }

        private void CalculateKnobPositions()
        {
            if (vibrationBarRect == null || vibrationKnob == null) return;
            float trackHalf = vibrationBarRect.rect.width * 0.5f;
            float knobHalf = vibrationKnob.rect.width * 0.5f;
            knobOnX = trackHalf - knobHalf;
            knobOffX = -trackHalf + knobHalf;
        }

        private void RegisterListeners()
        {
            if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (soundSlider) soundSlider.onValueChanged.AddListener(OnSoundChanged);
            if (vibrationToggleButton) vibrationToggleButton.onClick.AddListener(OnVibrationToggleClicked);
        }

        private void UnregisterListeners()
        {
            if (musicSlider) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (soundSlider) soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            if (vibrationToggleButton) vibrationToggleButton.onClick.RemoveListener(OnVibrationToggleClicked);
        }

        private void LoadSettings()
        {
            float music = PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
            float sound = PlayerPrefs.GetFloat(PREF_SOUND, 1f);
            vibrationOn = PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;

            musicSlider.SetValueWithoutNotify(music);
            soundSlider.SetValueWithoutNotify(sound);

            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(music);
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(sound);

            RefreshMusicBar(music);
            RefreshSoundBar(sound);
            RefreshVibrationBar(vibrationOn, animate: false);
            ApplyVibrationSetting();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MUSIC, musicSlider.value);
            PlayerPrefs.SetFloat(PREF_SOUND, soundSlider.value);
            PlayerPrefs.SetInt(PREF_VIBRATION, vibrationOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnMusicChanged(float value)
        {
            RefreshMusicBar(value);
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
            SaveSettings();
        }

        private void OnSoundChanged(float value)
        {
            RefreshSoundBar(value);
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(value);
            SaveSettings();
        }

        public void OnVibrationToggleClicked()
        {
            vibrationOn = !vibrationOn;
            RefreshVibrationBar(vibrationOn, animate: true);
            ApplyVibrationSetting();

            SaveSettings();
        }

        private void RefreshMusicBar(float value)
        {
            bool hasValue = value > 0f;
            //if (musicBarFill) musicBarFill.SetActive(hasValue);
           // if (musicBarEmpty) musicBarEmpty.SetActive(!hasValue);
        }

        private void RefreshSoundBar(float value)
        {
            bool hasValue = value > 0f;
           // if (soundBarFill) soundBarFill.SetActive(hasValue);
            //if (soundBarEmpty) soundBarEmpty.SetActive(!hasValue);
        }

        private void RefreshVibrationBar(bool isOn, bool animate)
        {
           if (vibrationBarOn) vibrationBarOn.SetActive(isOn);
           if (vibrationBarOff) vibrationBarOff.SetActive(!isOn);

            if (vibrationKnob == null) return;

            float targetX = isOn ? knobOnX : knobOffX;
            StopAllCoroutines();

            if (animate) StartCoroutine(SlideKnob(targetX));
            else SetKnobX(targetX);
        }

        private IEnumerator SlideKnob(float targetX)
        {
            while (true)
            {
                float currentX = vibrationKnob.anchoredPosition.x;
                float newX = Mathf.Lerp(currentX, targetX, Time.unscaledDeltaTime * slideSpeed);
                SetKnobX(newX);

                if (Mathf.Abs(newX - targetX) < 0.5f)
                {
                    SetKnobX(targetX);
                    break;
                }
                yield return null;
            }
        }


        private void SetKnobX(float x)
        {
            Vector2 pos = vibrationKnob.anchoredPosition;
            pos.x = x;
            vibrationKnob.anchoredPosition = pos;
        }

        public static bool IsVibrationEnabled() => PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;
        public static float GetMusicVolume() => PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
        public static float GetSoundVolume() => PlayerPrefs.GetFloat(PREF_SOUND, 1f);

        public void ShowPanel()
        {
            if (pausePanelRoot) pausePanelRoot.SetActive(true);
            Time.timeScale = 0f;
            LoadSettings();
        }

        public void HidePanel()
        {
            if (pausePanelRoot) pausePanelRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OnClickResume()
        {
            HidePanel();
        }

        private void ApplyVibrationSetting()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (vibrationOn)
            {
                Handheld.Vibrate(); // Test vibration when enabled
            }
#endif
            Debug.Log($"Vibration {(vibrationOn ? "Enabled" : "Disabled")}");
        }

        //public void OnClickReturnToMenu()
        //{
        //    Time.timeScale = 1f;
        //    if (GameProgressManager.Instance != null) Destroy(GameProgressManager.Instance.gameObject);
        //    if (XPManager.Instance != null) Destroy(XPManager.Instance.gameObject);
        //    SceneManager.LoadScene("MainMenu");
        //}
    }
}
