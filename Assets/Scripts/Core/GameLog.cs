using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Lightweight logging facade for gameplay code.
///
/// <para>
/// Calls to <see cref="Log"/>, <see cref="LogWarning"/> and <see cref="LogFormat"/> are
/// stripped <b>entirely</b> from non-development player builds by the <see cref="ConditionalAttribute"/>:
/// when neither <c>UNITY_EDITOR</c> nor <c>DEVELOPMENT_BUILD</c> is defined, the C# compiler
/// removes the call site — including evaluation of any arguments. That means a hot-path call
/// like <c>GameLog.Log($"took {dmg} dmg")</c> performs <b>zero</b> string interpolation,
/// allocation, or stack-trace capture in a release build.
/// </para>
///
/// <para>
/// <see cref="LogError"/> and <see cref="LogException"/> are deliberately NOT conditional —
/// they pass straight through to <see cref="UnityEngine.Debug"/> so genuine errors still
/// surface in production logs and crash reporting.
/// </para>
/// </summary>
public static class GameLog
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message) => Debug.Log(message);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, Object context) => Debug.Log(message, context);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message) => Debug.LogWarning(message);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, Object context) => Debug.LogWarning(message, context);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(string format, params object[] args) => Debug.LogFormat(format, args);

    // ── NOT stripped: errors/exceptions must reach production logs & crash reports ──
    public static void LogError(object message) => Debug.LogError(message);
    public static void LogError(object message, Object context) => Debug.LogError(message, context);
    public static void LogException(System.Exception exception) => Debug.LogException(exception);
}
