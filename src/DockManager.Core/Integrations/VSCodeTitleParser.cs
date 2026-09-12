namespace DockManager.Core.Integrations;

/// <summary>
/// Parses Visual Studio Code window titles ("README.md - my-app - Visual Studio Code") into the
/// open document and the project/workspace name. Pure string logic so it is fully unit tested; the
/// integration only falls back to the raw title when parsing yields nothing.
/// </summary>
public static class VSCodeTitleParser
{
    private static readonly string[] Suffixes =
    [
        " - Visual Studio Code Insiders",
        " - Visual Studio Code",
        " - Code - Insiders",
        " - Code",
    ];

    public static (string? Document, string? Project) Parse(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (null, null);
        }

        var trimmed = title.Trim();

        foreach (var suffix in Suffixes)
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^suffix.Length];
                break;
            }
        }

        trimmed = trimmed.Trim();
        if (trimmed.Length == 0)
        {
            return (null, null);
        }

        var parts = trimmed.Split(" - ");
        if (parts.Length == 1)
        {
            var single = Clean(parts[0]);
            return (null, single.Length > 0 ? single : null);
        }

        var document = Clean(parts[0]);
        var project = Clean(parts[^1]);

        return (
            document.Length > 0 ? document : null,
            project.Length > 0 ? project : null);
    }

    /// <summary>Removes VS Code's dirty markers from a title segment.</summary>
    private static string Clean(string segment)
        => segment.Trim().TrimStart('●', '•', '*').Trim();
}
