using DockManager.App.Integrations;
using DockManager.App.Ui;
using DockManager.Core.Diagnostics;
using DockManager.Core.Settings;
using DockManager.Core.Shell;
using DockManager.Core.Ui;
using DockManager.Core.Windows;
using DockManager.Core.Items;
using DockManager.Core.Integrations;

namespace DockManager.App.Composition;

/// <summary>
/// The composition root's product: every long lived service the dock needs, assembled once in
/// <c>App</c> and handed to the windows. Keeping the graph in one bag makes the windows trivially
/// constructible and the whole thing mockable in future tests.
/// </summary>
public sealed class DockServices
{
    public required IDockLogger Logger { get; init; }

    public required IFileSystemProbe FileSystem { get; init; }

    public required IShortcutResolver Shortcuts { get; init; }

    public required IShellLauncher Shell { get; init; }

    public required IIconProvider Icons { get; init; }

    public required SettingsStore Settings { get; init; }

    public required ItemStore Items { get; init; }

    public required IWindowManager Windows { get; init; }

    public required IStartupRegistration Startup { get; init; }

    public required DockViewModel ViewModel { get; init; }

    public required TrayIcon Tray { get; init; }

    public required ApplicationManager Applications { get; init; }

    public required WindowPreviewService Previews { get; init; }

    /// <summary>
    /// Resolves the integrations the user switched off in settings. Recomputed per call: the set is
    /// tiny and this avoids stale caches after settings changes.
    /// </summary>
    public IReadOnlySet<string> DisabledIntegrationSet()
        => new HashSet<string>(Settings.Current.DisabledIntegrations, StringComparer.Ordinal);
}
