using DockManager.Core.Integrations;
using DockManager.Core.Tests.Support;
using DockManager.Core.Windows;

namespace DockManager.Core.Tests;

public class GenericIntegrationTests
{
    private static WindowInfo Window(int handle, string title, bool minimized = false)
        => new(new IntPtr(handle), @"C:\apps\spotify.exe", handle) { Title = title, IsMinimized = minimized };

    private static RunningApp App(params WindowInfo[] windows) => new()
    {
        Key = @"c:\apps\spotify.exe",
        ExecutablePath = @"C:\apps\spotify.exe",
        Windows = windows,
        DisplayName = "spotify",
    };

    [Fact]
    public async Task Lists_each_window_as_an_item()
    {
        var integration = new GenericIntegration(new FakeWindowActivator());
        var app = App(Window(1, "Spotify"), Window(2, "Spotify Premium"));

        var items = await integration.GetContentAsync(app, IntegrationOptions.Default, CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("Spotify", items[0].Title);
        Assert.Equal("Spotify Premium", items[1].Title);
        Assert.All(items, item => Assert.Equal(AppContentKind.Window, item.Kind));
    }

    [Fact]
    public async Task Marks_the_foreground_window_as_active()
    {
        var integration = new GenericIntegration(new FakeWindowActivator());
        var app = App(Window(1, "A"), Window(2, "B"));

        var items = await integration.GetContentAsync(
            app,
            IntegrationOptions.Default with { ForegroundWindow = new IntPtr(2) },
            CancellationToken.None);

        Assert.False(items[0].IsActive);
        Assert.True(items[1].IsActive);
    }

    [Fact]
    public async Task Minimized_windows_are_labelled()
    {
        var integration = new GenericIntegration(new FakeWindowActivator());
        var app = App(Window(1, "A", minimized: true));

        var items = await integration.GetContentAsync(app, IntegrationOptions.Default, CancellationToken.None);

        Assert.Equal("Minimized", items[0].Subtitle);
    }

    [Fact]
    public async Task Grouping_collapses_multiple_windows_into_one_entry()
    {
        var integration = new GenericIntegration(new FakeWindowActivator());
        var app = App(Window(1, "A"), Window(2, "B"), Window(3, "C"));

        var items = await integration.GetContentAsync(
            app,
            IntegrationOptions.Default with { GroupMultipleWindows = true },
            CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal("spotify", item.Title);
        Assert.Equal("3 windows", item.Subtitle);
    }

    [Fact]
    public async Task Grouping_does_not_collapse_a_single_window()
    {
        var integration = new GenericIntegration(new FakeWindowActivator());
        var app = App(Window(1, "A"));

        var items = await integration.GetContentAsync(
            app,
            IntegrationOptions.Default with { GroupMultipleWindows = true },
            CancellationToken.None);

        Assert.Equal("A", Assert.Single(items).Title);
    }

    [Fact]
    public async Task Activation_goes_through_the_window_activator()
    {
        var activator = new FakeWindowActivator();
        var integration = new GenericIntegration(activator);
        var app = App(Window(7, "A"));
        var items = await integration.GetContentAsync(app, IntegrationOptions.Default, CancellationToken.None);

        var ok = await integration.ActivateItemAsync(items[0], CancellationToken.None);

        Assert.True(ok);
        Assert.Equal(new[] { new IntPtr(7) }, activator.Activated);
    }

    [Fact]
    public async Task Activation_failure_propagates_as_false()
    {
        var activator = new FakeWindowActivator { Result = false };
        var integration = new GenericIntegration(activator);
        var app = App(Window(7, "A"));
        var items = await integration.GetContentAsync(app, IntegrationOptions.Default, CancellationToken.None);

        Assert.False(await integration.ActivateItemAsync(items[0], CancellationToken.None));
    }

    [Fact]
    public void Generic_integration_never_claims_item_navigation()
    {
        Assert.False(new GenericIntegration(new FakeWindowActivator()).SupportsItemLevelNavigation);
        Assert.Empty(new GenericIntegration(new FakeWindowActivator()).ExecutableNames);
    }
}
