using DockManager.Core.Dock;
using DockManager.Core.Items;
using DockManager.Core.Persistence;
using DockManager.Core.Tests.Support;

namespace DockManager.Core.Tests;

public class OrganizationTests
{
    private static AppItem App(string path) => new(string.Empty, path) { ExecutablePath = path };

    [Fact]
    public void Groups_and_separators_can_be_added_without_a_target()
    {
        var store = new ItemStore();

        var group = store.AddGroup(DockSection.Applications, "Work");
        var separator = store.AddSeparator(DockSection.Applications);

        Assert.Contains(store.Items, item => ReferenceEquals(item, group));
        Assert.Contains(store.Items, item => ReferenceEquals(item, separator));
    }

    [Fact]
    public void Add_does_not_reject_organizational_items_for_missing_targets()
    {
        var store = new ItemStore();

        Assert.True(store.Add(new SeparatorItem(null, DockSection.Files)));
        Assert.True(store.Add(new GroupHeaderItem(null, "Docs", DockSection.Files)));
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void Find_group_reports_the_nearest_header_above_the_item()
    {
        var store = new ItemStore();
        var work = store.AddGroup(DockSection.Applications, "Work");
        var inside = App("C:\\Apps\\app.exe");
        store.Add(inside);
        store.AddGroup(DockSection.Applications, "Play");

        Assert.Same(work, store.FindGroupOf(inside.Id));
    }

    [Fact]
    public void Items_before_the_first_header_are_ungrouped()
    {
        var store = new ItemStore();
        var loose = App("C:\\Apps\\loose.exe");
        store.Add(loose);
        store.AddGroup(DockSection.Applications, "Work");

        Assert.Null(store.FindGroupOf(loose.Id));
    }

    [Fact]
    public void Move_to_group_places_the_item_at_the_end_of_that_group()
    {
        var store = new ItemStore();
        var work = store.AddGroup(DockSection.Applications, "Work");
        var first = App("C:\\Apps\\first.exe");
        store.Add(first);
        store.AddGroup(DockSection.Applications, "Play");
        var play = store.GetGroups(DockSection.Applications)[1];
        var moved = App("C:\\Apps\\moved.exe");
        store.Add(moved); // appended after "Play" header, so currently inside Play

        Assert.Same(play, store.FindGroupOf(moved.Id));

        Assert.True(store.MoveToGroup(moved.Id, work.Id));

        Assert.Same(work, store.FindGroupOf(moved.Id));
        var order = store.GetItems(DockSection.Applications).Select(item => item.Id).ToList();
        Assert.Equal(work.Id, order[0]);
        Assert.Equal(first.Id, order[1]);
        Assert.Equal(moved.Id, order[2]);
    }

    [Fact]
    public void Removing_a_group_header_keeps_its_items()
    {
        var store = new ItemStore();
        var work = store.AddGroup(DockSection.Applications, "Work");
        var item = App("C:\\Apps\\keep.exe");
        store.Add(item);

        Assert.True(store.Remove(work.Id));

        Assert.Single(store.GetItems(DockSection.Applications));
        Assert.Null(store.FindGroupOf(item.Id));
    }

    [Fact]
    public void Organizational_items_survive_a_persistence_round_trip()
    {
        var root = Path.Combine(Path.GetTempPath(), "dock-manager-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var repository = new JsonItemRepository(new DefaultStoragePaths(root));

            var items = new List<PinnedItem>
            {
                new GroupHeaderItem("g1", "Work", DockSection.Applications),
                new AppItem("a1", "C:\\Apps\\app.exe", null) { ExecutablePath = "C:\\Apps\\app.exe" },
                new SeparatorItem("s1", DockSection.Applications),
            };

            repository.Save(items);
            var loaded = repository.Load();

            Assert.Equal(3, loaded.Count);
            Assert.Equal(PinnedItemKind.GroupHeader, loaded[0].Kind);
            Assert.Equal("Work", loaded[0].EffectiveName);
            Assert.Equal(PinnedItemKind.Separator, loaded[2].Kind);
            Assert.Equal(DockSection.Applications, loaded[2].Section);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public void Remove_missing_never_removes_groups_or_separators()
    {
        var store = new ItemStore();
        var group = store.AddGroup(DockSection.Files, "Docs");
        var separator = store.AddSeparator(DockSection.Files);
        var file = new FileItem(string.Empty, "C:\\gone\\file.txt");
        store.Add(file);

        var removed = store.RemoveMissing(new FakeFileSystem());

        Assert.Equal(1, removed);
        Assert.Contains(store.Items, item => ReferenceEquals(item, group));
        Assert.Contains(store.Items, item => ReferenceEquals(item, separator));
    }

    [Fact]
    public void Organizational_items_are_never_openable()
    {
        var fileSystem = new FakeFileSystem();

        Assert.Null(new SeparatorItem(null, DockSection.Files).CreateLaunchRequest(fileSystem));
        Assert.Null(new GroupHeaderItem(null, "G", DockSection.Files).CreateLaunchRequest(fileSystem));
        Assert.True(PinnedItemKind.Separator.IsOrganizational());
        Assert.False(PinnedItemKind.GroupHeader.IsOpenable());
        Assert.True(PinnedItemKind.Application.IsOpenable());
    }
}
