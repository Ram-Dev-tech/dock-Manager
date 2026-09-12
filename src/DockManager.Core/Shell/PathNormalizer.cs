namespace DockManager.Core.Shell;

/// <summary>
/// Path helpers shared by pinning, persistence and running-application matching.
/// Windows paths are case insensitive and may use either separator, so every comparison in the
/// dock goes through <see cref="NormalizeKey"/>.
/// </summary>
public static class PathNormalizer
{
    /// <summary>Canonical comparison key for a Windows path.</summary>
    public static string NormalizeKey(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim().Replace('/', '\\');
        while (trimmed.Length > 3 && trimmed.EndsWith("\\\\", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^1];
        }

        return trimmed.TrimEnd('\\').ToLowerInvariant();
    }

    public static string FileName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim().Replace('/', '\\').TrimEnd('\\');
        var index = normalized.LastIndexOf('\\');
        return index >= 0 ? normalized[(index + 1)..] : normalized;
    }

    /// <summary>Display name without the extension, used for tooltips and labels.</summary>
    public static string DisplayName(string? path)
    {
        var name = FileName(path);
        if (name.Length == 0)
        {
            return string.IsNullOrWhiteSpace(path) ? "Unknown" : path!;
        }

        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }

    public static string Extension(string? path)
    {
        var name = FileName(path);
        var dot = name.LastIndexOf('.');
        return dot >= 0 ? name[dot..].ToLowerInvariant() : string.Empty;
    }

    public static bool SameExecutable(string? a, string? b)
    {
        var keyA = NormalizeKey(a);
        var keyB = NormalizeKey(b);
        if (keyA.Length == 0 || keyB.Length == 0)
        {
            return false;
        }

        if (string.Equals(keyA, keyB, StringComparison.Ordinal))
        {
            return true;
        }

        // Fall back to the file name: shortcuts, store apps and portable installs frequently point
        // at the same executable through a different path.
        return string.Equals(FileName(keyA), FileName(keyB), StringComparison.Ordinal);
    }
}
