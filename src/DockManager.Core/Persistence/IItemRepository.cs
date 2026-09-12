using DockManager.Core.Items;

namespace DockManager.Core.Persistence;

/// <summary>Reads and writes the pinned item list.</summary>
public interface IItemRepository
{
    string FilePath { get; }

    IReadOnlyList<PinnedItem> Load();

    void Save(IReadOnlyList<PinnedItem> items);
}
