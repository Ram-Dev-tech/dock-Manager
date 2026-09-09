using DockManager.Core.Diagnostics;

namespace DockManager.Core.Settings;

public sealed class SettingsChangedEventArgs(DockSettings settings) : EventArgs
{
    public DockSettings Settings { get; } = settings;
}

/// <summary>
/// Holds the current settings, validates every change and persists it. The UI binds to
/// <see cref="Current"/> and calls <see cref="Update"/> so a change is always clamped, saved and
/// broadcast in one step.
/// </summary>
public sealed class SettingsStore
{
    private readonly ISettingsRepository _repository;
    private readonly IDockLogger? _logger;

    public SettingsStore(ISettingsRepository repository, IDockLogger? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger;
    }

    public DockSettings Current { get; private set; } = new DockSettings().Sanitized();

    public event EventHandler<SettingsChangedEventArgs>? Changed;

    /// <summary>Loads from disk, falling back to defaults when the file is missing or corrupt.</summary>
    public DockSettings Load()
    {
        try
        {
            var loaded = _repository.Load();
            Current = (loaded ?? new DockSettings()).Sanitized();
        }
        catch (Exception ex)
        {
            _logger.Error("Falling back to default settings.", ex);
            Current = new DockSettings().Sanitized();
        }

        return Current;
    }

    /// <summary>Applies a mutation, clamps the result, saves it and raises <see cref="Changed"/>.</summary>
    public DockSettings Update(Action<DockSettings> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        var draft = Current.Clone();
        mutate(draft);
        var sanitized = draft.Sanitized();
        Current = sanitized;

        try
        {
            _repository.Save(sanitized);
        }
        catch (Exception ex)
        {
            _logger.Error("Could not save settings.", ex);
        }

        Changed?.Invoke(this, new SettingsChangedEventArgs(sanitized));
        return sanitized;
    }

    /// <summary>Writes the current settings to disk without changing them.</summary>
    public void Save()
    {
        try
        {
            _repository.Save(Current);
        }
        catch (Exception ex)
        {
            _logger.Error("Could not save settings.", ex);
        }
    }
}
