using System;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SceneManagement;

public class GameRoot : MonoBehaviour
{
    private static GameRoot instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        QualitySettings.antiAliasing = 1;
        Application.runInBackground = false;
    }
    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMainMenuMusic();
        else
            Debug.LogWarning("AudioManager not found! Make sure it exists in the scene or is a DontDestroyOnLoad singleton.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += (arg0, mode) =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        };
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Application.runInBackground = true;
    }
}