using System.Threading;
using UnityEngine;

public static class AppLifetime
{
    private static readonly CancellationTokenSource QuitCts = new();

    public static CancellationToken Token => QuitCts.Token;

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        Application.quitting += OnQuit;
    }

    private static void OnQuit()
    {
        if (!QuitCts.IsCancellationRequested)
            QuitCts.Cancel();
    }
}