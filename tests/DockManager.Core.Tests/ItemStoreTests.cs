using DockManager.Core.Dock;
using DockManager.Core.Items;
using DockManager.Core.Tests.Support;

namespace DockManager.Core.Tests;

public class ItemStoreTests
{
    private static AppItem App(string path) => new(Guid.NewGuid().ToString("N"), path) { ExecutablePath = path };

    private static FileItem File(string path) => new(Guid.NewGuid().ToString("N"), path);

    private static FolderItem Folder(string path) => new(Guid.NewGuid().ToString("N"), path);

    [Fact]
    public void Add_keeps_applications_before_files()
    {
        var store = new ItemStore();

        store.Add(File(@"C:\data\notes.txt"));
        store.Add(App(@"C:\apps\chrome.exe"));
        store.Add(Folder(@"C:\data\projects"));
        store.Add(App(@"C:\apps\code.exe"));

        Assert.Equal(
            [@"C:\apps\chrome.exe", @"C:\apps\code.exe", @"C:\data\notes.txt", @"C:\data\projects"],
            store.Items.Select(item => item.TargetPath).ToArray());

        Assert.Equal(2, store.CountOf(DockSection.Applications));
        Assert.Equal(2, store.CountOf(DockSection.Files));
    }

    [Fact]
    public void Add_rejects_duplicates_and_empty_targets()
    {
        var store = new ItemStore();
        Assert.True(store.Add(App(@"C:\apps\chrome.exe")));
        Assert.False(store.Add(App(@"c:/apps/CHROME.exe")));
        Assert.False(store.Add(new FileItem("id", "   ")));
        Assert.False(store.Add(null));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Add_raises_changed_once_per_accepted_item()
    {
        var store = new ItemStore();
        var raised = 0;
        store.Changed += (_, _) => raised++;

        store.Add(App(@"C:\apps\chrome.exe"));
        store.Add(App(@"C:\apps\chrome.exe"));

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Remove_deletes_only_the_requested_item()
    {
        var store = new ItemStore();
        var keep = App(@"C:\apps\chrome.exe");
        var drop = App(@"C:\apps\code.exe");
        store.Add(keep);
        store.Add(drop);

        Assert.True(store.Remove(drop.Id));
        Assert.False(store.Remove(drop.Id));
        Assert.False(store.Remove("does-not-exist"));
        Assert.Same(keep, Assert.Single(store.Items));
    }

    [Fact]
    public void Move_reorders_within_the_items_own_section()
    {
        var store = new ItemStore();
        var chrome = App(@"C:\apps\chrome.exe");
        var code = App(@"C:\apps\code.exe");
        var spotify = App(@"C:\apps\spotify.exe");
        var notes = File(@"C:\data\notes.txt");
        store.Add(chrome);
        store.Add(code);
        store.Add(spotify);
        store.Add(notes);

        Assert.True(store.Move(spotify.Id, 0));

        Assert.Equal(
            [@"C:\apps\spotify.exe", @"C:\apps\chrome.exe", @"C:\apps\code.exe", @"C:\data\notes.txt"],
            store.Items.Select(item => item.TargetPath).ToArray());
    }

    [Fact]
    public void Move_clamps_out_of_range_indexes_and_reports_no_op_moves()
    {
        var store = new ItemStore();
        var first = App(@"C:\apps\chrome.exe");
        var second = App(@"C:\apps\code.exe");
        store.Add(first);
        store.Add(second);

        Assert.False(store.Move(first.Id, 0));
        Assert.True(store.Move(first.Id, 99));
        Assert.Equal(second.Id, store.Items[0].Id);
        Assert.Equal(first.Id, store.Items[1].Id);
        Assert.False(store.Move("missing", 1));
    }

    [Fact]
    public void MoveUp_and_MoveDown_step_through_the_section()
    {
        var store = new ItemStore();
        var a = App(@"C:\apps\a.exe");
        var b = App(@"C:\apps\b.exe");
        var c = App(@"C:\apps\c.exe");
        store.Add(a);
        store.Add(b);
        store.Add(c);

        Assert.True(store.MoveUp(c.Id));
        Assert.Equal([a.Id, c.Id, b.Id], store.Items.Select(item => item.Id).ToArray());

        Assert.False(store.MoveUp(a.Id));
        Assert.True(store.MoveDown(a.Id));
        Assert.Equal([c.Id, a.Id, b.Id], store.Items.Select(item => item.Id).ToArray());

        Assert.False(store.MoveDown(b.Id));
    }

    [Fact]
    public void ReplaceAll_restores_section_grouping()
    {
        var store = new ItemStore();
        var notes = File(@"C:\data\notes.txt");
        var chrome = App(@"C:\apps\chrome.exe");

        store.ReplaceAll([notes, chrome]);

        Assert.Equal(chrome.Id, store.Items[0].Id);
        Assert.Equal(notes.Id, store.Items[1].Id);
    }

    [Fact]
    public void RemoveMissing_drops_only_unavailable_items()
    {
        var fileSystem = new FakeFileSystem()
            .WithFile(@"C:\apps\chrome.exe")
            .WithFile(@"C:\data\gone.txt");

        var store = new ItemStore();
        var chrome = App(@"C:\apps\chrome.exe");
        var gone = File(@"C:\data\gone.txt");
        store.Add(chrome);
        store.Add(gone);

        fileSystem.Remove(@"C:\data\gone.txt");

        Assert.Equal(1, store.RemoveMissing(fileSystem));
        Assert.Same(chrome, Assert.Single(store.Items));
        Assert.Equal(0, store.RemoveMissing(fileSystem));
    }

    [Fact]
    public void FindByTarget_ignores_case_and_separators()
    {
        var store = new ItemStore();
        store.Add(App(@"C:\apps\chrome.exe"));

        Assert.NotNull(store.FindByTarget(@"c:/APPS/Chrome.exe"));
        Assert.Null(store.FindByTarget(@"C:\apps\code.exe"));
        Assert.True(store.ContainsTarget(@"C:\APPS\CHROME.EXE"));
    }

    [Fact]
    public void Clear_empties_the_store_and_notifies()
    {
        var store = new ItemStore();
        store.Add(App(@"C:\apps\chrome.exe"));
        var raised = 0;
        store.Changed += (_, _) => raised++;

        store.Clear();
        store.Clear();

        Assert.Empty(store.Items);
        Assert.Equal(1, raised);
    }
}
