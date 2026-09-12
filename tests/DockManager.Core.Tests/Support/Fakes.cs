using DockManager.Core.Shell;

namespace DockManager.Core.Tests.Support;

/// <summary>In-memory file system so pinned items can be tested without touching the disk.</summary>
public sealed class FakeFileSystem : IFileSystemProbe
{
    private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    public FakeFileSystem WithFile(string path)
    {
        _files.Add(PathNormalizer.NormalizeKey(path));
        return this;
    }

    public FakeFileSystem WithDirectory(string path)
    {
        _directories.Add(PathNormalizer.NormalizeKey(path));
        return this;
    }

    public FakeFileSystem Remove(string path)
    {
        _files.Remove(PathNormalizer.NormalizeKey(path));
        _directories.Remove(PathNormalizer.NormalizeKey(path));
        return this;
    }

    public bool Exists(string path)
    {
        var key = PathNormalizer.NormalizeKey(path);
        return _files.Contains(key) || _directories.Contains(key);
    }

    public bool IsDirectory(string path) => _directories.Contains(PathNormalizer.NormalizeKey(path));

    public bool IsFile(string path) => _files.Contains(PathNormalizer.NormalizeKey(path));
}

public sealed class FakeShortcutResolver : IShortcutResolver
{
    private readonly Dictionary<string, ShortcutTarget> _targets = new(StringComparer.OrdinalIgnoreCase);

    public FakeShortcutResolver With(string shortcutPath, ShortcutTarget target)
    {
        _targets[shortcutPath] = target;
        return this;
    }

    public bool TryResolve(string shortcutPath, out ShortcutTarget? target)
        => _targets.TryGetValue(shortcutPath, out target);
}
