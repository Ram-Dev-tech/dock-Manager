using DockManager.Core.Dock;
using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>
/// A pinned application. <see cref="TargetPath"/> is what gets launched (an <c>.exe</c> or the
/// <c>.lnk</c> the user pinned) while <see cref="ExecutablePath"/> is the resolved executable used
/// to recognise the app in the running window list.
/// </summary>
public sealed class AppItem : PinnedItem
{
    public AppItem(string id, string targetPath, string? displayName = null)
        : base(id, targetPath, displayName)
    {
    }

    /// <summary>Resolved executable of the app, used for running-application matching.</summary>
    public string? ExecutablePath { get; set; }

    public string? Arguments { get; set; }

    public string? WorkingDirectory { get; set; }

    public override PinnedItemKind Kind => PinnedItemKind.Application;

    public override DockSection Section => DockSection.Applications;

    public override string RunningMatchKey
    {
        get
        {
            var candidate = !string.IsNullOrWhiteSpace(ExecutablePath) ? ExecutablePath : TargetPath;
            return IsShellTarget ? string.Empty : PathNormalizer.NormalizeKey(candidate);
        }
    }

    /// <summary>True for shell pseudo targets such as <c>shell:AppsFolder\...</c> (store apps).</summary>
    public bool IsShellTarget
        => TargetPath.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)
           || TargetPath.StartsWith("explorer.exe shell:", StringComparison.OrdinalIgnoreCase);

    public override ItemAvailability GetAvailability(IFileSystemProbe fileSystem)
    {
        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            return ItemAvailability.Invalid;
        }

        if (IsShellTarget)
        {
            // The shell resolves these at launch time; existence cannot be checked cheaply.
            return ItemAvailability.Available;
        }

        return fileSystem.Exists(TargetPath) ? ItemAvailability.Available : ItemAvailability.Missing;
    }

    public override LaunchRequest? CreateLaunchRequest(IFileSystemProbe fileSystem)
    {
        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            return null;
        }

        if (!IsShellTarget && !fileSystem.Exists(TargetPath))
        {
            return null;
        }

        return new LaunchRequest(TargetPath, LaunchVerb.Execute)
        {
            Arguments = string.IsNullOrWhiteSpace(Arguments) ? null : Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory) ? null : WorkingDirectory,
        };
    }

    public override PinnedItem Clone() => new AppItem(Id, TargetPath, DisplayName)
    {
        ExecutablePath = ExecutablePath,
        Arguments = Arguments,
        WorkingDirectory = WorkingDirectory,
        PinnedAtUtc = PinnedAtUtc,
    };
}
