using DockManager.Core.Settings;

namespace DockManager.Core.Tests.Support;

public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private DockSettings? _stored;

    public string FilePath => "memory://settings.json";

    public int SaveCount { get; private set; }

    public DockSettings? Load() => _stored?.Clone();

    public void Save(DockSettings settings)
    {
        _stored = settings.Clone();
        SaveCount++;
    }
}

public sealed class ThrowingSettingsRepository : ISettingsRepository
{
    public string FilePath => "memory://boom";

    public DockSettings? Load() => throw new IOException("disk on fire");

    public void Save(DockSettings settings) => throw new IOException("disk on fire");
}
