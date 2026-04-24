using UnityEngine;

public class MainMenuAudioHook : MonoBehaviour
{
    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMainMenuMusic();
        else
            Debug.LogWarning("AudioManager not found! Make sure it exists in the scene or is a DontDestroyOnLoad singleton.");
    }
}