using DockManager.Core.Dock;
using DockManager.Core.Items;
using DockManager.Core.Shell;
using DockManager.Core.Tests.Support;

namespace DockManager.Core.Tests;

public class PinnedItemTests
{
    private static readonly FakeFileSystem FileSystem = new FakeFileSystem()
        .WithFile(@"C:\apps\chrome.exe")
        .WithFile(@"C:\Start Menu\Google Chrome.lnk")
        .WithFile(@"C:\data\report.pdf")
        .WithDirectory(@"C:\data\projects");

    [Fact]
    public void Application_is_available_and_launches_its_target()
    {
        var item = new AppItem("id", @"C:\apps\chrome.exe") { ExecutablePath = @"C:\apps\chrome.exe" };

        Assert.Equal(ItemAvailability.Available, item.GetAvailability(FileSystem));
        Assert.Equal(DockSection.Applications, item.Section);

        var request = item.CreateLaunchRequest(FileSystem);
        Assert.NotNull(request);
        Assert.Equal(LaunchVerb.Execute, request!.Verb);
        Assert.Equal(@"C:\apps\chrome.exe", request.FilePath);
    }

    [Fact]
    public void Application_with_arguments_passes_them_through()
    {
        var item = new AppItem("id", @"C:\apps\chrome.exe")
        {
            Arguments = "--incognito",
            WorkingDirectory = @"C:\apps",
        };

        var request = item.CreateLaunchRequest(FileSystem);
        Assert.Equal("--incognito", request!.Arguments);
        Assert.Equal(@"C:\apps", request.WorkingDirectory);
    }

    [Fact]
    public void Missing_application_is_reported_and_cannot_launch()
    {
        var item = new AppItem("id", @"C:\apps\removed.exe");

        Assert.Equal(ItemAvailability.Missing, item.GetAvailability(FileSystem));
        Assert.Null(item.CreateLaunchRequest(FileSystem));
    }

    [Fact]
    public void Shell_targets_are_treated_as_available_and_never_match_running_apps()
    {
        var item = new AppItem("id", "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App");

        Assert.Equal(ItemAvailability.Available, item.GetAvailability(FileSystem));
        Assert.Equal(string.Empty, item.RunningMatchKey);
        Assert.NotNull(item.CreateLaunchRequest(FileSystem));
    }

    [Fact]
    public void File_opens_with_the_shell_default_verb()
    {
        var item = new FileItem("id", @"C:\data\report.pdf");

        Assert.Equal(ItemAvailability.Available, item.GetAvailability(FileSystem));
        Assert.Equal(DockSection.Files, item.Section);

        var request = item.CreateLaunchRequest(FileSystem);
        Assert.Equal(LaunchVerb.Open, request!.Verb);
        Assert.Equal(@"C:\data\report.pdf", request.FilePath);
    }

    [Fact]
    public void Deleted_file_is_missing_but_does_not_throw()
    {
        var item = new FileItem("id", @"C:\data\deleted.pdf");

        Assert.Equal(ItemAvailability.Missing, item.GetAvailability(FileSystem));
        Assert.Null(item.CreateLaunchRequest(FileSystem));

        // The item is still rendered and named so the user can remove it from the dock.
        Assert.Equal("deleted.pdf", item.EffectiveName);
    }

    [Fact]
    public void File_pointing_at_a_directory_is_invalid()
    {
        var item = new FileItem("id", @"C:\data\projects");
        Assert.Equal(ItemAvailability.Invalid, item.GetAvailability(FileSystem));
        Assert.Null(item.CreateLaunchRequest(FileSystem));
    }

    [Fact]
    public void Folder_opens_in_explorer()
    {
        var item = new FolderItem("id", @"C:\data\projects");

        Assert.Equal(ItemAvailability.Available, item.GetAvailability(FileSystem));

        var request = item.CreateLaunchRequest(FileSystem);
        Assert.Equal(LaunchVerb.Explore, request!.Verb);
        Assert.Equal("explorer.exe", request.FilePath);
        Assert.Equal("\"C:\\data\\projects\"", request.Arguments);
    }

    [Fact]
    public void Folder_that_is_actually_a_file_is_invalid()
    {
        var item = new FolderItem("id", @"C:\data\report.pdf");
        Assert.Equal(ItemAvailability.Invalid, item.GetAvailability(FileSystem));
        Assert.Null(item.CreateLaunchRequest(FileSystem));
    }

    [Fact]
    public void Empty_target_is_invalid()
    {
        Assert.Equal(ItemAvailability.Invalid, new FileItem("id", string.Empty).GetAvailability(FileSystem));
        Assert.Equal(ItemAvailability.Invalid, new AppItem("id", "  ").GetAvailability(FileSystem));
    }

    [Fact]
    public void Display_name_falls_back_to_the_file_name()
    {
        Assert.Equal("chrome", new AppItem("id", @"C:\apps\chrome.exe").EffectiveName);
        Assert.Equal("report.pdf", new FileItem("id", @"C:\data\report.pdf").EffectiveName);
        Assert.Equal("Docs", new FolderItem("id", @"C:\data\projects", "Docs").EffectiveName);
    }

    [Fact]
    public void Running_match_key_uses_the_resolved_executable()
    {
        var item = new AppItem("id", @"C:\Start Menu\Google Chrome.lnk")
        {
            ExecutablePath = @"C:\Program Files\Google\Chrome\chrome.exe",
        };

        Assert.Equal(@"c:\program files\google\chrome\chrome.exe", item.RunningMatchKey);
    }

    [Fact]
    public void Clone_copies_every_field()
    {
        var item = new AppItem("id", @"C:\apps\chrome.exe", "Chrome")
        {
            ExecutablePath = @"C:\apps\chrome.exe",
            Arguments = "--new-window",
            WorkingDirectory = @"C:\apps",
        };

        var clone = (AppItem)item.Clone();

        Assert.Equal(item.Id, clone.Id);
        Assert.Equal(item.TargetPath, clone.TargetPath);
        Assert.Equal(item.DisplayName, clone.DisplayName);
        Assert.Equal(item.ExecutablePath, clone.ExecutablePath);
        Assert.Equal(item.Arguments, clone.Arguments);
        Assert.Equal(item.WorkingDirectory, clone.WorkingDirectory);
    }

    [Fact]
    public void Factory_maps_paths_to_the_right_kind()
    {
        var shortcuts = new FakeShortcutResolver().With(
            @"C:\Start Menu\Google Chrome.lnk",
            new ShortcutTarget(@"C:\Program Files\Google\Chrome\chrome.exe")
            {
                Arguments = "--profile-directory=Default",
                WorkingDirectory = @"C:\Program Files\Google\Chrome",
            });

        var folder = PinnedItemFactory.CreateFromPath(@"C:\data\projects", FileSystem, shortcuts);
        Assert.IsType<FolderItem>(folder);

        var exe = PinnedItemFactory.CreateFromPath(@"C:\apps\chrome.exe", FileSystem, shortcuts);
        var appFromExe = Assert.IsType<AppItem>(exe);
        Assert.Equal(@"C:\apps\chrome.exe", appFromExe.ExecutablePath);

        var shortcut = PinnedItemFactory.CreateFromPath(@"C:\Start Menu\Google Chrome.lnk", FileSystem, shortcuts);
        var appFromShortcut = Assert.IsType<AppItem>(shortcut);
        Assert.Equal(@"C:\Program Files\Google\Chrome\chrome.exe", appFromShortcut.ExecutablePath);
        Assert.Equal("--profile-directory=Default", appFromShortcut.Arguments);

        var file = PinnedItemFactory.CreateFromPath(@"C:\data\report.pdf", FileSystem, shortcuts);
        Assert.IsType<FileItem>(file);
    }

    [Fact]
    public void Factory_refuses_paths_that_do_not_exist()
    {
        Assert.Null(PinnedItemFactory.CreateFromPath(@"C:\nowhere\missing.pdf", FileSystem));
        Assert.Null(PinnedItemFactory.CreateFromPath("   ", FileSystem));
    }

    [Fact]
    public void Factory_handles_a_batch_and_skips_bad_entries()
    {
        var items = PinnedItemFactory.CreateFromPaths(
            [@"C:\data\projects", @"C:\nowhere\missing.pdf", @"C:\data\report.pdf"],
            FileSystem);

        Assert.Equal(2, items.Count);
    }
}
