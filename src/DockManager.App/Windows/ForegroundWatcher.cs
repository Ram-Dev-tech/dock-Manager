using DockManager.Core.Diagnostics;

namespace DockManager.App.Windows;

/// <summary>
/// Event driven foreground tracking. Using <c>SetWinEventHook</c> means the active application
/// indicator updates the moment the user switches windows, without polling for it.
/// </summary>
public sealed class ForegroundWatcher : IDisposable
{
    private readonly IDockLogger? _logger;

    // The delegate must stay alive for as long as the hook exists or the callback is collected.
    private Win32.WinEventProc? _callback;
    private IntPtr _hook = IntPtr.Zero;
    private bool _disposed;

    public ForegroundWatcher(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Raised on the thread that installed the hook; the dock marshals it to the UI thread.</summary>
    public event EventHandler? ForegroundChanged;

    public bool IsHooked => _hook != IntPtr.Zero;

    public void Start()
    {
        if (_disposed || _hook != IntPtr.Zero)
        {
            return;
        }

        try
        {
            _callback = OnWinEvent;
            _hook = Win32.SetWinEventHook(
                Win32.EventSystemForeground,
                Win32.EventSystemForeground,
                IntPtr.Zero,
                _callback,
                0,
                0,
                Win32.WineventOutOfContext | Win32.WineventSkipOwnProcess);

            if (_hook == IntPtr.Zero)
            {
                _logger?.Warn("Foreground hook could not be installed; falling back to polling.");
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            _logger?.Warn("Foreground hook is unavailable on this system.", ex);
            _hook = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hook != IntPtr.Zero)
        {
            Win32.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }

        _callback = null;
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        try
        {
            ForegroundChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger?.Error("Foreground changed handler failed.", ex);
        }
    }
}
