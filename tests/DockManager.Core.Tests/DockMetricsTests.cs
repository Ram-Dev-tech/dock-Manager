using DockManager.Core.Dock;

namespace DockManager.Core.Tests;

public class DockMetricsTests
{
    private static readonly DockLayoutMetrics Metrics = new(iconSize: 32, itemPadding: 6, dockPadding: 7, sectionGap: 6, separatorThickness: 1);

    [Fact]
    public void ItemSize_and_dock_width_follow_the_icon_size()
    {
        Assert.Equal(44, Metrics.ItemSize);
        Assert.Equal(58, Metrics.DockWidth);
    }

    [Fact]
    public void Compute_stacks_apps_then_separator_then_files_then_footer()
    {
        var plan = DockMetrics.Compute(applicationCount: 3, fileCount: 2, Metrics);

        Assert.Equal(7, plan.ItemRects.Count);
        Assert.Equal(3, plan.ApplicationCount);
        Assert.Equal(2, plan.FileCount);
        Assert.Equal(DockMetrics.FooterSlotCount, plan.FooterCount);
        Assert.Equal(2, plan.SeparatorRects.Count);
        Assert.Equal(Metrics.DockWidth, plan.Width);

        // Slots are contiguous inside a section.
        Assert.Equal(7, plan.ItemRects[0].Y);
        Assert.Equal(7 + 44, plan.ItemRects[1].Y);
        Assert.Equal(7 + 88, plan.ItemRects[2].Y);

        // A separator sits between the two pinned sections.
        var separator = plan.SeparatorRects[0];
        Assert.True(separator.Top > plan.ItemRects[2].Bottom);
        Assert.True(separator.Top < plan.ItemRects[3].Top);

        // Total height accounts for padding, slots, separators and gaps.
        var expected = (2 * Metrics.DockPadding)
            + (7 * Metrics.ItemSize)
            + (2 * ((2 * Metrics.SectionGap) + Metrics.SeparatorThickness));
        Assert.Equal(expected, plan.Height);
    }

    [Fact]
    public void Compute_omits_separators_for_empty_sections()
    {
        var onlyApps = DockMetrics.Compute(2, 0, Metrics);
        Assert.Single(onlyApps.SeparatorRects);

        var empty = DockMetrics.Compute(0, 0, Metrics);
        Assert.Empty(empty.SeparatorRects);
        Assert.Equal(DockMetrics.FooterSlotCount, empty.ItemCount);
    }

    [Fact]
    public void Section_lookup_maps_flattened_indexes_both_ways()
    {
        var plan = DockMetrics.Compute(3, 2, Metrics);

        Assert.Equal(DockSection.Applications, plan.SectionOf(0));
        Assert.Equal(DockSection.Applications, plan.SectionOf(2));
        Assert.Equal(DockSection.Files, plan.SectionOf(3));
        Assert.Equal(DockSection.Files, plan.SectionOf(4));
        Assert.Equal(DockSection.Footer, plan.SectionOf(5));
        Assert.Equal(DockSection.None, plan.SectionOf(7));
        Assert.Equal(5, plan.Flatten(DockSection.Footer, 0));
    }

    [Fact]
    public void HitTestItem_returns_the_slot_under_the_cursor()
    {
        var plan = DockMetrics.Compute(2, 1, Metrics);

        Assert.Equal(-1, DockMetrics.HitTestItem(plan, 0));
        Assert.Equal(0, DockMetrics.HitTestItem(plan, 8));
        Assert.Equal(1, DockMetrics.HitTestItem(plan, 8 + 44));
        Assert.Equal(-1, DockMetrics.HitTestItem(plan, plan.Height - 2));
    }

    [Fact]
    public void GetInsertionIndex_uses_slot_mid_points()
    {
        var plan = DockMetrics.Compute(3, 0, Metrics);
        var first = plan.ItemRects[0];

        Assert.Equal(0, DockMetrics.GetInsertionIndex(plan, DockSection.Applications, first.Top + 1));
        Assert.Equal(1, DockMetrics.GetInsertionIndex(plan, DockSection.Applications, first.Bottom - 1));
        Assert.Equal(3, DockMetrics.GetInsertionIndex(plan, DockSection.Applications, plan.ItemRects[2].Bottom));
        Assert.Equal(0, DockMetrics.GetInsertionIndex(plan, DockSection.Files, first.Top));
    }

    [Theory]
    [InlineData(DockEdge.Left)]
    [InlineData(DockEdge.Right)]
    public void ComputeRect_is_fully_off_screen_when_hidden_and_flush_when_revealed(DockEdge edge)
    {
        var workArea = new DockRect(0, 0, 1920, 1040);
        var hidden = DockMetrics.ComputeRect(edge, workArea, 58, 300, 0d);
        var shown = DockMetrics.ComputeRect(edge, workArea, 58, 300, 1d);

        if (edge == DockEdge.Left)
        {
            Assert.Equal(-58, hidden.X);
            Assert.Equal(0, shown.X);
        }
        else
        {
            Assert.Equal(1920, hidden.X);
            Assert.Equal(1920 - 58, shown.X);
        }

        // Vertically centred in the work area.
        Assert.Equal((1040 - 300) / 2d, shown.Y);
        Assert.Equal(58, shown.Width);
        Assert.Equal(300, shown.Height);
    }

    [Fact]
    public void ComputeRect_interpolates_between_hidden_and_shown()
    {
        var workArea = new DockRect(0, 0, 1920, 1040);
        var half = DockMetrics.ComputeRect(DockEdge.Left, workArea, 100, 200, 0.5d);
        Assert.Equal(-50, half.X);
    }

    [Fact]
    public void ComputeRect_keeps_the_dock_inside_the_work_area_on_small_screens()
    {
        var workArea = new DockRect(0, 0, 800, 200);
        var rect = DockMetrics.ComputeRect(DockEdge.Right, workArea, 58, 900, 1d);

        Assert.Equal(200, rect.Height);
        Assert.Equal(0, rect.Y);
    }

    [Fact]
    public void Activation_zone_hugs_the_configured_edge()
    {
        var workArea = new DockRect(0, 0, 1920, 1040);

        var left = DockMetrics.ComputeActivationZone(DockEdge.Left, workArea, 2);
        Assert.Equal(0, left.X);
        Assert.Equal(2, left.Width);
        Assert.Equal(1040, left.Height);

        var right = DockMetrics.ComputeActivationZone(DockEdge.Right, workArea, 3);
        Assert.Equal(1917, right.X);
        Assert.Equal(3, right.Width);
    }

    [Fact]
    public void IsAtEdge_only_matches_the_configured_edge_and_vertical_range()
    {
        var workArea = new DockRect(0, 0, 1920, 1040);

        Assert.True(DockMetrics.IsAtEdge(DockEdge.Left, workArea, 2, 0, 500));
        Assert.True(DockMetrics.IsAtEdge(DockEdge.Left, workArea, 2, 2, 500));
        Assert.False(DockMetrics.IsAtEdge(DockEdge.Left, workArea, 2, 20, 500));
        Assert.False(DockMetrics.IsAtEdge(DockEdge.Left, workArea, 2, 0, 2000));

        Assert.True(DockMetrics.IsAtEdge(DockEdge.Right, workArea, 2, 1919, 500));
        Assert.False(DockMetrics.IsAtEdge(DockEdge.Right, workArea, 2, 1900, 500));
    }

    [Fact]
    public void Icon_size_is_clamped_to_the_supported_range()
    {
        Assert.Equal(DockLayoutMetrics.MinIconSize, DockLayoutMetrics.ForIconSize(1).IconSize);
        Assert.Equal(DockLayoutMetrics.MaxIconSize, DockLayoutMetrics.ForIconSize(999).IconSize);
    }
}
