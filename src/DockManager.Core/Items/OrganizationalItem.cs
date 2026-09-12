using DockManager.Core.Dock;
using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>
/// A purely visual divider between neighbouring dock items. It has no target and can never be
/// opened; it only structures the list.
/// </summary>
public sealed class SeparatorItem : PinnedItem
{
    public SeparatorItem(string? id, DockSection section)
        : base(id ?? Guid.NewGuid().ToString("N"), targetPath: string.Empty)
    {
        _section = section;
    }

    private readonly DockSection _section;

    public override PinnedItemKind Kind => PinnedItemKind.Separator;

    public override DockSection Section => _section;

    public override ItemAvailability GetAvailability(IFileSystemProbe fileSystem) => ItemAvailability.Available;

    public override LaunchRequest? CreateLaunchRequest(IFileSystemProbe fileSystem) => null;

    public override PinnedItem Clone() => new SeparatorItem(Id, _section) { PinnedAtUtc = PinnedAtUtc };
}

/// <summary>
/// The header of an optional group. The group consists of this header and every item that follows
/// it until the next header (or the end of the section), which means groups ride entirely on the
/// existing ordered list: reordering, drag and drop and persistence need no special handling.
/// </summary>
public sealed class GroupHeaderItem : PinnedItem
{
    public GroupHeaderItem(string? id, string name, DockSection section)
        : base(id ?? Guid.NewGuid().ToString("N"), targetPath: string.Empty, displayName: name)
    {
        _section = section;
    }

    private readonly DockSection _section;

    public override PinnedItemKind Kind => PinnedItemKind.GroupHeader;

    public override DockSection Section => _section;

    public override string EffectiveName
        => string.IsNullOrWhiteSpace(DisplayName) ? "Group" : DisplayName!.Trim();

    public override ItemAvailability GetAvailability(IFileSystemProbe fileSystem) => ItemAvailability.Available;

    public override LaunchRequest? CreateLaunchRequest(IFileSystemProbe fileSystem) => null;

    public override PinnedItem Clone()
        => new GroupHeaderItem(Id, DisplayName ?? string.Empty, _section) { PinnedAtUtc = PinnedAtUtc };
}
