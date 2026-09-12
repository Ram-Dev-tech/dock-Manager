using DockManager.Core.Shell;

namespace DockManager.Core.Windows;

/// <summary>
/// Immutable lookup over the currently running applications, rebuilt whenever the window list
/// changes. Lookups are O(1) so the dock can resolve every pinned item on each refresh without
/// scanning the process list repeatedly.
/// </summary>
public sealed class RunningAppIndex
{
    public static RunningAppIndex Empty { get; } = new([], []);

    private readonly Dictionary<string, RunningApp> _byKey;
    private readonly Dictionary<string, RunningApp> _byFileName;
    private readonly Dictionary<IntPtr, RunningApp> _byHandle;

    private RunningAppIndex(IReadOnlyList<RunningApp> apps, IReadOnlyList<WindowInfo> windows)
    {
        Apps = apps;
        Windows = windows;

        _byKey = new Dictionary<string, RunningApp>(StringComparer.Ordinal);
        _byFileName = new Dictionary<string, RunningApp>(StringComparer.Ordinal);
        _byHandle = new Dictionary<IntPtr, RunningApp>();

        foreach (var app in apps)
        {
            _byKey.TryAdd(app.Key, app);
            var fileName = PathNormalizer.FileName(app.ExecutablePath).ToLowerInvariant();
            if (fileName.Length > 0)
            {
                _byFileName.TryAdd(fileName, app);
            }

            foreach (var window in app.Windows)
            {
                _byHandle.TryAdd(window.Handle, app);
            }
        }
    }

    public IReadOnlyList<RunningApp> Apps { get; }

    public IReadOnlyList<WindowInfo> Windows { get; }

    public bool Any => Apps.Count > 0;

    /// <summary>Builds the index, grouping windows by executable path.</summary>
    public static RunningAppIndex Build(IEnumerable<WindowInfo>? windows)
    {
        if (windows is null)
        {
            return Empty;
        }

        var materialized = windows.Where(window => window is not null).ToList();
        var grouped = new Dictionary<string, List<WindowInfo>>(StringComparer.Ordinal);

        foreach (var window in materialized)
        {
            var key = window.MatchKey;
            if (key.Length == 0)
            {
                // No executable path (access denied): keep the window under its process so it is
                // still switchable, but it will not match a pinned item.
                key = $"pid:{window.ProcessId}";
            }

            if (!grouped.TryGetValue(key, out var list))
            {
                list = [];
                grouped[key] = list;
            }

            list.Add(window);
        }

        var apps = grouped
            .Select(pair => new RunningApp
            {
                Key = pair.Key,
                ExecutablePath = pair.Value[0].ExecutablePath,
                Windows = pair.Value,
                DisplayName = PathNormalizer.DisplayName(pair.Value[0].ExecutablePath),
            })
            .ToList();

        return new RunningAppIndex(apps, materialized);
    }

    /// <summary>Finds the running application for an executable path, tolerating path differences.</summary>
    public RunningApp? FindByExecutable(string? executablePath)
    {
        var key = PathNormalizer.NormalizeKey(executablePath);
        if (key.Length == 0)
        {
            return null;
        }

        if (_byKey.TryGetValue(key, out var exact))
        {
            return exact;
        }

        var fileName = PathNormalizer.FileName(key).ToLowerInvariant();
        return fileName.Length > 0 && _byFileName.TryGetValue(fileName, out var byName) ? byName : null;
    }

    public bool IsRunning(string? executablePath) => FindByExecutable(executablePath) is not null;

    public RunningApp? FindByWindowHandle(IntPtr handle)
        => handle != IntPtr.Zero && _byHandle.TryGetValue(handle, out var app) ? app : null;
}
