using System.Text.Json;
using DockManager.Core.Diagnostics;
using DockManager.Core.Settings;

namespace DockManager.Core.Persistence;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly IDockLogger? _logger;

    public JsonSettingsRepository(IStoragePaths paths, IDockLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _logger = logger;
        FilePath = paths.SettingsFile;
    }

    public string FilePath { get; }

    public DockSettings? Load()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<DockSettings>(json, JsonFile.Options);
        }
        catch (JsonException ex)
        {
            _logger.Warn("Settings file was corrupt and has been moved aside.", ex);
            _logger.Info($"Corrupt settings moved to: {AtomicFileWriter.MoveAside(FilePath)}");
            return null;
        }
    }

    public void Save(DockSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var json = JsonSerializer.Serialize(settings.Sanitized(), JsonFile.Options);
        AtomicFileWriter.Write(FilePath, json);
    }
}
