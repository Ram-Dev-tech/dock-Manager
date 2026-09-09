using DockManager.Core.Dock;
using DockManager.Core.Shell;

namespace DockManager.Core.Items;

/// <summary>
/// In-memory ordered collection of pinned items. Items are kept grouped by section (applications
/// first, then files and folders) and reordered inside their own section, which is what makes the
/// dock layout predictable.
/// Not thread safe by design: the dock touches it only from the UI thread.
/// </summary>
public sealed class ItemStore
{
    private readonly List<PinnedItem> _items = [];

    /// <summary>Raised after any mutation. The dock re-renders from this single event.</summary>
    public event EventHandler? Changed;

    /// <summary>All items in paint order.</summary>
    public IReadOnlyList<PinnedItem> Items => _items;

    public int Count => _items.Count;

    public IReadOnlyList<PinnedItem> GetItems(DockSection section)
        => _items.Where(item => item.Section == section).ToList();

    public int CountOf(DockSection section) => _items.Count(item => item.Section == section);

    public PinnedItem? Get(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        foreach (var item in _items)
        {
            if (string.Equals(item.Id, id, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    public PinnedItem? FindByTarget(string? targetPath)
    {
        var key = PathNormalizer.NormalizeKey(targetPath);
        if (key.Length == 0)
        {
            return null;
        }

        foreach (var item in _items)
        {
            if (PathNormalizer.NormalizeKey(item.TargetPath) == key)
            {
                return item;
            }
        }

        return null;
    }

    public bool ContainsTarget(string? targetPath) => FindByTarget(targetPath) is not null;

    /// <summary>Adds an item at the end of its section. Returns false when it is already pinned.</summary>
    public bool Add(PinnedItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.TargetPath))
        {
            return false;
        }

        if (ContainsTarget(item.TargetPath))
        {
            return false;
        }

        _items.Insert(EndIndexOfSection(item.Section), item);
        OnChanged();
        return true;
    }

    public int AddRange(IEnumerable<PinnedItem>? items)
    {
        if (items is null)
        {
            return 0;
        }

        var added = 0;
        foreach (var item in items)
        {
            if (Add(item))
            {
                added++;
            }
        }

        return added;
    }

    public bool Remove(string? id)
    {
        var item = Get(id);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        OnChanged();
        return true;
    }

    /// <summary>Removes every item whose target no longer exists. Used to clean up on demand.</summary>
    public int RemoveMissing(IFileSystemProbe fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        var missing = _items
            .Where(item => item.GetAvailability(fileSystem) != ItemAvailability.Available)
            .ToList();

        if (missing.Count == 0)
        {
            return 0;
        }

        foreach (var item in missing)
        {
            _items.Remove(item);
        }

        OnChanged();
        return missing.Count;
    }

    /// <summary>Moves an item to <paramref name="newIndexInSection"/> within its own section.</summary>
    public bool Move(string? id, int newIndexInSection)
    {
        var item = Get(id);
        if (item is null)
        {
            return false;
        }

        var sectionItems = GetItems(item.Section).ToList();
        var currentIndex = sectionItems.IndexOf(item);
        if (currentIndex < 0)
        {
            return false;
        }

        var target = Math.Clamp(newIndexInSection, 0, sectionItems.Count - 1);
        if (target == currentIndex)
        {
            return false;
        }

        sectionItems.RemoveAt(currentIndex);
        sectionItems.Insert(target, item);
        RebuildSection(item.Section, sectionItems);
        OnChanged();
        return true;
    }

    public bool MoveUp(string? id) => MoveRelative(id, -1);

    public bool MoveDown(string? id) => MoveRelative(id, 1);

    /// <summary>Replaces the contents, restoring the section grouping invariant.</summary>
    public void ReplaceAll(IEnumerable<PinnedItem>? items)
    {
        _items.Clear();
        if (items is not null)
        {
            var stable = items.Where(item => item is not null).ToList();
            _items.AddRange(stable.Where(item => item.Section == DockSection.Applications));
            _items.AddRange(stable.Where(item => item.Section != DockSection.Applications));
        }

        OnChanged();
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        OnChanged();
    }

    private bool MoveRelative(string? id, int delta)
    {
        var item = Get(id);
        if (item is null)
        {
            return false;
        }

        var sectionItems = GetItems(item.Section);
        var index = -1;
        for (var i = 0; i < sectionItems.Count; i++)
        {
            if (ReferenceEquals(sectionItems[i], item))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return false;
        }

        return Move(id, index + delta);
    }

    private int EndIndexOfSection(DockSection section)
    {
        var last = -1;
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Section == section)
            {
                last = i;
            }
        }

        if (last >= 0)
        {
            return last + 1;
        }

        return section == DockSection.Applications ? 0 : CountOf(DockSection.Applications);
    }

    private void RebuildSection(DockSection section, IReadOnlyList<PinnedItem> orderedSectionItems)
    {
        var rebuilt = new List<PinnedItem>(_items.Count);
        var inserted = false;

        foreach (var existing in _items)
        {
            if (existing.Section == section)
            {
                if (!inserted)
                {
                    rebuilt.AddRange(orderedSectionItems);
                    inserted = true;
                }
            }
            else
            {
                rebuilt.Add(existing);
            }
        }

        if (!inserted)
        {
            rebuilt.AddRange(orderedSectionItems);
        }

        _items.Clear();
        _items.AddRange(rebuilt);
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
