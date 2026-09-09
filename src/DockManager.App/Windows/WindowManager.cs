using DockManager.Core.Diagnostics;
using DockManager.Core.Windows;

namespace DockManager.App.Windows;

/// <summary>
/// The single entry point the dock uses for everything window related: what is running, what is in
/// the foreground, and switching to an application.
/// </summary>
public sealed class WindowManager : IWindowManager
{
    private readonly WindowEnumerator _enumerator;
    private readonly WindowActivator _activator;
    private readonly IDockLogger? _logger;

    public WindowManager(IDockLogger? logger = null, ProcessResolver? processResolver = null)
    {
        _logger = logger;
        _enumerator = new WindowEnumerator(processResolver);
        _activator = new WindowActivator(logger);
    }

    public RunningAppIndex GetRunningApps()
    {
        try
        {
            return RunningAppIndex.Build(_enumerator.GetTopLevelWindows());
        }
        catch (Exception ex)
        {
            _logger?.Error("Enumerating windows failed; reporting an empty running list.", ex);
            return RunningAppIndex.Empty;
        }
    }

    public IntPtr GetForegroundWindowHandle()
    {
        try
        {
            return Win32.GetForegroundWindow();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return IntPtr.Zero;
        }
    }

    public bool Activate(RunningApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var hwnd = app.PreferredWindow;
        if (hwnd == IntPtr.Zero)
        {
            _logger?.Warn($"{app.DisplayName} has no window to activate.");
            return false;
        }

        var activated = _activator.Activate(hwnd);
        if (!activated)
        {
            _logger?.Warn($"Could not bring {app.DisplayName} to the foreground.");
        }

        return activated;
    }
}
