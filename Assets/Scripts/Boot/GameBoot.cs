using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRoot : MonoBehaviour
{
    private static GameRoot instance;

    // First OnApplicationFocus(true) fires during cold start, on top of the
    // boot sequence. Skipping it once avoids piling a GC hint onto an already
    // busy main thread.
    private bool focusInitialised;

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
            GameLog.LogWarning("AudioManager not found! Make sure it exists in the scene or is a DontDestroyOnLoad singleton.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Previously this callback ran GC.Collect() + GC.WaitForPendingFinalizers()
    // + GC.Collect() synchronously on every sceneLoaded. During boot we load
    // BOOTSTRAPPER -> LOADING -> MAIN_MENU plus unloads, so the main thread
    // stalled 3-5 times in a row and the splash felt stuck on lower-end Android.
    // We let Unity's incremental GC handle steady-state and only post a
    // non-blocking hint here so a collection can overlap with frames instead of
    // stopping the world.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Application.runInBackground = true;

        if (!focusInitialised)
        {
            focusInitialised = true;
            return;
        }

        if (!hasFocus) return;

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: false);
    }
}
