namespace DockManager.Core.Integrations;

/// <summary>
/// Brings a window to the foreground (restoring it when minimized). Implemented by the Win32 layer
/// and injected into integrations so they stay testable.
/// </summary>
public interface IWindowActivator
{
    bool ActivateWindow(IntPtr handle);
}
