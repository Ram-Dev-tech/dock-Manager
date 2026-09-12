namespace DockManager.Core.Shell;

/// <summary>The resolved contents of a Windows shortcut (.lnk).</summary>
public sealed record ShortcutTarget(string TargetPath)
{
    public string? Arguments { get; init; }

    public string? WorkingDirectory { get; init; }

    public string? Description { get; init; }
}

/// <summary>
/// Resolves <c>.lnk</c> files. Pinning a shortcut stores the shortcut itself (so the app is
/// launched exactly like the Start menu would) but also stores the resolved executable so the dock
/// can match it against running windows.
/// </summary>
public interface IShortcutResolver
{
    bool TryResolve(string shortcutPath, out ShortcutTarget? target);
}
