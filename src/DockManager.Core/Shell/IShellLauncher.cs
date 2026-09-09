namespace DockManager.Core.Shell;

/// <summary>Outcome of a launch attempt.</summary>
public sealed record LaunchResult(bool Success, string? Error = null)
{
    public static LaunchResult Ok { get; } = new(true);

    public static LaunchResult Failed(string error) => new(false, error);
}

/// <summary>Opens apps, files and folders through the Windows shell.</summary>
public interface IShellLauncher
{
    LaunchResult Launch(LaunchRequest request);

    /// <summary>Opens the folder that contains <paramref name="path"/> with the item selected.</summary>
    LaunchResult OpenContainingFolder(string path);
}
