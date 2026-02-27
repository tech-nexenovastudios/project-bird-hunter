using UnityEngine;
using UnityEngine.UI;

public class SettingPanelController : MonoBehaviour
{
    [Header("Toggle Type")]
    [SerializeField] private ToggleType toggleType;
    
    [Header("UI References")]
    [SerializeField] private GameObject onBg;
    [SerializeField] private GameObject offBg;
    [SerializeField] private RectTransform toggleBtn;
    
    [Header("Toggle Position")]
    [SerializeField] private float toggleOnPositionX = 34.656f;
    
    private float toggleOffPositionX;
    private bool isEnabled = true;
    
    // PlayerPrefs keys
    private const string SOUND_KEY = "SoundEnabled";
    private const string MUSIC_KEY = "MusicEnabled";
    private const string NOTIFICATION_KEY = "NotificationEnabled";
    private const string VIBRATION_KEY = "VibrationEnabled";

    public enum ToggleType
    {
        Sound,
        Music,
        Notification,
        Vibration
    }

    private void Awake()
    {
        toggleOffPositionX = -toggleOnPositionX;
        LoadSetting();
    }

    private void Start()
    {
        UpdateUI();
        ApplySetting();
    }

    public void Toggle()
    {
        isEnabled = !isEnabled;
        UpdateUI();
        SaveSetting();
        ApplySetting();
    }

    private void UpdateUI()
    {
        // Update backgrounds
        onBg.SetActive(isEnabled);
        offBg.SetActive(!isEnabled);
        
        // Update toggle button position
        Vector2 pos = toggleBtn.anchoredPosition;
        pos.x = isEnabled ? toggleOnPositionX : toggleOffPositionX;
        toggleBtn.anchoredPosition = pos;
    }

    private void ApplySetting()
    {
        switch (toggleType)
        {
            case ToggleType.Sound:
                ApplySoundSetting();
                break;
            case ToggleType.Music:
                ApplyMusicSetting();
                break;
            case ToggleType.Notification:
                ApplyNotificationSetting();
                break;
            case ToggleType.Vibration:
                ApplyVibrationSetting();
                break;
        }
    }

    private void ApplySoundSetting()
    {
        if (isEnabled)
        {
            // Enable sound effects
            AudioListener.pause = false;
        }
        else
        {
            // Disable sound effects
            AudioListener.pause = true;
        }
        Debug.Log($"Sound {(isEnabled ? "Enabled" : "Disabled")}");
    }

    private void ApplyMusicSetting()
    {
        if (isEnabled)
        {
            // Enable music
            AudioListener.volume = 1f;
        }
        else
        {
            // Disable music
            AudioListener.volume = 0f;
        }
        Debug.Log($"Music {(isEnabled ? "Enabled" : "Disabled")}");
    }

    private void ApplyNotificationSetting()
    {
        // Implement your notification enable/disable logic here
        Debug.Log($"Notifications {(isEnabled ? "Enabled" : "Disabled")}");
    }

    private void ApplyVibrationSetting()
    {
        // Implement your vibration enable/disable logic here
#if UNITY_ANDROID || UNITY_IOS
        if (isEnabled)
        {
            Handheld.Vibrate(); // Test vibration when enabled
        }
#endif
        Debug.Log($"Vibration {(isEnabled ? "Enabled" : "Disabled")}");
    }

    private void SaveSetting()
    {
        string key = GetPlayerPrefsKey();
        PlayerPrefs.SetInt(key, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSetting()
    {
        string key = GetPlayerPrefsKey();
        isEnabled = PlayerPrefs.GetInt(key, 1) == 1; // Default to enabled (1)
    }

    private string GetPlayerPrefsKey()
    {
        switch (toggleType)
        {
            case ToggleType.Sound:
                return SOUND_KEY;
            case ToggleType.Music:
                return MUSIC_KEY;
            case ToggleType.Notification:
                return NOTIFICATION_KEY;
            case ToggleType.Vibration:
                return VIBRATION_KEY;
            default:
                return "";
        }
    }

    // Public getter for external scripts
    public bool IsEnabled() => isEnabled;
    public ToggleType GetToggleType() => toggleType;
}