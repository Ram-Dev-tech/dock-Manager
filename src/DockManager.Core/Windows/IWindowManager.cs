namespace DockManager.Core.Windows;

/// <summary>
/// The window operations the dock needs. Defined in the core so the running/switching logic can be
/// tested with a fake, while the Win32 implementation lives in the application layer.
/// </summary>
public interface IWindowManager
{
    /// <summary>Enumerates the relevant top level windows and groups them per application.</summary>
    RunningAppIndex GetRunningApps();

    /// <summary>Handle of the window that currently has the foreground.</summary>
    IntPtr GetForegroundWindowHandle();

    /// <summary>
    /// Brings the application forward, restoring it when minimized. Returns false when no window
    /// could be activated.
    /// </summary>
    bool Activate(RunningApp app);
}
