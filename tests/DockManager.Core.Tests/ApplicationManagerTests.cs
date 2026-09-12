using DockManager.Core.Integrations;
using DockManager.Core.Tests.Support;
using DockManager.Core.Windows;

namespace DockManager.Core.Tests;

public class ApplicationManagerTests
{
    private sealed class StubIntegration : IApplicationIntegration
    {
        public StubIntegration(string id, params string[] executables)
        {
            Id = id;
            ExecutableNames = executables;
        }

        public string Id { get; }

        public string DisplayName => Id;

        public IReadOnlyCollection<string> ExecutableNames { get; }

        public bool SupportsItemLevelNavigation => true;

        public Task<IReadOnlyList<AppContentItem>> GetContentAsync(
            RunningApp app,
            IntegrationOptions options,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AppContentItem>>([]);

        public Task<bool> ActivateItemAsync(AppContentItem item, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public IReadOnlyList<QuickAction> GetQuickActions(RunningApp app) => [];
    }

    private static RunningApp App(string executable) => new()
    {
        Key = executable.ToLowerInvariant(),
        ExecutablePath = executable,
        Windows = [new WindowInfo(new IntPtr(1), executable, 1)],
        DisplayName = "app",
    };

    private static ApplicationManager CreateManager()
    {
        var manager = new ApplicationManager(new GenericIntegration(new FakeWindowActivator()));
        manager.Register(new StubIntegration("chrome", "chrome.exe"));
        manager.Register(new StubIntegration("code", "code.exe"));
        return manager;
    }

    [Fact]
    public void Resolves_the_integration_matching_the_executable()
    {
        var manager = CreateManager();

        Assert.Equal("chrome", manager.Resolve(App(@"C:\Program Files\Chrome\chrome.exe")).Id);
        Assert.Equal("code", manager.Resolve(App(@"C:\Program Files\Code\Code.exe")).Id);
    }

    [Fact]
    public void Unknown_applications_fall_back_to_generic()
    {
        var manager = CreateManager();
        Assert.Equal("generic", manager.Resolve(App(@"C:\apps\spotify.exe")).Id);
    }

    [Fact]
    public void Disabled_integrations_fall_back_to_generic()
    {
        var manager = CreateManager();
        var disabled = new HashSet<string> { "chrome" };

        Assert.Equal("generic", manager.Resolve(App(@"C:\apps\chrome.exe"), disabled).Id);
        Assert.Equal("code", manager.Resolve(App(@"C:\apps\code.exe"), disabled).Id);
    }

    [Fact]
    public void Applications_without_an_executable_use_generic()
    {
        var manager = CreateManager();
        var app = new RunningApp
        {
            Key = "pid:42",
            ExecutablePath = string.Empty,
            Windows = [new WindowInfo(new IntPtr(1), string.Empty, 42)],
            DisplayName = "app",
        };

        Assert.Equal("generic", manager.Resolve(app).Id);
    }

    [Fact]
    public void Register_ignores_duplicate_ids()
    {
        var manager = CreateManager();
        manager.Register(new StubIntegration("chrome", "other.exe"));

        Assert.Equal(2, manager.Registered.Count);
    }

    [Fact]
    public void Generic_is_exposed_for_direct_fallback()
    {
        var manager = CreateManager();
        Assert.Equal("generic", manager.Generic.Id);
    }
}
