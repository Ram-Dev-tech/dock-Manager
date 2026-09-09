namespace DockManager.Core.Dock;

/// <summary>
/// Geometry for the dock panel and for the edge activation zone. Everything here is pure math so
/// the same rules can be exercised by unit tests on any platform.
/// </summary>
public static class DockMetrics
{
    /// <summary>The always present footer slots: "add item" and "settings".</summary>
    public const int FooterSlotCount = 2;

    /// <summary>Lays out the panel and returns every slot rectangle in dock coordinates.</summary>
    public static DockPlan Compute(
        int applicationCount,
        int fileCount,
        DockLayoutMetrics metrics,
        int footerCount = FooterSlotCount)
    {
        applicationCount = Math.Max(0, applicationCount);
        fileCount = Math.Max(0, fileCount);
        footerCount = Math.Max(0, footerCount);

        var itemRects = new List<DockRect>(applicationCount + fileCount + footerCount);
        var separators = new List<DockRect>(2);
        var slot = metrics.ItemSize;

        var y = (double)metrics.DockPadding;
        var hasContent = false;

        void AppendSection(int count)
        {
            for (var i = 0; i < count; i++)
            {
                itemRects.Add(new DockRect(metrics.DockPadding, y, slot, slot));
                y += slot;
            }

            hasContent |= count > 0;
        }

        void AppendSeparator()
        {
            y += metrics.SectionGap;
            var width = Math.Max(8, slot - 8);
            var x = metrics.DockPadding + ((slot - width) / 2d);
            separators.Add(new DockRect(x, y + (metrics.SeparatorThickness / 2d), width, metrics.SeparatorThickness));
            y += metrics.SeparatorThickness + metrics.SectionGap;
        }

        AppendSection(applicationCount);

        if (applicationCount > 0 && fileCount > 0)
        {
            AppendSeparator();
        }

        AppendSection(fileCount);

        if (footerCount > 0)
        {
            if (hasContent)
            {
                AppendSeparator();
            }

            AppendSection(footerCount);
        }

        var height = (int)Math.Ceiling(y + metrics.DockPadding);
        return new DockPlan
        {
            Width = metrics.DockWidth,
            Height = Math.Max(metrics.DockPadding * 2, height),
            ItemRects = itemRects,
            SeparatorRects = separators,
            ApplicationCount = applicationCount,
            FileCount = fileCount,
            FooterCount = footerCount,
        };
    }

    /// <summary>
    /// Position of the dock window for a given reveal progress.
    /// <paramref name="revealProgress"/> 0 means "fully hidden off screen", 1 means "flush with the edge".
    /// </summary>
    public static DockRect ComputeRect(
        DockEdge edge,
        DockRect workArea,
        int dockWidth,
        int dockHeight,
        double revealProgress)
    {
        var progress = Math.Clamp(revealProgress, 0d, 1d);
        var height = Math.Min(dockHeight, Math.Max(0, workArea.Height));
        var y = workArea.Y + Math.Max(0d, (workArea.Height - height) / 2d);

        var shownX = edge == DockEdge.Left ? workArea.Left : workArea.Right - dockWidth;
        var hiddenX = edge == DockEdge.Left ? workArea.Left - dockWidth : workArea.Right;
        var x = hiddenX + ((shownX - hiddenX) * progress);

        return new DockRect(x, y, dockWidth, height);
    }

    /// <summary>
    /// The invisible strip along the configured edge that triggers a reveal. It is intentionally
    /// only a few pixels wide so it never gets in the way of scroll bars or window resizing.
    /// </summary>
    public static DockRect ComputeActivationZone(DockEdge edge, DockRect workArea, int thickness)
    {
        var width = Math.Max(1, thickness);
        return edge == DockEdge.Left
            ? new DockRect(workArea.Left, workArea.Top, width, workArea.Height)
            : new DockRect(workArea.Right - width, workArea.Top, width, workArea.Height);
    }

    /// <summary>True when a screen coordinate is inside the activation strip of the given edge.</summary>
    public static bool IsAtEdge(DockEdge edge, DockRect workArea, int thickness, double x, double y)
    {
        if (y < workArea.Top || y > workArea.Bottom)
        {
            return false;
        }

        return edge == DockEdge.Left
            ? x <= workArea.Left + Math.Max(1, thickness)
            : x >= workArea.Right - Math.Max(1, thickness);
    }

    /// <summary>
    /// Returns the flattened index of the slot under a vertical dock coordinate, or -1 when the
    /// coordinate is on padding or a separator.
    /// </summary>
    public static int HitTestItem(DockPlan plan, double y)
    {
        ArgumentNullException.ThrowIfNull(plan);

        for (var i = 0; i < plan.ItemRects.Count; i++)
        {
            var rect = plan.ItemRects[i];
            if (y >= rect.Top && y < rect.Bottom)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Insertion index (0..count) for drag-and-drop reordering inside one section, based on the
    /// mid point of each slot so the insertion indicator feels predictable.
    /// </summary>
    public static int GetInsertionIndex(DockPlan plan, DockSection section, double y)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var count = plan.CountOf(section);
        if (count == 0)
        {
            return 0;
        }

        var start = section switch
        {
            DockSection.Applications => plan.ApplicationStart,
            DockSection.Files => plan.FileStart,
            DockSection.Footer => plan.FooterStart,
            _ => -1,
        };

        if (start < 0)
        {
            return 0;
        }

        for (var i = 0; i < count; i++)
        {
            var rect = plan.ItemRects[start + i];
            if (y < rect.Top + (rect.Height / 2d))
            {
                return i;
            }
        }

        return count;
    }
}
