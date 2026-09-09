using DockManager.Core.Dock;
using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>A pinned file, opened with its normal Windows associated application.</summary>
public sealed class FileItem : PinnedItem
{
    public FileItem(string id, string targetPath, string? displayName = null)
        : base(id, targetPath, displayName)
    {
    }

    public override PinnedItemKind Kind => PinnedItemKind.File;

    public override DockSection Section => DockSection.Files;

    public override ItemAvailability GetAvailability(IFileSystemProbe fileSystem)
    {
        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            return ItemAvailability.Invalid;
        }

        if (!fileSystem.Exists(TargetPath))
        {
            return ItemAvailability.Missing;
        }

        // A pinned file whose path is now a folder would open Explorer instead of the file.
        return fileSystem.IsDirectory(TargetPath) ? ItemAvailability.Invalid : ItemAvailability.Available;
    }

    public override LaunchRequest? CreateLaunchRequest(IFileSystemProbe fileSystem)
    {
        if (GetAvailability(fileSystem) != ItemAvailability.Available)
        {
            return null;
        }

        return new LaunchRequest(TargetPath, LaunchVerb.Open);
    }

    public override PinnedItem Clone() => new FileItem(Id, TargetPath, DisplayName)
    {
        PinnedAtUtc = PinnedAtUtc,
    };
}
