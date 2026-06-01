using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Feature-tagged logger. Each feature is a <see cref="LogCategory"/> channel that can be
/// independently enabled and routed to the Unity console and/or a per-feature file on disk
/// (<c>Application.persistentDataPath/GameLogs/&lt;category&gt;.log</c>).
///
/// The point: when you want to trace one feature — e.g. every coin earned/spent — you flip on
/// that category, reproduce, then pull a clean, greppable file containing only that feature's
/// events instead of digging through the whole console.
///
/// Usage:
///   GameLogger.Log(LogCategory.Economy, "spent 200 gold on cannon upgrade");
///   GameLogger.SetEnabled(LogCategory.Boss, true);   // turn a channel on at runtime
///   EconomyLog.Spend(CurrencyType.Gold, 200, balanceAfter: 1300, reason: "cannon_upgrade");
///
/// Files live at GameLogger.LogDirectory. In the editor: Tools ▸ Game Logger ▸ Open Log Folder.
/// </summary>
public static class GameLogger
{
    // ==================== Configuration ====================

    /// <summary>Categories enabled by default. Add a flag here to trace that feature out of the box.</summary>
    private const LogCategory DefaultEnabled = LogCategory.Economy;

    /// <summary>Mirror every enabled log to the Unity console.</summary>
    public static bool ConsoleEnabled { get; set; } = true;

    /// <summary>Persist every enabled log to its per-category file under <see cref="LogDirectory"/>.</summary>
    public static bool FileEnabled { get; set; } = true;

    private static LogCategory _enabled = DefaultEnabled;

    // ==================== Paths ====================

    public static string LogDirectory => Path.Combine(Application.persistentDataPath, "GameLogs");

    public static string GetLogPath(LogCategory category) =>
        Path.Combine(LogDirectory, category.ToString().ToLowerInvariant() + ".log");

    // ==================== Enable / Disable ====================

    public static bool IsEnabled(LogCategory category) => (_enabled & category) != 0;

    public static void SetEnabled(LogCategory category, bool enabled)
    {
        if (enabled) _enabled |= category;
        else _enabled &= ~category;
    }

    public static void SetOnly(LogCategory categories)
    {
        _enabled = categories;
    }

    // ==================== Public Log API ====================

    public static void Log(LogCategory category, string message, Object context = null)
        => Write(category, LogLevel.Info, message, context);

    public static void LogWarning(LogCategory category, string message, Object context = null)
        => Write(category, LogLevel.Warning, message, context);

    public static void LogError(LogCategory category, string message, Object context = null)
        => Write(category, LogLevel.Error, message, context);

    // ==================== Internals ====================

    private enum LogLevel { Info, Warning, Error }

    private static readonly object _ioLock = new object();
    private static readonly Dictionary<LogCategory, StreamWriter> _writers = new();
    private static bool _directoryReady;

    private static void Write(LogCategory category, LogLevel level, string message, Object context)
    {
        if (!IsEnabled(category)) return;

        string tag = category.ToString().ToUpperInvariant();
        string line = $"[{Now()}] [{tag}] {LevelTag(level)}{message}";

        if (ConsoleEnabled)
        {
            string consoleLine = $"[{tag}] {message}";
            switch (level)
            {
                case LogLevel.Warning: Debug.LogWarning(consoleLine, context); break;
                case LogLevel.Error: Debug.LogError(consoleLine, context); break;
                default: Debug.Log(consoleLine, context); break;
            }
        }

        if (FileEnabled)
            WriteToFile(category, line);
    }

    private static void WriteToFile(LogCategory category, string line)
    {
        try
        {
            lock (_ioLock)
            {
                if (!_writers.TryGetValue(category, out var writer))
                {
                    EnsureDirectory();
                    writer = new StreamWriter(GetLogPath(category), append: true, Encoding.UTF8)
                    {
                        AutoFlush = false
                    };
                    _writers[category] = writer;
                }

                writer.WriteLine(line);
            }
        }
        catch (Exception ex)
        {
            // Never let logging crash gameplay.
            Debug.LogWarning($"[GameLogger] file write failed for {category}: {ex.Message}");
        }
    }

    private static void EnsureDirectory()
    {
        if (_directoryReady) return;
        Directory.CreateDirectory(LogDirectory);
        _directoryReady = true;
    }

    /// <summary>Flush all open files to disk. Called automatically on pause/quit.</summary>
    public static void Flush()
    {
        lock (_ioLock)
        {
            foreach (var writer in _writers.Values)
            {
                try { writer.Flush(); }
                catch { /* ignore */ }
            }
        }
    }

    internal static void CloseAll()
    {
        lock (_ioLock)
        {
            foreach (var writer in _writers.Values)
            {
                try { writer.Flush(); writer.Dispose(); }
                catch { /* ignore */ }
            }
            _writers.Clear();
        }
    }

    /// <summary>Delete all on-disk log files. Open writers are closed first.</summary>
    public static void ClearFiles()
    {
        CloseAll();
        try
        {
            if (Directory.Exists(LogDirectory))
                Directory.Delete(LogDirectory, recursive: true);
            _directoryReady = false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameLogger] clear failed: {ex.Message}");
        }
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

    private static string LevelTag(LogLevel level) => level switch
    {
        LogLevel.Warning => "WARN  ",
        LogLevel.Error => "ERROR ",
        _ => string.Empty
    };

    // ==================== Lifecycle ====================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("[GameLogger]");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameLoggerRunner>();

        Log(LogCategory.Economy, $"Logger initialized. Files: {LogDirectory}");
    }

    /// <summary>Drives periodic flush and flushes on pause/quit so device logs survive a kill.</summary>
    private sealed class GameLoggerRunner : MonoBehaviour
    {
        private float _flushTimer;
        private const float FlushInterval = 2f;

        private void Update()
        {
            _flushTimer += Time.unscaledDeltaTime;
            if (_flushTimer >= FlushInterval)
            {
                _flushTimer = 0f;
                Flush();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit() => CloseAll();
    }
}

/// <summary>
/// Feature channels. <c>[Flags]</c> so multiple can be enabled at once and combined as a bitmask.
/// Add a value here when you want to start tracing a new feature, then sprinkle
/// <c>GameLogger.Log(LogCategory.YourFeature, ...)</c> calls at its key points.
/// </summary>
[Flags]
public enum LogCategory
{
    None = 0,
    Economy = 1 << 0,
    Boot = 1 << 1,
    Progression = 1 << 2,
    Gameplay = 1 << 3,
    Boss = 1 << 4,
    Ads = 1 << 5,
    Network = 1 << 6,
    Powerup = 1 << 7,

    All = ~0
}
