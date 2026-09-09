using DockManager.Core.Dock;

namespace DockManager.App.Dock;

/// <summary>
/// Geometry of the monitor the dock lives on. Everything is kept in physical pixels because the
/// window is positioned with <c>SetWindowPos</c>; WPF's own Left/Top are avoided so the dock stays
/// correct on multi monitor setups with mixed DPI.
/// </summary>
internal sealed record MonitorGeometry(Win32.RECT WorkArea, double DpiScale, IntPtr Monitor)
{
    public static MonitorGeometry Unknown { get; } = new(
        new Win32.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1040 },
        1d,
        IntPtr.Zero);

    public int Width => WorkArea.Width;

    public int Height => WorkArea.Height;

    /// <summary>Work area expressed in the DIP space of this monitor.</summary>
    public DockRect WorkAreaInDips => new(
        WorkArea.Left / DpiScale,
        WorkArea.Top / DpiScale,
        WorkArea.Width / DpiScale,
        WorkArea.Height / DpiScale);
}

/// <summary>Resolves the monitor the dock should attach to.</summary>
internal sealed class MonitorInfoSource
{
    /// <summary>
    /// Returns the monitor geometry for <paramref name="ownerWindow"/>, falling back to the primary
    /// monitor. The dock attaches to the primary display in Phase 1; the seam exists so a later
    /// phase can attach per monitor.
    /// </summary>
    public MonitorGeometry GetFor(IntPtr ownerWindow)
    {
        var monitor = ownerWindow != IntPtr.Zero
            ? Win32.MonitorFromWindow(ownerWindow, Win32.MonitorDefaultToNearest)
            : Win32.MonitorFromPoint(new Win32.POINT { X = 0, Y = 0 }, 1 /* MONITOR_DEFAULTTOPRIMARY */);

        if (monitor == IntPtr.Zero)
        {
            return MonitorGeometry.Unknown;
        }

        var info = new Win32.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Win32.MONITORINFO>() };
        if (!Win32.GetMonitorInfo(monitor, ref info))
        {
            return MonitorGeometry.Unknown;
        }

        return new MonitorGeometry(info.rcWork, GetDpiScale(monitor), monitor);
    }

    /// <summary>Returns the monitor handle that contains a physical screen point.</summary>
    public IntPtr MonitorFromPoint(int x, int y)
        => Win32.MonitorFromPoint(new Win32.POINT { X = x, Y = y }, Win32.MonitorDefaultToNearest);

    private static double GetDpiScale(IntPtr monitor)
    {
        try
        {
            if (Win32.GetDpiForMonitor(monitor, 0 /* MDT_EFFECTIVE_DPI */, out var dpiX, out _) == 0 && dpiX > 0)
            {
                return dpiX / 96d;
            }
        }
        catch (EntryPointNotFoundException)
        {
            // shcore is missing on very old builds; fall back to 100%.
        }

        return 1d;
    }
}
