namespace DockManager.Core.Dock;

/// <summary>
/// All fixed spacing values used to lay out the dock panel. Derived from the icon size so a
/// single "size" setting scales the whole dock consistently.
/// </summary>
public readonly record struct DockLayoutMetrics
{
    public const int MinIconSize = 20;
    public const int MaxIconSize = 56;

    public DockLayoutMetrics(int iconSize, int itemPadding, int dockPadding, int sectionGap, int separatorThickness)
    {
        IconSize = Math.Clamp(iconSize, MinIconSize, MaxIconSize);
        ItemPadding = Math.Max(0, itemPadding);
        DockPadding = Math.Max(0, dockPadding);
        SectionGap = Math.Max(0, sectionGap);
        SeparatorThickness = Math.Max(1, separatorThickness);
    }

    public int IconSize { get; }
    public int ItemPadding { get; }
    public int DockPadding { get; }
    public int SectionGap { get; }
    public int SeparatorThickness { get; }

    /// <summary>Height (and width) of a single tappable slot in the dock.</summary>
    public int ItemSize => IconSize + (2 * ItemPadding);

    /// <summary>Width of the dock panel.</summary>
    public int DockWidth => ItemSize + (2 * DockPadding);

    /// <summary>Builds the spacing set that matches the dock size presets.</summary>
    public static DockLayoutMetrics ForIconSize(int iconSize)
    {
        var clamped = Math.Clamp(iconSize, MinIconSize, MaxIconSize);
        var itemPadding = clamped switch
        {
            < 28 => 4,
            < 40 => 6,
            _ => 7,
        };
        var dockPadding = itemPadding + 1;
        return new DockLayoutMetrics(clamped, itemPadding, dockPadding, sectionGap: 6, separatorThickness: 1);
    }
}
