using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>
/// Turns a dropped or picked path into the right kind of pinned item. Centralising the rules here
/// means drag-and-drop, the file dialog and the "add item" menu all behave identically.
/// </summary>
public static class PinnedItemFactory
{
    private static readonly string[] ExecutableExtensions =
    [
        ".exe", ".com", ".scr", ".bat", ".cmd",
    ];

    public static PinnedItem? CreateFromPath(string? path, IFileSystemProbe fileSystem, IShortcutResolver? shortcuts = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var target = path.Trim().Trim('"');
        if (target.Length == 0)
        {
            return null;
        }

        if (fileSystem.IsDirectory(target))
        {
            return new FolderItem(Guid.NewGuid().ToString("N"), target);
        }

        var extension = PathNormalizer.Extension(target);

        if (extension == ".lnk")
        {
            var app = new AppItem(Guid.NewGuid().ToString("N"), target);
            if (shortcuts is not null && shortcuts.TryResolve(target, out var resolved) && resolved is not null)
            {
                app.ExecutablePath = resolved.TargetPath;
                app.Arguments = resolved.Arguments;
                app.WorkingDirectory = resolved.WorkingDirectory;
            }

            return app;
        }

        if (Array.IndexOf(ExecutableExtensions, extension) >= 0)
        {
            return new AppItem(Guid.NewGuid().ToString("N"), target)
            {
                ExecutablePath = extension == ".exe" ? target : null,
            };
        }

        if (!fileSystem.Exists(target))
        {
            // Nothing on disk and not a known executable: refuse rather than pin a dead entry.
            return null;
        }

        return new FileItem(Guid.NewGuid().ToString("N"), target);
    }

    /// <summary>Creates items for a batch of paths, silently skipping the ones that cannot be pinned.</summary>
    public static IReadOnlyList<PinnedItem> CreateFromPaths(
        IEnumerable<string>? paths,
        IFileSystemProbe fileSystem,
        IShortcutResolver? shortcuts = null)
    {
        var result = new List<PinnedItem>();
        if (paths is null)
        {
            return result;
        }

        foreach (var path in paths)
        {
            var item = CreateFromPath(path, fileSystem, shortcuts);
            if (item is not null)
            {
                result.Add(item);
            }
        }

        return result;
    }
}
