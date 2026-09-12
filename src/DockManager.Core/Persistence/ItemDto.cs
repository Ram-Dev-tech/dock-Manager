using DockManager.Core.Dock;
using DockManager.Core.Items;

namespace DockManager.Core.Persistence;

/// <summary>
/// On disk representation of one pinned item. Only what is needed to reopen the item is stored:
/// the path (a stable Windows identifier for apps, files and folders) plus the launch details of a
/// shortcut when one was pinned. Groups and separators store their section instead of a path.
/// </summary>
public sealed class ItemDto
{
    public string? Id { get; set; }

    public PinnedItemKind Kind { get; set; }

    /// <summary>Section of organizational entries (separators, group headers); null otherwise.</summary>
    public DockSection? Section { get; set; }

    public string? TargetPath { get; set; }

    public string? ExecutablePath { get; set; }

    public string? DisplayName { get; set; }

    public string? Arguments { get; set; }

    public string? WorkingDirectory { get; set; }

    public DateTime? PinnedAtUtc { get; set; }

    public static ItemDto From(PinnedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var dto = new ItemDto
        {
            Id = item.Id,
            Kind = item.Kind,
            TargetPath = item.TargetPath,
            DisplayName = item.DisplayName,
            PinnedAtUtc = item.PinnedAtUtc,
        };

        if (item is AppItem app)
        {
            dto.ExecutablePath = app.ExecutablePath;
            dto.Arguments = app.Arguments;
            dto.WorkingDirectory = app.WorkingDirectory;
        }

        if (item.Kind.IsOrganizational())
        {
            dto.Section = item.Section;
        }

        return dto;
    }

    /// <summary>Rebuilds the item. Returns <c>null</c> for entries this version cannot understand.</summary>
    public PinnedItem? ToItem()
    {
        if (Kind.IsOrganizational())
        {
            var section = Section ?? DockSection.Applications;
            return Kind == PinnedItemKind.GroupHeader
                ? new GroupHeaderItem(Id, DisplayName ?? string.Empty, section)
                : new SeparatorItem(Id, section);
        }

        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            return null;
        }

        var id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("N") : Id!;

        PinnedItem item = Kind switch
        {
            PinnedItemKind.Application => new AppItem(id, TargetPath!, DisplayName)
            {
                ExecutablePath = ExecutablePath,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirectory,
            },
            PinnedItemKind.Folder => new FolderItem(id, TargetPath!, DisplayName),
            PinnedItemKind.File => new FileItem(id, TargetPath!, DisplayName),
            _ => new FileItem(id, TargetPath!, DisplayName),
        };

        if (PinnedAtUtc is { } pinnedAt)
        {
            item.PinnedAtUtc = pinnedAt;
        }

        return item;
    }
}

/// <summary>Root document of <c>pinned-items.json</c>.</summary>
public sealed class ItemStoreDocument
{
    public int Version { get; set; } = 1;

    public List<ItemDto> Items { get; set; } = [];
}
