using DockManager.Core.Diagnostics;

using DockManager.Core.Integrations;

namespace DockManager.App.Windows;

/// <summary>
/// Brings a window to the foreground. Windows blocks <c>SetForegroundWindow</c> from processes that
/// do not already own the foreground, so the input threads are attached for the duration of the
/// call, which is the standard way to make the switch reliable without stealing focus elsewhere.
/// </summary>
public sealed class WindowActivator : IWindowActivator
{
    private static readonly IntPtr HwndTop = IntPtr.Zero;

    private readonly IDockLogger? _logger;

    public WindowActivator(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    public bool Activate(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !Win32.IsWindow(hwnd))
        {
            return false;
        }

        try
        {
            if (Win32.IsIconic(hwnd))
            {
                Win32.ShowWindow(hwnd, Win32.SwRestore);
            }

            var foreground = Win32.GetForegroundWindow();
            var foregroundThread = foreground == IntPtr.Zero
                ? 0
                : Win32.GetWindowThreadProcessId(foreground, out _);
            var currentThread = Win32.GetCurrentThreadId();

            var attached = foregroundThread != 0
                && foregroundThread != currentThread
                && Win32.AttachThreadInput(currentThread, foregroundThread, true);

            try
            {
                Win32.BringWindowToTop(hwnd);
                if (!Win32.SetForegroundWindow(hwnd))
                {
                    _logger?.Warn($"SetForegroundWindow refused handle {hwnd}; falling back to top-most placement.");
                    Win32.SetWindowPos(
                        hwnd,
                        HwndTop,
                        0,
                        0,
                        0,
                        0,
                        Win32.SwpNoMove | Win32.SwpNoSize | Win32.SwpShowWindow);
                }
            }
            finally
            {
                if (attached)
                {
                    Win32.AttachThreadInput(currentThread, foregroundThread, false);
                }
            }

            return Win32.GetForegroundWindow() == hwnd;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            _logger?.Error("Window activation failed.", ex);
            return false;
        }
    }

    bool IWindowActivator.ActivateWindow(IntPtr handle) => Activate(handle);
}
