namespace DockManager.Core.Shell;

/// <summary>How the shell should be asked to open a target.</summary>
public enum LaunchVerb
{
    /// <summary>Default shell action (opens files with their associated application).</summary>
    Open,

    /// <summary>Run an executable directly.</summary>
    Execute,

    /// <summary>Open a folder in File Explorer.</summary>
    Explore,
}

/// <summary>
/// A resolved request to open something. The platform layer turns this into a ShellExecute call.
/// </summary>
public sealed record LaunchRequest(string FilePath, LaunchVerb Verb)
{
    public string? Arguments { get; init; }

    public string? WorkingDirectory { get; init; }
}
