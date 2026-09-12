namespace DockManager.Core.Persistence;

/// <summary>Where the dock keeps its state. Isolated so tests can point at a temp folder.</summary>
public interface IStoragePaths
{
    string Directory { get; }

    string ItemsFile { get; }

    string SettingsFile { get; }

    string LogFile { get; }
}
