using DockManager.Core.Shell;

namespace DockManager.Core.Windows;

/// <summary>
/// A single top level window as seen by the dock. <see cref="Handle"/> is an <c>HWND</c> on
/// Windows; it is stored as <see cref="IntPtr"/> so the core stays platform neutral and testable.
/// </summary>
public sealed record WindowInfo(IntPtr Handle, string ExecutablePath, int ProcessId)
{
    public string Title { get; init; } = string.Empty;

    public bool IsMinimized { get; init; }

    /// <summary>Comparison key used to group windows into one application entry.</summary>
    public string MatchKey => PathNormalizer.NormalizeKey(ExecutablePath);
}
