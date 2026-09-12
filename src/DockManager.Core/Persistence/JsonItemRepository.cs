using System.Text.Json;
using DockManager.Core.Diagnostics;
using DockManager.Core.Items;
using DockManager.Core.Shell;

namespace DockManager.Core.Persistence;

/// <summary>
/// Persists pinned items as JSON in the user's app data folder. Loading is defensive: a missing,
/// empty or corrupt file yields an empty dock rather than a crash, and a corrupt file is moved
/// aside so the data is not lost.
/// </summary>
public sealed class JsonItemRepository : IItemRepository
{
    private static readonly JsonSerializerOptions Options = JsonFile.Options;

    private readonly IDockLogger? _logger;

    public JsonItemRepository(IStoragePaths paths, IDockLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _logger = logger;
        FilePath = paths.ItemsFile;
    }

    public string FilePath { get; }

    public IReadOnlyList<PinnedItem> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var document = JsonSerializer.Deserialize<ItemStoreDocument>(json, Options);
            if (document?.Items is null)
            {
                return [];
            }

            var items = new List<PinnedItem>(document.Items.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var dto in document.Items)
            {
                var item = dto?.ToItem();
                if (item is null)
                {
                    continue;
                }

                // Organizational entries have no target and are never duplicates of each other.
                if (!item.Kind.IsOrganizational())
                {
                    var key = PathNormalizer.NormalizeKey(item.TargetPath);
                    if (key.Length == 0 || !seen.Add(key))
                    {
                        continue;
                    }
                }

                items.Add(item);
            }

            return items;
        }
        catch (JsonException ex)
        {
            var backup = AtomicFileWriter.MoveAside(FilePath);
            _logger.Warn($"Pinned items file was corrupt{(backup is null ? "." : $", moved to {backup}.")}", ex);
            return [];
        }
        catch (IOException ex)
        {
            _logger.Warn("Could not read the pinned items file.", ex);
            return [];
        }
    }

    public void Save(IReadOnlyList<PinnedItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var document = new ItemStoreDocument
        {
            Version = 1,
            Items = items.Select(ItemDto.From).ToList(),
        };

        AtomicFileWriter.Write(FilePath, JsonSerializer.Serialize(document, Options));
    }
}
