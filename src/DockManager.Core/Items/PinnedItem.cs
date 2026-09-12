using DockManager.Core.Dock;
using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>
/// Base class for everything that can be pinned to the dock. Subclasses only differ in how they
/// are validated and launched, which keeps the store, the renderer and the persistence layer
/// completely kind agnostic.
/// </summary>
public abstract class PinnedItem
{
    protected PinnedItem(string id, string targetPath, string? displayName = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
        TargetPath = targetPath ?? string.Empty;
        DisplayName = displayName;
    }

    /// <summary>Stable identifier used by the UI and by the persisted store.</summary>
    public string Id { get; }

    /// <summary>What to open: an executable, a shortcut, a file or a folder.</summary>
    public string TargetPath { get; set; }

    /// <summary>Optional user supplied label. Falls back to the file name.</summary>
    public string? DisplayName { get; set; }

    public DateTime PinnedAtUtc { get; set; } = DateTime.UtcNow;

    public abstract PinnedItemKind Kind { get; }

    /// <summary>Which section of the dock this item is rendered in.</summary>
    public abstract DockSection Section { get; }

    public virtual string EffectiveName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return DisplayName!;
            }

            return Kind == PinnedItemKind.Application
                ? PathNormalizer.DisplayName(TargetPath)
                : PathNormalizer.FileName(TargetPath);
        }
    }

    /// <summary>Key used to match this pinned item against a running application. Empty for non apps.</summary>
    public virtual string RunningMatchKey => string.Empty;

    public abstract ItemAvailability GetAvailability(IFileSystemProbe fileSystem);

    /// <summary>Returns how to open this item, or <c>null</c> when it cannot be opened.</summary>
    public abstract LaunchRequest? CreateLaunchRequest(IFileSystemProbe fileSystem);

    /// <summary>Creates a copy so callers can mutate a candidate without touching the store.</summary>
    public abstract PinnedItem Clone();

    public override string ToString() => $"{Kind}: {EffectiveName} ({TargetPath})";
}
