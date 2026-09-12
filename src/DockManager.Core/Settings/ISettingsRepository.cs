namespace DockManager.Core.Settings;

/// <summary>Reads and writes the settings document.</summary>
public interface ISettingsRepository
{
    string FilePath { get; }

    /// <summary>Returns the stored settings or <c>null</c> when nothing has been saved yet.</summary>
    DockSettings? Load();

    void Save(DockSettings settings);
}
