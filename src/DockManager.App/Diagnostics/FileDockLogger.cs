using DockManager.Core.Diagnostics;

namespace DockManager.App.Diagnostics;

/// <summary>
/// Small rolling file logger. Warnings and errors only by default: an idle dock should not touch the
/// disk.
/// </summary>
public sealed class FileDockLogger : IDockLogger
{
    private const long MaxBytes = 256 * 1024;
    private const long KeepBytes = 64 * 1024;

    private readonly string _path;
    private readonly object _gate = new();
    private readonly bool _includeDebug;

    public FileDockLogger(string path, bool includeDebug = false)
    {
        _path = path;
        _includeDebug = includeDebug;
    }

    public void Log(DockLogLevel level, string message, Exception? error = null)
    {
        if (level == DockLogLevel.Debug && !_includeDebug)
        {
            return;
        }

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}"
            + (error is null ? string.Empty : $" | {error.GetType().Name}: {error.Message}");

        lock (_gate)
        {
            try
            {
                TrimIfNeeded();
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch (Exception)
            {
                // Logging must never take the dock down.
            }
        }
    }

    private void TrimIfNeeded()
    {
        var info = new FileInfo(_path);
        if (!info.Exists || info.Length < MaxBytes)
        {
            return;
        }

        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var start = Math.Max(0, stream.Length - KeepBytes);
        stream.Seek(start, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var tail = reader.ReadToEnd();
        var newline = tail.IndexOf('\n');
        if (newline >= 0)
        {
            tail = tail[(newline + 1)..];
        }

        File.WriteAllText(_path, tail);
    }
}
