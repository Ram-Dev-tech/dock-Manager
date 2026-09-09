namespace DockManager.Core.Items;

/// <summary>The three kinds of thing Phase 1 can pin.</summary>
public enum PinnedItemKind
{
    Application = 0,
    File = 1,
    Folder = 2,
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
