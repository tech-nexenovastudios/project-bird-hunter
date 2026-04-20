public struct DataLoadStartedEvent { }

public struct DataLoadProgressEvent
{
    public float progress;
    public string currentStep;
}

public struct DataLoadCompletedEvent { }

public struct DataLoadFailedEvent
{
    public string errorMessage;
    public bool canRetry;
}
