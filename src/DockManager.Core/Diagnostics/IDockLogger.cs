namespace DockManager.Core.Diagnostics;

public enum DockLogLevel
{
    Debug = 0,
    Info,
    Warning,
    Error,
}

/// <summary>Minimal logging seam so the core can report problems without owning a log sink.</summary>
public interface IDockLogger
{
    void Log(DockLogLevel level, string message, Exception? error = null);
}

public static class DockLoggerExtensions
{
    public static void Debug(this IDockLogger? logger, string message) => logger?.Log(DockLogLevel.Debug, message);

    public static void Info(this IDockLogger? logger, string message) => logger?.Log(DockLogLevel.Info, message);

    public static void Warn(this IDockLogger? logger, string message, Exception? error = null)
        => logger?.Log(DockLogLevel.Warning, message, error);

    public static void Error(this IDockLogger? logger, string message, Exception? error = null)
        => logger?.Log(DockLogLevel.Error, message, error);
}

/// <summary>Throws everything away; handy in tests.</summary>
public sealed class NullDockLogger : IDockLogger
{
    public static NullDockLogger Instance { get; } = new();

    public void Log(DockLogLevel level, string message, Exception? error = null)
    {
    }
}
