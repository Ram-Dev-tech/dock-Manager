using DockManager.App.Dock;
using DockManager.Core.Windows;

namespace DockManager.App.Windows;

/// <summary>
/// Enumerates the top level windows a user would consider "an application": visible, unowned,
/// titled, not cloaked and not a shell window. Called only while the dock is visible so an idle
/// dock costs nothing.
/// </summary>
public sealed class WindowEnumerator
{
    private static readonly HashSet<string> ExcludedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Shell_Desktop",
        "Windows.UI.Core.CoreWindow",
        "XamlExplorerHostIslandWindow",
        "ForegroundStaging",
        "ApplicationManager_DesktopShellWindow",
        "TaskListThumbnailWnd",
        "NotifyIconOverflowWindow",
        "MultitaskingViewFrame",
        "Button",
        "tooltips_class32",
    };

    private readonly ProcessResolver _processResolver;
    private readonly int _ownProcessId = Environment.ProcessId;

    public WindowEnumerator(ProcessResolver? processResolver = null)
    {
        _processResolver = processResolver ?? new ProcessResolver();
    }

    public IReadOnlyList<WindowInfo> GetTopLevelWindows()
    {
        var windows = new List<WindowInfo>(32);

        // The callback must never throw: an exception inside EnumWindows corrupts the enumeration.
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            try
            {
                var info = Describe(hwnd);
                if (info is not null)
                {
                    windows.Add(info);
                }
            }
            catch (Exception)
            {
                // Ignore this window and keep going.
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private WindowInfo? Describe(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindowVisible(hwnd))
        {
            return null;
        }

        if (NativeMethods.GetWindow(hwnd, NativeMethods.GwOwner) != IntPtr.Zero)
        {
            return null;
        }

        var exStyle = NativeMethods.GetWindowLongPtrSafe(hwnd, NativeMethods.GwlExStyle).ToInt64();
        if ((exStyle & NativeMethods.WsExToolWindow) != 0)
        {
            return null;
        }

        var className = NativeMethods.GetWindowClassName(hwnd);
        if (ExcludedClasses.Contains(className))
        {
            return null;
        }

        var title = NativeMethods.GetWindowTitle(hwnd);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        if (IsCloaked(hwnd))
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0 || processId == (uint)_ownProcessId)
        {
            return null;
        }

        var executable = _processResolver.GetExecutablePath(processId);
        return new WindowInfo(hwnd, executable, (int)processId)
        {
            Title = title,
            IsMinimized = NativeMethods.IsIconic(hwnd),
        };
    }

    private static bool IsCloaked(IntPtr hwnd)
    {
        try
        {
            if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DwmwaCloaked, out var cloaked, sizeof(int)) == 0)
            {
                return cloaked != 0;
            }
        }
        catch (DllNotFoundException)
        {
            // DWM is always present on supported Windows versions; ignore if it is not.
        }

        return false;
    }
}
