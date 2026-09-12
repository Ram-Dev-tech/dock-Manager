using DockManager.Core.Shell;

namespace DockManager.Core.Windows;

/// <summary>
/// One application level entry built from all top level windows that belong to the same
/// executable. Phase 1 treats every window of an app as a single entry: no tab extraction.
/// </summary>
public sealed class RunningApp
{
    public required string Key { get; init; }

    public required string ExecutablePath { get; init; }

    public required IReadOnlyList<WindowInfo> Windows { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>True when every window of the application is minimized.</summary>
    public bool IsMinimized => Windows.Count > 0 && Windows.All(window => window.IsMinimized);

    public int WindowCount => Windows.Count;

    public int ProcessId => Windows.Count > 0 ? Windows[0].ProcessId : 0;

    /// <summary>
    /// The window that should be brought forward: the first visible one, or the first minimized
    /// one when the app has no visible window.
    /// </summary>
    public IntPtr PreferredWindow
    {
        get
        {
            foreach (var window in Windows)
            {
                if (!window.IsMinimized)
                {
                    return window.Handle;
                }
            }

            return Windows.Count > 0 ? Windows[0].Handle : IntPtr.Zero;
        }
    }

    public override string ToString() => $"{DisplayName} ({WindowCount} window(s))";
}
