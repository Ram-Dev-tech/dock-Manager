namespace DockManager.Core.Integrations;

/// <summary>What an application content entry represents.</summary>
public enum AppContentKind
{
    Window = 0,
    Tab,
    Document,
    Project,
    Folder,
}

/// <summary>
/// One entry shown in the secondary hover panel: a browser tab, an open document, an Explorer
/// folder or a plain window. Deliberately a dumb record so integrations can produce it from any
/// technology (UI Automation, COM, title parsing) without the panel caring.
/// </summary>
public sealed record AppContentItem(string Id, string Title, AppContentKind Kind, IntPtr WindowHandle)
{
    /// <summary>Second line, e.g. a URL host, folder path or "Minimized".</summary>
    public string? Subtitle { get; init; }

    /// <summary>Whether this entry is the currently active one (subtle highlight, no glow).</summary>
    public bool IsActive { get; init; }

    /// <summary>Path used to resolve the entry's icon.</summary>
    public string? IconKey { get; init; }

    /// <summary>
    /// Integration specific hint used to re-locate the entry when activating it later, e.g. a tab
    /// title. UI elements themselves are never stored because they go stale as apps change.
    /// </summary>
    public string? Locator { get; init; }
}
