using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    // ─── State ───
    public string CurrentActiveScene => SceneManager.GetActiveScene().name;
    public bool IsLoading { get; private set; }

    // ─── Public API ───

    /// <summary>
    /// Loads a scene additively (on top of current scenes) without unloading anything.
    /// </summary>
    public async UniTask LoadSceneAdditiveAsync(
        string sceneName,
        bool setActive = false,
        IProgress<float> progress = null,
        CancellationToken ct = default)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoader] Already loading a scene. Ignoring additive load for '{sceneName}'.");
            return;
        }

        if (IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is already loaded.");
            return;
        }

        IsLoading = true;
        EventBus.Publish(new SceneLoadStartedEvent { sceneName = sceneName });

        try
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(op.progress);
                EventBus.Publish(new SceneLoadProgressEvent { sceneName = sceneName, progress = op.progress });
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (setActive)
            {
                var loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.IsValid())
                    SceneManager.SetActiveScene(loadedScene);
            }

            EventBus.Publish(new SceneLoadCompletedEvent { sceneName = sceneName });
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[SceneLoader] Load cancelled for '{sceneName}'.");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Unloads a previously loaded scene.
    /// </summary>
    public async UniTask UnloadSceneAsync(string sceneName, CancellationToken ct = default)
    {
        if (!IsSceneLoaded(sceneName))
        {
            Debug.Log($"[SceneLoader] Scene '{sceneName}' already unloaded or never loaded — skipping.");
            return;
        }

        EventBus.Publish(new SceneUnloadStartedEvent { sceneName = sceneName });

        var op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] UnloadSceneAsync returned null for '{sceneName}'.");
            return;
        }

        while (!op.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        EventBus.Publish(new SceneUnloadCompletedEvent { sceneName = sceneName });
    }

    /// <summary>
    /// Convenience: load a scene additively, then unload another.
    /// Useful for transitions like "load MainMenu, unload Bootstrapper".
    /// </summary>
    public async UniTask TransitionToSceneAsync(
        string sceneToLoad,
        string sceneToUnload,
        bool setLoadedActive = true,
        IProgress<float> progress = null,
        CancellationToken ct = default)
    {
        await LoadSceneAdditiveAsync(sceneToLoad, setLoadedActive, progress, ct);
        await UnloadSceneAsync(sceneToUnload, ct);
    }

    /// <summary>
    /// Hard scene swap — unloads everything and loads one scene. Use sparingly.
    /// </summary>
    public async UniTask LoadSceneSingleAsync(
        string sceneName,
        IProgress<float> progress = null,
        CancellationToken ct = default)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoader] Already loading. Ignoring single load for '{sceneName}'.");
            return;
        }

        IsLoading = true;
        EventBus.Publish(new SceneLoadStartedEvent { sceneName = sceneName });

        try
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(op.progress);
                EventBus.Publish(new SceneLoadProgressEvent { sceneName = sceneName, progress = op.progress });
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            EventBus.Publish(new SceneLoadCompletedEvent { sceneName = sceneName });
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── Helpers ───

    public bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }
        return false;
    }
}