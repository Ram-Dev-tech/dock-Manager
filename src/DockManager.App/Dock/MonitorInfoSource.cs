using System.Runtime.InteropServices;
using DockManager.Core.Dock;

namespace DockManager.App.Dock;

/// <summary>
/// Geometry of the monitor the dock lives on. Everything is kept in physical pixels because the
/// window is positioned with <c>SetWindowPos</c>; WPF's own Left/Top are avoided so the dock stays
/// correct on multi monitor setups with mixed DPI.
/// </summary>
internal sealed record MonitorGeometry(Win32.RECT WorkArea, double DpiScale, IntPtr Monitor, string DeviceName)
{
    public static MonitorGeometry Unknown { get; } = new(
        new Win32.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1040 },
        1d,
        IntPtr.Zero,
        string.Empty);

    public int Width => WorkArea.Width;

    public int Height => WorkArea.Height;

    /// <summary>Work area expressed in the DIP space of this monitor.</summary>
    public DockRect WorkAreaInDips => new(
        WorkArea.Left / DpiScale,
        WorkArea.Top / DpiScale,
        WorkArea.Width / DpiScale,
        WorkArea.Height / DpiScale);
}

/// <summary>One connected display, as offered in the settings screen.</summary>
internal sealed record MonitorSummary(string DeviceName, Win32.RECT WorkArea, bool IsPrimary)
{
    public string Description
        => $"{DeviceName.Replace("\\\\.\\", string.Empty)} — {WorkArea.Width}×{WorkArea.Height}"
           + (IsPrimary ? " (primary)" : string.Empty);
}

/// <summary>Resolves the monitor the dock should attach to.</summary>
internal sealed class MonitorInfoSource
{
    /// <summary>
    /// Returns the geometry of the monitor the dock should live on: the configured one when it is
    /// still connected, otherwise the monitor nearest the dock window (primary on first start).
    /// </summary>
    public MonitorGeometry GetFor(IntPtr ownerWindow, string? preferredDeviceName = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            foreach (var summary in EnumerateMonitors())
            {
                if (string.Equals(summary.DeviceName, preferredDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return ToGeometry(summary.DeviceName);
                }
            }

            // The configured display was unplugged: fall back instead of disappearing.
        }

        var monitor = ownerWindow != IntPtr.Zero
            ? Win32.MonitorFromWindow(ownerWindow, Win32.MonitorDefaultToNearest)
            : Win32.MonitorFromPoint(new Win32.POINT { X = 0, Y = 0 }, 1 /* MONITOR_DEFAULTTOPRIMARY */);

        if (monitor == IntPtr.Zero)
        {
            return MonitorGeometry.Unknown;
        }

        var info = new Win32.MONITORINFO { cbSize = Marshal.SizeOf<Win32.MONITORINFO>() };
        if (!Win32.GetMonitorInfo(monitor, ref info))
        {
            return MonitorGeometry.Unknown;
        }

        return new MonitorGeometry(info.rcWork, GetDpiScale(monitor), monitor, DeviceNameOf(monitor));
    }

    /// <summary>Returns the monitor handle that contains a physical screen point.</summary>
    public IntPtr MonitorFromPoint(int x, int y)
        => Win32.MonitorFromPoint(new Win32.POINT { X = x, Y = y }, Win32.MonitorDefaultToNearest);

    /// <summary>Every connected display, primary first.</summary>
    public IReadOnlyList<MonitorSummary> EnumerateMonitors()
    {
        var found = new List<MonitorSummary>();

        try
        {
            Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
            {
                var info = new Win32.MONITORINFOEX { cbSize = Marshal.SizeOf<Win32.MONITORINFOEX>() };
                if (Win32.GetMonitorInfoEx(monitor, ref info))
                {
                    found.Add(new MonitorSummary(
                        info.szDevice ?? string.Empty,
                        info.rcWork,
                        (info.dwFlags & 1) != 0));
                }

                return true;
            }, IntPtr.Zero);
        }
        catch (Exception)
        {
            // Enumeration is best effort; the caller falls back to the owner window's monitor.
        }

        return found.OrderByDescending(summary => summary.IsPrimary).ToList();
    }

    private static MonitorGeometry ToGeometry(string deviceName)
    {
        var handle = MonitorHandleByName(deviceName);
        if (handle == IntPtr.Zero)
        {
            return MonitorGeometry.Unknown;
        }

        var info = new Win32.MONITORINFO { cbSize = Marshal.SizeOf<Win32.MONITORINFO>() };
        if (!Win32.GetMonitorInfo(handle, ref info))
        {
            return MonitorGeometry.Unknown;
        }

        return new MonitorGeometry(info.rcWork, GetDpiScale(handle), handle, deviceName);
    }

    private static IntPtr MonitorHandleByName(string deviceName)
    {
        var result = IntPtr.Zero;

        try
        {
            Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
            {
                if (DeviceNameOf(monitor).Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    result = monitor;
                    return false;
                }

                return true;
            }, IntPtr.Zero);
        }
        catch (Exception)
        {
            // Best effort.
        }

        return result;
    }

    private static string DeviceNameOf(IntPtr monitor)
    {
        var info = new Win32.MONITORINFOEX { cbSize = Marshal.SizeOf<Win32.MONITORINFOEX>() };
        return Win32.GetMonitorInfoEx(monitor, ref info) ? info.szDevice ?? string.Empty : string.Empty;
    }

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
