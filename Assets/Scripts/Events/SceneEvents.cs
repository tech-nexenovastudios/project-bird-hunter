public struct SceneLoadStartedEvent
{
    public string sceneName;
}

public struct SceneLoadProgressEvent
{
    public string sceneName;
    public float progress;
}

public struct SceneLoadCompletedEvent
{
    public string sceneName;
}

public struct SceneUnloadStartedEvent
{
    public string sceneName;
}

public struct SceneUnloadCompletedEvent
{
    public string sceneName;
}