using DockManager.Core.Dock;
using DockManager.Core.Items;
using DockManager.Core.Persistence;
using DockManager.Core.Settings;

namespace DockManager.Core.Tests;

public sealed class JsonRepositoryTests : IDisposable
{
    private readonly string _root;
    private readonly DefaultStoragePaths _paths;

    public JsonRepositoryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "dock-manager-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _paths = new DefaultStoragePaths(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public void Loading_without_a_file_yields_an_empty_dock()
    {
        var repository = new JsonItemRepository(_paths);
        Assert.False(File.Exists(repository.FilePath));
        Assert.Empty(repository.Load());
    }

    [Fact]
    public void Save_then_load_round_trips_every_kind_in_order()
    {
        var repository = new JsonItemRepository(_paths);
        var items = new List<PinnedItem>
        {
            new AppItem("app-1", @"C:\apps\chrome.exe", "Chrome")
            {
                ExecutablePath = @"C:\apps\chrome.exe",
                Arguments = "--new-window",
                WorkingDirectory = @"C:\apps",
            },
            new AppItem("app-2", @"C:\Start Menu\Code.lnk"),
            new FileItem("file-1", @"C:\data\report.pdf"),
            new FolderItem("folder-1", @"C:\data\projects", "Projects"),
        };

        repository.Save(items);
        Assert.True(File.Exists(repository.FilePath));
        Assert.DoesNotContain(Directory.GetFiles(_root), file => file.EndsWith(".tmp", StringComparison.Ordinal));

        var loaded = repository.Load();

        Assert.Equal(items.Count, loaded.Count);
        Assert.Equal(items.Select(item => item.Id), loaded.Select(item => item.Id));
        Assert.Equal(items.Select(item => item.Kind), loaded.Select(item => item.Kind));

        var app = Assert.IsType<AppItem>(loaded[0]);
        Assert.Equal("--new-window", app.Arguments);
        Assert.Equal(@"C:\apps", app.WorkingDirectory);
        Assert.Equal(@"C:\apps\chrome.exe", app.ExecutablePath);
        Assert.Equal("Chrome", app.DisplayName);

        var folder = Assert.IsType<FolderItem>(loaded[3]);
        Assert.Equal("Projects", folder.DisplayName);
    }

    [Fact]
    public void Saving_an_empty_list_is_allowed()
    {
        var repository = new JsonItemRepository(_paths);
        repository.Save([]);
        Assert.Empty(repository.Load());
    }

    [Fact]
    public void A_corrupt_file_is_moved_aside_instead_of_crashing()
    {
        File.WriteAllText(_paths.ItemsFile, "{ this is not json ");

        var repository = new JsonItemRepository(_paths);
        Assert.Empty(repository.Load());
        Assert.False(File.Exists(_paths.ItemsFile));
        Assert.Contains(Directory.GetFiles(_root), file => file.Contains(".corrupt-", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_kinds_degrade_to_files_instead_of_failing_the_whole_list()
    {
        File.WriteAllText(
            _paths.ItemsFile,
            """
            {
              "version": 1,
              "items": [
                { "id": "a", "kind": "Widget", "targetPath": "C:\\data\\thing.bin" },
                { "id": "b", "kind": "File", "targetPath": "C:\\data\\report.pdf" }
              ]
            }
            """);

        var loaded = new JsonItemRepository(_paths).Load();

        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, item => Assert.Equal(PinnedItemKind.File, item.Kind));
    }

    [Fact]
    public void Duplicate_and_target_less_entries_are_skipped_on_load()
    {
        File.WriteAllText(
            _paths.ItemsFile,
            """
            {
              "version": 1,
              "items": [
                { "id": "a", "kind": "File", "targetPath": "C:\\data\\report.pdf" },
                { "id": "b", "kind": "File", "targetPath": "c:/DATA/report.pdf" },
                { "id": "c", "kind": "File" },
                { "kind": "Folder", "targetPath": "C:\\data\\projects" }
              ]
            }
            """);

        var loaded = new JsonItemRepository(_paths).Load();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("a", loaded[0].Id);
        Assert.False(string.IsNullOrWhiteSpace(loaded[1].Id));
    }

    [Fact]
    public void Settings_round_trip_preserves_values()
    {
        var repository = new JsonSettingsRepository(_paths);
        Assert.Null(repository.Load());

        repository.Save(new DockSettings
        {
            Edge = DockEdge.Right,
            IconSize = 40,
            AutoHide = false,
            HideDelayMs = 900,
            LaunchAtStartup = true,
            PanelOpacity = 0.5,
        });

        var loaded = repository.Load();
        Assert.NotNull(loaded);
        Assert.Equal(DockEdge.Right, loaded!.Edge);
        Assert.Equal(40, loaded.IconSize);
        Assert.False(loaded.AutoHide);
        Assert.Equal(900, loaded.HideDelayMs);
        Assert.True(loaded.LaunchAtStartup);
        Assert.Equal(0.5, loaded.PanelOpacity);
    }

    [Fact]
    public void Settings_are_clamped_when_saved()
    {
        var repository = new JsonSettingsRepository(_paths);
        repository.Save(new DockSettings { IconSize = 5000, HideDelayMs = -20, PanelOpacity = 0.01 });

        var loaded = repository.Load()!;
        Assert.Equal(DockLayoutMetrics.MaxIconSize, loaded.IconSize);
        Assert.Equal(0, loaded.HideDelayMs);
        Assert.Equal(0.35, loaded.PanelOpacity);
    }

    [Fact]
    public void Corrupt_settings_are_ignored_rather_than_thrown()
    {
        File.WriteAllText(_paths.SettingsFile, "not json at all");

        var repository = new JsonSettingsRepository(_paths);
        Assert.Null(repository.Load());
    }

    [Fact]
    public void Atomic_write_replaces_the_previous_content()
    {
        AtomicFileWriter.Write(_paths.SettingsFile, "first");
        AtomicFileWriter.Write(_paths.SettingsFile, "second");

        Assert.Equal("second", File.ReadAllText(_paths.SettingsFile));
    }
}
