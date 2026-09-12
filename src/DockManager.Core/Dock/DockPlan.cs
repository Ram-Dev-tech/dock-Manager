namespace DockManager.Core.Dock;

/// <summary>
/// The fully resolved layout of the dock panel: its size, where every slot lives and how a
/// mouse coordinate maps onto an item. Pure geometry, so it can be unit tested and reused by
/// any renderer.
/// </summary>
public sealed class DockPlan
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Slot rectangles in dock panel coordinates, in paint order.</summary>
    public required IReadOnlyList<DockRect> ItemRects { get; init; }

    /// <summary>Separator rectangles in dock panel coordinates.</summary>
    public required IReadOnlyList<DockRect> SeparatorRects { get; init; }

    /// <summary>Number of pinned applications rendered in the first section.</summary>
    public required int ApplicationCount { get; init; }

    /// <summary>Number of pinned files/folders rendered in the second section.</summary>
    public required int FileCount { get; init; }

    /// <summary>Number of always present footer slots (add item, settings).</summary>
    public required int FooterCount { get; init; }

    public int ItemCount => ItemRects.Count;

    public int ApplicationStart => 0;
    public int FileStart => ApplicationCount;
    public int FooterStart => ApplicationCount + FileCount;

    public DockRect GetItemRect(int flattenedIndex)
        => flattenedIndex >= 0 && flattenedIndex < ItemRects.Count ? ItemRects[flattenedIndex] : default;

    /// <summary>Translates a section relative index into the flattened index used by <see cref="ItemRects"/>.</summary>
    public int Flatten(DockSection section, int indexInSection) => section switch
    {
        DockSection.Applications => ApplicationStart + indexInSection,
        DockSection.Files => FileStart + indexInSection,
        DockSection.Footer => FooterStart + indexInSection,
        _ => -1,
    };

    /// <summary>Returns the section that owns <paramref name="flattenedIndex"/>.</summary>
    public DockSection SectionOf(int flattenedIndex)
    {
        if (flattenedIndex < 0 || flattenedIndex >= ItemCount)
        {
            return DockSection.None;
        }

        if (flattenedIndex < FileStart)
        {
            return DockSection.Applications;
        }

        return flattenedIndex < FooterStart ? DockSection.Files : DockSection.Footer;
    }

    public int CountOf(DockSection section) => section switch
    {
        DockSection.Applications => ApplicationCount,
        DockSection.Files => FileCount,
        DockSection.Footer => FooterCount,
        _ => 0,
    };
}
