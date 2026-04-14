using UnityEngine;

public class MainMenuAudioHook : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance?.PlayMainMenuMusic();
    }
}