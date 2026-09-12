using DockManager.Core.Windows;

namespace DockManager.Core.Integrations;

/// <summary>
/// The fallback every application gets: its open windows, with activation and minimize/restore.
/// This is what keeps Phase 2 from ever regressing Phase 1 behaviour.
/// </summary>
public sealed class GenericIntegration : IApplicationIntegration
{
    private readonly IWindowActivator _activator;

    public GenericIntegration(IWindowActivator activator)
    {
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
    }

    public string Id => "generic";

    public string DisplayName => "Windows";

    public IReadOnlyCollection<string> ExecutableNames => [];

    public bool SupportsItemLevelNavigation => false;

    public Task<IReadOnlyList<AppContentItem>> GetContentAsync(
        RunningApp app,
        IntegrationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        List<AppContentItem> items;

        if (options.GroupMultipleWindows && app.Windows.Count > 1)
        {
            items =
            [
                new AppContentItem($"{app.Key}:group", app.DisplayName, AppContentKind.Window, app.PreferredWindow)
                {
                    Subtitle = $"{app.Windows.Count} windows",
                    IsActive = app.Windows.Any(window => window.Handle == options.ForegroundWindow),
                },
            ];
        }
        else
        {
            items = app.Windows
                .Select((window, index) => new AppContentItem(
                    $"{app.Key}:w{index}",
                    string.IsNullOrWhiteSpace(window.Title) ? app.DisplayName : window.Title,
                    AppContentKind.Window,
                    window.Handle)
                {
                    Subtitle = window.IsMinimized ? "Minimized" : null,
                    IsActive = window.Handle == options.ForegroundWindow,
                })
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<AppContentItem>>(items);
    }

    public Task<bool> ActivateItemAsync(AppContentItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        var activated = item.WindowHandle != IntPtr.Zero && _activator.ActivateWindow(item.WindowHandle);
        return Task.FromResult(activated);
    }
}
