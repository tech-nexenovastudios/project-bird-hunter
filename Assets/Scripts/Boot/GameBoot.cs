using UnityEngine;

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

        // Must be 0 for targetFrameRate to work
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        Application.runInBackground = true;
    }
}