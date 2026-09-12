using DockManager.Core.Items;

namespace DockManager.Core.Ui;

/// <summary>
/// The dock's lightweight filter: substring matching over item names and paths. It only narrows
/// what the dock itself shows and is deliberately not a system search.
/// </summary>
public static class ItemSearchFilter
{
    /// <summary>True when an item matches the query. An empty query matches every openable item.</summary>
    public static bool Matches(PinnedItem? item, string? query)
    {
        if (item is null || !item.Kind.IsOpenable())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var needle = query.Trim();
        if (Contains(item.EffectiveName, needle) || Contains(item.TargetPath, needle))
        {
            return true;
        }

        if (item is AppItem app && Contains(app.ExecutablePath ?? string.Empty, needle))
        {
            return true;
        }

        return false;
    }

    private static bool Contains(string haystack, string needle)
        => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
