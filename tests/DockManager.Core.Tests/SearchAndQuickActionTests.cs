using DockManager.Core.Dock;
using DockManager.Core.Integrations;
using DockManager.Core.Items;
using DockManager.Core.Tests.Support;
using DockManager.Core.Ui;
using DockManager.Core.Windows;

namespace DockManager.Core.Tests;

public class SearchAndQuickActionTests
{
    [Theory]
    [InlineData("chr")]
    [InlineData("CHR")]
    [InlineData("chrome")]
    public void Matches_items_by_name_ignoring_case(string query)
    {
        var item = new AppItem(null, "C:\\Apps\\chrome.exe", "Chrome") { ExecutablePath = "C:\\Apps\\chrome.exe" };

        Assert.True(ItemSearchFilter.Matches(item, query));
    }

    [Fact]
    public void Matches_by_path_and_resolved_executable()
    {
        var item = new AppItem(null, "C:\\Start Menu\\Visual Studio Code.lnk", null)
        {
            ExecutablePath = "C:\\Program Files\\code.exe",
        };

        Assert.True(ItemSearchFilter.Matches(item, "code.exe"));
        Assert.True(ItemSearchFilter.Matches(item, "Start Menu"));
    }

    [Fact]
    public void Empty_query_matches_openable_items_only()
    {
        Assert.True(ItemSearchFilter.Matches(new FileItem(null, "C:\\notes.txt", null), ""));
        Assert.False(ItemSearchFilter.Matches(new SeparatorItem(null, DockSection.Files), ""));
        Assert.False(ItemSearchFilter.Matches(new GroupHeaderItem(null, "G", DockSection.Files), ""));
    }

    [Fact]
    public void Non_matching_queries_do_not_match()
    {
        var item = new FolderItem(null, "C:\\Projects", null);

        Assert.False(ItemSearchFilter.Matches(item, "zzz"));
        Assert.False(ItemSearchFilter.Matches(null, "anything"));
    }

    [Fact]
    public void Generic_integration_offers_no_quick_actions()
    {
        var integration = new GenericIntegration(new FakeWindowActivator());
        var app = new RunningApp
        {
            Key = "app",
            ExecutablePath = "C:\\Apps\\app.exe",
            Windows = [new WindowInfo(new IntPtr(1), "C:\\Apps\\app.exe", 1)],
        };

        Assert.Empty(integration.GetQuickActions(app));
    }
}
