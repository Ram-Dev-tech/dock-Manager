namespace DockManager.Core.Dock;

/// <summary>
/// The screen edge the dock is attached to. Phase 1 is vertical only: the dock never
/// attaches to the top or bottom edge.
/// </summary>
public enum DockEdge
{
    Left = 0,
    Right = 1,
}

public static class DockEdgeExtensions
{
    /// <summary>Returns the opposite edge, used by the settings UI and by tests.</summary>
    public static DockEdge Opposite(this DockEdge edge)
        => edge == DockEdge.Left ? DockEdge.Right : DockEdge.Left;

    /// <summary>True when <paramref name="value"/> is a valid member of <see cref="DockEdge"/>.</summary>
    public static bool IsValid(this DockEdge value)
        => value is DockEdge.Left or DockEdge.Right;
}
