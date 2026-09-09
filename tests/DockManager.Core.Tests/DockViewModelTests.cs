using DockManager.Core.Dock;
using DockManager.Core.Items;
using DockManager.Core.Tests.Support;
using DockManager.Core.Ui;
using DockManager.Core.Windows;

namespace DockManager.Core.Tests;

public class DockViewModelTests
{
    private const string Chrome = @"C:\apps\chrome.exe";
    private const string Code = @"C:\apps\code.exe";
    private const string Report = @"C:\data\report.pdf";
    private const string Projects = @"C:\data\projects";

    private readonly FakeFileSystem _fileSystem = new FakeFileSystem()
        .WithFile(Chrome)
        .WithFile(Code)
        .WithFile(Report)
        .WithDirectory(Projects);

    private readonly ItemStore _store = new();

    private DockViewModel CreateViewModel()
    {
        _store.Add(new AppItem("chrome", Chrome) { ExecutablePath = Chrome });
        _store.Add(new AppItem("code", Code) { ExecutablePath = Code });
        _store.Add(new FileItem("report", Report));
        _store.Add(new FolderItem("projects", Projects));
        return new DockViewModel(_store, _fileSystem, DockLayoutMetrics.ForIconSize(32));
    }

    private static WindowInfo Window(int handle, string exe) => new(new IntPtr(handle), exe, handle);

    [Fact]
    public void Items_are_split_into_the_two_rendered_sections()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(2, viewModel.Applications.Count);
        Assert.Equal(2, viewModel.Files.Count);
        Assert.Equal(2, viewModel.Plan.ApplicationCount);
        Assert.Equal(2, viewModel.Plan.FileCount);
        Assert.Equal(4, viewModel.Plan.ApplicationCount + viewModel.Plan.FileCount);
    }

    [Fact]
    public void Flattened_indexes_map_onto_the_rendered_list()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(0, viewModel.FlattenedIndexOf("chrome"));
        Assert.Equal(1, viewModel.FlattenedIndexOf("code"));
        Assert.Equal(2, viewModel.FlattenedIndexOf("report"));
        Assert.Equal(3, viewModel.FlattenedIndexOf("projects"));
        Assert.Equal(-1, viewModel.FlattenedIndexOf("nope"));

        Assert.Equal("chrome", viewModel.GetFlattened(0)!.Id);
        Assert.Equal("projects", viewModel.GetFlattened(3)!.Id);
        Assert.Null(viewModel.GetFlattened(4));
        Assert.Null(viewModel.GetFlattened(-1));
    }

    [Fact]
    public void Running_and_active_state_is_reported_per_item()
    {
        var viewModel = CreateViewModel();
        var index = RunningAppIndex.Build(
        [
            Window(10, Chrome),
            Window(11, Chrome),
            Window(20, Code),
        ]);

        viewModel.RefreshRunning(index, activeWindowHandle: new IntPtr(20));

        var chrome = viewModel.Find("chrome")!;
        var code = viewModel.Find("code")!;
        var report = viewModel.Find("report")!;

        Assert.True(chrome.IsRunning);
        Assert.False(chrome.IsActive);
        Assert.Equal(2, chrome.WindowCount);
        Assert.Contains("running", chrome.Tooltip, StringComparison.Ordinal);

        Assert.True(code.IsActive);
        Assert.Equal(PinnedItemRunState.Active, code.RunState);

        // Files are never "running".
        Assert.False(report.IsRunning);
        Assert.Equal(PinnedItemRunState.NotRunning, report.RunState);
    }

    [Fact]
    public void A_pinned_shortcut_matches_the_running_executable()
    {
        var store = new ItemStore();
        store.Add(new AppItem("chrome", @"C:\Start Menu\Google Chrome.lnk") { ExecutablePath = Chrome });
        var viewModel = new DockViewModel(store, _fileSystem, DockLayoutMetrics.ForIconSize(32));

        viewModel.RefreshRunning(RunningAppIndex.Build([Window(10, Chrome)]), IntPtr.Zero);

        Assert.True(viewModel.Find("chrome")!.IsRunning);
    }

    [Fact]
    public void Store_changes_flow_into_the_view_model()
    {
        var viewModel = CreateViewModel();
        var raised = 0;
        viewModel.ContentChanged += (_, _) => raised++;

        _store.Add(new AppItem("spotify", @"C:\apps\spotify.exe") { ExecutablePath = @"C:\apps\spotify.exe" });
        Assert.Equal(3, viewModel.Applications.Count);

        _store.Remove("report");
        Assert.Single(viewModel.Files);
        Assert.Null(viewModel.Find("report"));

        Assert.Equal(2, raised);
        Assert.Equal(3, viewModel.Plan.ApplicationCount);
    }

    [Fact]
    public void Removing_an_item_keeps_the_other_view_models_stable()
    {
        var viewModel = CreateViewModel();
        var chrome = viewModel.Find("chrome")!;

        _store.Remove("code");

        Assert.Same(chrome, viewModel.Find("chrome"));
    }

    [Fact]
    public void Availability_is_refreshed_without_rebuilding_the_list()
    {
        var viewModel = CreateViewModel();
        var report = viewModel.Find("report")!;
        Assert.True(report.IsAvailable);

        _fileSystem.Remove(Report);
        viewModel.RefreshAvailability();

        Assert.True(report.IsMissing);
        Assert.False(report.IsAvailable);
        Assert.Contains("unavailable", report.Tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_application_is_not_reported_as_running()
    {
        var store = new ItemStore();
        store.Add(new AppItem("gone", @"C:\apps\gone.exe") { ExecutablePath = @"C:\apps\gone.exe" });
        var viewModel = new DockViewModel(store, _fileSystem, DockLayoutMetrics.ForIconSize(32));

        viewModel.RefreshRunning(RunningAppIndex.Build([Window(1, @"C:\apps\gone.exe")]), new IntPtr(1));

        var item = viewModel.Find("gone")!;
        Assert.True(item.IsMissing);
        Assert.False(item.IsRunning);
        Assert.Equal(PinnedItemRunState.NotRunning, item.RunState);
    }

    [Fact]
    public void Changing_the_icon_size_rebuilds_the_layout()
    {
        var viewModel = CreateViewModel();
        var before = viewModel.Plan;
        var raised = 0;
        viewModel.ContentChanged += (_, _) => raised++;

        viewModel.ApplyMetrics(DockLayoutMetrics.ForIconSize(40));

        Assert.NotEqual(before.Width, viewModel.Plan.Width);
        Assert.Equal(DockLayoutMetrics.ForIconSize(40).DockWidth, viewModel.Plan.Width);
        Assert.Equal(1, raised);

        // Applying the same metrics twice is a no op.
        viewModel.ApplyMetrics(DockLayoutMetrics.ForIconSize(40));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Reordering_goes_through_the_store_and_updates_the_layout()
    {
        var viewModel = CreateViewModel();

        Assert.True(_store.Move("code", 0));

        Assert.Equal("code", viewModel.Applications[0].Id);
        Assert.Equal(0, viewModel.FlattenedIndexOf("code"));
    }
}
