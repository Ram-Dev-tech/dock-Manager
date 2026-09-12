using DockManager.Core.Dock;

namespace DockManager.App.Dock;

/// <summary>Physical window rectangle the dock should occupy at a given reveal progress.</summary>
internal readonly record struct DockPlacement(int X, int Y, int Width, int Height);

/// <summary>
/// Turns the core layout (which is in DIPs) into physical pixel positions on the target monitor.
/// </summary>
internal static class DockPosition
{
    /// <summary>
    /// Computes the physical placement. <paramref name="progress"/> runs from 0 (fully off screen)
    /// to 1 (flush with the edge).
    /// </summary>
    public static DockPlacement Compute(
        DockEdge edge,
        MonitorGeometry monitor,
        int dockWidthInDips,
        int dockHeightInDips,
        double progress)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        var scale = monitor.DpiScale <= 0 ? 1d : monitor.DpiScale;
        var work = monitor.WorkArea;

        var width = (int)Math.Round(dockWidthInDips * scale);
        var height = (int)Math.Min(Math.Round(dockHeightInDips * scale), Math.Max(0, work.Height));

        var y = work.Top + Math.Max(0, (work.Height - height) / 2);

        var shownX = edge == DockEdge.Left ? work.Left : work.Right - width;
        var hiddenX = edge == DockEdge.Left ? work.Left - width : work.Right;
        var clamped = Math.Clamp(progress, 0d, 1d);
        var x = (int)Math.Round(hiddenX + ((shownX - hiddenX) * clamped));

        return new DockPlacement(x, y, width, height);
    }

    /// <summary>True when a physical cursor position is inside the activation strip of the edge.</summary>
    public static bool IsAtEdge(DockEdge edge, MonitorGeometry monitor, int thresholdPixels, int cursorX, int cursorY)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        var work = monitor.WorkArea;
        if (cursorY < work.Top || cursorY > work.Bottom)
        {
            return false;
        }

        var thickness = Math.Max(1, thresholdPixels);
        return edge == DockEdge.Left
            ? cursorX <= work.Left + thickness
            : cursorX >= work.Right - thickness;
    }
}
