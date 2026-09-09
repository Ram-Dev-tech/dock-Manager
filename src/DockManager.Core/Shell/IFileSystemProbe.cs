namespace DockManager.Core.Shell;

/// <summary>
/// File system probe used to decide whether a pinned item still exists. Abstracted so tests can
/// simulate deleted files without touching the disk.
/// </summary>
public interface IFileSystemProbe
{
    bool Exists(string path);

    bool IsDirectory(string path);

    bool IsFile(string path);
}
