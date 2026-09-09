using System.Windows;
using System.Windows.Threading;
using DockManager.App.Composition;
using DockManager.App.Diagnostics;
using DockManager.App.Dock;
using DockManager.App.Settings;
using DockManager.App.Shell;
using DockManager.App.Ui;
using DockManager.App.Windows;
using DockManager.Core.Diagnostics;
using DockManager.Core.Items;
using DockManager.Core.Persistence;
using DockManager.Core.Settings;
using DockManager.Core.Shell;
using DockManager.Core.Ui;

namespace DockManager.App;

/// <summary>
/// Composition root. Builds the service graph, owns the dock window, the tray icon and the settings
/// window, and wires the lifetime events. The dock itself is a tray application: no taskbar entry,
/// and it only quits explicitly.
/// </summary>
public partial class App : Application
{
    private DockServices? _services;
    private DockWindow? _dock;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        InstallExceptionHandlers();
        BuildServices();

        _dock = new DockWindow(_services!);
        _dock.SettingsRequested += (_, _) => OpenSettings();
        _dock.QuitRequested += (_, _) => Shutdown();

        _services!.Tray.ShowDockRequested += (_, _) => _dock.Reveal();
        _services.Tray.SettingsRequested += (_, _) => OpenSettings();
        _services.Tray.QuitRequested += (_, _) => Shutdown();

        _dock.Show();
    }

    private void InstallExceptionHandlers()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            _services?.Logger.Error("Unhandled UI exception.", e.Exception);
            e.Handled = true; // Keep the dock alive; a bad click should not kill it.
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            _services?.Logger.Error("Unhandled exception.", e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _services?.Logger.Error("Unobserved task exception.", e.Exception);
            e.SetObserved();
        };
    }

    private void BuildServices()
    {
        var paths = new DefaultStoragePaths();
        var logger = new FileDockLogger(paths.LogFile);

        var fileSystem = new PhysicalFileSystemProbe();
        var shortcuts = new ShellLinkResolver(logger);
        var shell = new ShellLauncher(logger);
        var icons = new IconProvider(logger);

        var settings = new SettingsStore(new JsonSettingsRepository(paths, logger), logger);
        settings.Load();

        var items = new ItemStore();
        var itemRepository = new JsonItemRepository(paths, logger);
        items.ReplaceAll(itemRepository.Load());
        items.Changed += (_, _) => itemRepository.Save(items.Items);

        var windows = new WindowManager(logger);
        var startup = new StartupRegistration(logger: logger);
        var viewModel = new DockViewModel(items, fileSystem, settings.Current.CreateLayoutMetrics());

        settings.Changed += (_, e) =>
        {
            viewModel.ApplyMetrics(e.Settings.CreateLayoutMetrics());
            ApplyStartupRegistration(e.Settings.LaunchAtStartup, startup, logger);
        };

        ApplyStartupRegistration(settings.Current.LaunchAtStartup, startup, logger);

        var tray = new TrayIcon(logger);

        _services = new DockServices
        {
            Logger = logger,
            FileSystem = fileSystem,
            Shortcuts = shortcuts,
            Shell = shell,
            Icons = icons,
            Settings = settings,
            Items = items,
            Windows = windows,
            Startup = startup,
            ViewModel = viewModel,
            Tray = tray,
        };

        tray.Install();
    }

    private static void ApplyStartupRegistration(bool desired, IStartupRegistration registration, IDockLogger logger)
    {
        try
        {
            if (desired && !registration.IsEnabled)
            {
                registration.Enable();
            }
            else if (!desired && registration.IsEnabled)
            {
                registration.Disable();
            }
        }
        catch (Exception ex)
        {
            logger.Warn("Could not update the startup registration.", ex);
        }
    }

    private void OpenSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_services!.Settings, _services.Startup);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Tray.Dispose();
        if (_services is not null)
        {
            _services.Settings.Save();
        }

        base.OnExit(e);
    }
}
