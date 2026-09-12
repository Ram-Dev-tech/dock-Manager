using DockManager.Core.Diagnostics;
using DockManager.Core.Integrations;
using DockManager.Core.Shell;
using DockManager.Core.Windows;

namespace DockManager.App.Integrations;

/// <summary>
/// Visual Studio Code integration. Every VS Code window becomes a navigation target named after its
/// project/workspace, with the open document as the subtitle, parsed from the window title. Title
/// parsing is intentionally the only source: it is cheap, reliable and needs no access to the
/// editor internals.
/// </summary>
public sealed class VSCodeIntegration : IApplicationIntegration
{
    private readonly IntegrationWorker _worker;
    private readonly IWindowActivator _activator;
    private readonly IDockLogger? _logger;

    public VSCodeIntegration(IntegrationWorker worker, IWindowActivator activator, IDockLogger? logger = null)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        _logger = logger;
    }

    public string Id => "vscode";

    public string DisplayName => "VS Code";

    public IReadOnlyCollection<string> ExecutableNames => ["code.exe", "code_insiders.exe"];

    public bool SupportsItemLevelNavigation => true;

    public async Task<IReadOnlyList<AppContentItem>> GetContentAsync(
        RunningApp app,
        IntegrationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);

        try
        {
            return await _worker.RunAsync(_ => ReadContent(app, options), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ObjectDisposedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Warn("VS Code integration failed; falling back to windows.", ex);
            return app.Windows
                .Select(window => new AppContentItem(
                    $"{app.Key}:{window.Handle}",
                    string.IsNullOrWhiteSpace(window.Title) ? app.DisplayName : window.Title,
                    AppContentKind.Window,
                    window.Handle))
                .ToList();
        }
    }

    public Task<bool> ActivateItemAsync(AppContentItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Task.FromResult(item.WindowHandle != IntPtr.Zero && _activator.ActivateWindow(item.WindowHandle));
    }

    public IReadOnlyList<QuickAction> GetQuickActions(RunningApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (string.IsNullOrWhiteSpace(app.ExecutablePath))
        {
            return [];
        }

        return
        [
            new QuickAction(
                "New window",
                new LaunchRequest(app.ExecutablePath, LaunchVerb.Execute) { Arguments = "--new-window" }),
        ];
    }

    private static IReadOnlyList<AppContentItem> ReadContent(RunningApp app, IntegrationOptions options)
    {
        var items = new List<AppContentItem>(app.Windows.Count);

        foreach (var window in app.Windows)
        {
            var (document, project) = VSCodeTitleParser.Parse(window.Title);
            var title = project ?? document
                ?? (string.IsNullOrWhiteSpace(window.Title) ? app.DisplayName : window.Title);

            items.Add(new AppContentItem(
                $"{app.Key}:{window.Handle}",
                title,
                project is not null ? AppContentKind.Project : AppContentKind.Window,
                window.Handle)
            {
                Subtitle = document ?? (window.IsMinimized ? "Minimized" : null),
                IsActive = window.Handle == options.ForegroundWindow,
                IconKey = app.ExecutablePath,
            });
        }

        return items;
    }
}
