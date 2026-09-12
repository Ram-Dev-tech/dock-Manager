namespace DockManager.Core.Persistence;

/// <summary>Per user storage under <c>%APPDATA%\DockManager</c>.</summary>
public sealed class DefaultStoragePaths : IStoragePaths
{
    public DefaultStoragePaths(string? rootOverride = null)
    {
        var root = string.IsNullOrWhiteSpace(rootOverride)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DockManager")
            : rootOverride!;

        Directory = root;
        ItemsFile = Path.Combine(root, "pinned-items.json");
        SettingsFile = Path.Combine(root, "settings.json");
        LogFile = Path.Combine(root, "dock-manager.log");
    }

    public string Directory { get; }

    public string ItemsFile { get; }

    public string SettingsFile { get; }

    public string LogFile { get; }
}
