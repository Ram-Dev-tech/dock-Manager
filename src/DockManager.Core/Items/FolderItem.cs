using DockManager.Core.Dock;
using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>A pinned folder, opened in Windows File Explorer.</summary>
public sealed class FolderItem : PinnedItem
{
    public FolderItem(string id, string targetPath, string? displayName = null)
        : base(id, targetPath, displayName)
    {
    }

    public override PinnedItemKind Kind => PinnedItemKind.Folder;

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

        return fileSystem.IsDirectory(TargetPath) ? ItemAvailability.Available : ItemAvailability.Invalid;
    }

    public override LaunchRequest? CreateLaunchRequest(IFileSystemProbe fileSystem)
    {
        if (GetAvailability(fileSystem) != ItemAvailability.Available)
        {
            return null;
        }

        // explorer.exe is used explicitly so folders always open in Explorer rather than in
        // whatever the shell decides to do with a directory.
        return new LaunchRequest("explorer.exe", LaunchVerb.Explore)
        {
            Arguments = Quote(TargetPath),
        };
    }

    private static string Quote(string path)
        => path.StartsWith('"') ? path : $"\"{path}\"";

    public override PinnedItem Clone() => new FolderItem(Id, TargetPath, DisplayName)
    {
        PinnedAtUtc = PinnedAtUtc,
    };
}
