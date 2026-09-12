namespace DockManager.Core.Items;

/// <summary>Everything the dock can hold: openable items and pure layout entries.</summary>
public enum PinnedItemKind
{
    Application = 0,
    File = 1,
    Folder = 2,

    /// <summary>A purely visual divider; cannot be opened.</summary>
    Separator = 3,

    /// <summary>The header of an optional group; cannot be opened.</summary>
    GroupHeader = 4,
}

public static class PinnedItemKindExtensions
{
    /// <summary>Whether this kind represents something the user can open.</summary>
    public static bool IsOpenable(this PinnedItemKind kind)
        => kind is PinnedItemKind.Application or PinnedItemKind.File or PinnedItemKind.Folder;

    /// <summary>Whether this kind only structures the list (groups and separators).</summary>
    public static bool IsOrganizational(this PinnedItemKind kind)
        => kind is PinnedItemKind.Separator or PinnedItemKind.GroupHeader;
}

/// <summary>Whether a pinned item can still be opened.</summary>
public enum ItemAvailability
{
    /// <summary>Target exists and can be opened.</summary>
    Available = 0,

    /// <summary>Target no longer exists (deleted, moved or an unplugged drive).</summary>
    Missing,

    /// <summary>The stored target is not usable at all (empty or malformed path).</summary>
    Invalid,
}
