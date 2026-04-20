public struct AuthStartedEvent
{
    public string method;
}

public struct AuthProgressEvent
{
    public float progress;
    public string currentStep;
}

public struct AuthCompletedEvent
{
    public string playerId;
    public string displayHint;
    public bool wasAnonymous;
}

public struct AuthFailedEvent
{
    public string errorMessage;
    public AuthFailureReason reason;
}

public enum AuthFailureReason
{
    ServicesInitFailed,
    GpgsSignInFailed,
    AnonymousSignInFailed,
    NetworkError,
    Cancelled,
    Unknown
}