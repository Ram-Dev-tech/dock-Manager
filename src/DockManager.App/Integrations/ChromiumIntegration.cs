using System.Windows.Automation;
using DockManager.Core.Diagnostics;
using DockManager.Core.Integrations;
using DockManager.Core.Shell;
using DockManager.Core.Windows;

namespace DockManager.App.Integrations;

/// <summary>
/// Base class for Chromium based browsers. Tabs are read through UI Automation: Chromium exposes a
/// tab strip as <c>TabItem</c> elements once a UIA client attaches, which is the same mechanism the
/// Windows shell uses for browser tabs in Alt+Tab. When the accessibility tree is unavailable the
/// integration degrades to one entry per window (the Phase 1 behaviour) instead of failing.
/// </summary>
public abstract class ChromiumIntegration : IApplicationIntegration
{
    private readonly IntegrationWorker _worker;
    private readonly IWindowActivator _activator;
    private readonly IDockLogger? _logger;

    protected ChromiumIntegration(IntegrationWorker worker, IWindowActivator activator, IDockLogger? logger = null)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        _logger = logger;
    }

    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public abstract IReadOnlyCollection<string> ExecutableNames { get; }

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
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Warn($"{Id} integration failed; falling back to windows.", ex);
            return FallbackWindows(app, options);
        }
    }

    public async Task<bool> ActivateItemAsync(AppContentItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            if (item.Kind == AppContentKind.Tab && item.Locator is not null)
            {
                // Best effort: select the tab inside the browser, then bring the window forward.
                await _worker.RunAsync(_ => SelectTab(item.WindowHandle, item.Locator), cancellationToken)
                    .ConfigureAwait(false);
            }

            return _activator.ActivateWindow(item.WindowHandle);
        }
        catch (Exception ex)
        {
            _logger?.Warn($"{Id} could not activate '{item.Title}'.", ex);
            return _activator.ActivateWindow(item.WindowHandle);
        }
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
                "New tab",
                new LaunchRequest(app.ExecutablePath, LaunchVerb.Execute)),
            new QuickAction(
                "New window",
                new LaunchRequest(app.ExecutablePath, LaunchVerb.Execute) { Arguments = "--new-window" }),
        ];
    }

    private IReadOnlyList<AppContentItem> ReadContent(RunningApp app, IntegrationOptions options)
    {
        var items = new List<AppContentItem>();

        foreach (var window in app.Windows)
        {
            var tabs = ReadTabs(window.Handle);
            if (tabs.Count == 0)
            {
                items.Add(WindowItem(app, window, options));
                continue;
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                items.Add(new AppContentItem(
                    $"{app.Key}:{window.Handle}:{i}",
                    tab.Title,
                    AppContentKind.Tab,
                    window.Handle)
                {
                    IsActive = tab.IsSelected && window.Handle == options.ForegroundWindow,
                    Locator = tab.Title,
                    IconKey = app.ExecutablePath,
                });
            }
        }

        return items;
    }

    private static AppContentItem WindowItem(RunningApp app, WindowInfo window, IntegrationOptions options)
        => new(
            $"{app.Key}:{window.Handle}",
            string.IsNullOrWhiteSpace(window.Title) ? app.DisplayName : window.Title,
            AppContentKind.Window,
            window.Handle)
        {
            Subtitle = window.IsMinimized ? "Minimized" : null,
            IsActive = window.Handle == options.ForegroundWindow,
            IconKey = app.ExecutablePath,
        };

    private static IReadOnlyList<AppContentItem> FallbackWindows(RunningApp app, IntegrationOptions options)
        => app.Windows.Select(window => WindowItem(app, window, options)).ToList();

    private sealed record TabInfo(string Title, bool IsSelected);

    private static List<TabInfo> ReadTabs(IntPtr hwnd)
    {
        var tabs = new List<TabInfo>();

        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root is null)
            {
                return tabs;
            }

            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);
            var found = root.FindAll(TreeScope.Descendants, condition);

            foreach (AutomationElement element in found)
            {
                string name;
                bool selected;
                try
                {
                    name = element.Current.Name;
                    selected = element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern)
                        && pattern is SelectionItemPattern selection
                        && selection.Current.IsSelected;
                }
                catch (ElementNotAvailableException)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    tabs.Add(new TabInfo(name, selected));
                }
            }
        }
        catch (Exception)
        {
            // The browser may refuse or be mid-shutdown; the caller falls back to windows.
        }

        return tabs;
    }

    private static bool SelectTab(IntPtr hwnd, string locator)
    {
        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root is null)
            {
                return false;
            }

            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);
            foreach (AutomationElement element in root.FindAll(TreeScope.Descendants, condition))
            {
                try
                {
                    if (!string.Equals(element.Current.Name, locator, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern)
                        && pattern is SelectionItemPattern selection)
                    {
                        selection.Select();
                        return true;
                    }
                }
                catch (ElementNotAvailableException)
                {
                    continue;
                }
            }
        }
        catch (Exception)
        {
            // Fall through to window-level activation.
        }

        return false;
    }
}

/// <summary>Google Chrome.</summary>
public sealed class ChromeIntegration : ChromiumIntegration
{
    public ChromeIntegration(IntegrationWorker worker, IWindowActivator activator, IDockLogger? logger = null)
        : base(worker, activator, logger)
    {
    }

    public override string Id => "chrome";

    public override string DisplayName => "Chrome";

    public override IReadOnlyCollection<string> ExecutableNames => ["chrome.exe"];
}

/// <summary>Microsoft Edge.</summary>
public sealed class EdgeIntegration : ChromiumIntegration
{
    public EdgeIntegration(IntegrationWorker worker, IWindowActivator activator, IDockLogger? logger = null)
        : base(worker, activator, logger)
    {
    }

    public override string Id => "edge";

    public override string DisplayName => "Edge";

    public override IReadOnlyCollection<string> ExecutableNames => ["msedge.exe"];
}
