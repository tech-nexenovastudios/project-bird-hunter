using System;
using UnityEngine;
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
    }
}