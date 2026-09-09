using DockManager.Core.Windows;

namespace DockManager.Core.Tests;

public class RunningAppIndexTests
{
    private static WindowInfo Window(int handle, string exe, int pid, bool minimized = false, string title = "Window")
        => new(new IntPtr(handle), exe, pid) { IsMinimized = minimized, Title = title };

    [Fact]
    public void Windows_of_the_same_executable_are_grouped_into_one_app()
    {
        var index = RunningAppIndex.Build(
        [
            Window(1, @"C:\Program Files\Google\Chrome\chrome.exe", 100),
            Window(2, @"C:\Program Files\Google\Chrome\chrome.exe", 100),
            Window(3, @"C:\Program Files\Microsoft VS Code\Code.exe", 200),
        ]);

        Assert.Equal(2, index.Apps.Count);
        Assert.Equal(3, index.Windows.Count);

        var chrome = index.FindByExecutable(@"C:\Program Files\Google\Chrome\chrome.exe");
        Assert.NotNull(chrome);
        Assert.Equal(2, chrome!.WindowCount);
        Assert.Equal("chrome", chrome.DisplayName);
    }

    [Fact]
    public void Lookup_tolerates_case_and_separator_differences()
    {
        var index = RunningAppIndex.Build([Window(1, @"C:\Program Files\Google\Chrome\chrome.exe", 100)]);

        Assert.NotNull(index.FindByExecutable(@"c:/program files/google/chrome/CHROME.EXE"));
        Assert.True(index.IsRunning(@"C:\PROGRAM FILES\GOOGLE\CHROME\chrome.exe"));
    }

    [Fact]
    public void Lookup_falls_back_to_the_file_name()
    {
        var index = RunningAppIndex.Build([Window(1, @"D:\Portable\chrome.exe", 100)]);

        // A pinned shortcut points at the installed location but the same binary is running.
        Assert.NotNull(index.FindByExecutable(@"C:\Program Files\Google\Chrome\chrome.exe"));
    }

    [Fact]
    public void Unknown_executables_are_not_matched()
    {
        var index = RunningAppIndex.Build([Window(1, @"C:\apps\chrome.exe", 100)]);

        Assert.Null(index.FindByExecutable(@"C:\apps\code.exe"));
        Assert.Null(index.FindByExecutable(null));
        Assert.False(index.IsRunning(string.Empty));
    }

    [Fact]
    public void Windows_without_an_executable_stay_switchable_by_process()
    {
        var index = RunningAppIndex.Build([Window(7, string.Empty, 4242)]);

        var app = Assert.Single(index.Apps);
        Assert.Equal("pid:4242", app.Key);
        Assert.Equal(new IntPtr(7), app.PreferredWindow);
        Assert.Null(index.FindByExecutable(@"C:\anything.exe"));
    }

    [Fact]
    public void Minimized_state_and_preferred_window_are_computed()
    {
        var allMinimized = RunningAppIndex.Build(
        [
            Window(1, @"C:\apps\spotify.exe", 1, minimized: true),
            Window(2, @"C:\apps\spotify.exe", 1, minimized: true),
        ]);
        Assert.True(allMinimized.FindByExecutable(@"C:\apps\spotify.exe")!.IsMinimized);
        Assert.Equal(new IntPtr(1), allMinimized.FindByExecutable(@"C:\apps\spotify.exe")!.PreferredWindow);

        var oneVisible = RunningAppIndex.Build(
        [
            Window(1, @"C:\apps\spotify.exe", 1, minimized: true),
            Window(2, @"C:\apps\spotify.exe", 1),
        ]);
        var app = oneVisible.FindByExecutable(@"C:\apps\spotify.exe")!;
        Assert.False(app.IsMinimized);
        Assert.Equal(new IntPtr(2), app.PreferredWindow);
    }

    [Fact]
    public void Active_window_is_resolved_by_handle()
    {
        var index = RunningAppIndex.Build(
        [
            Window(1, @"C:\apps\chrome.exe", 1),
            Window(2, @"C:\apps\code.exe", 2),
        ]);

        Assert.Equal("code", index.FindByWindowHandle(new IntPtr(2))!.DisplayName);
        Assert.Null(index.FindByWindowHandle(IntPtr.Zero));
        Assert.Null(index.FindByWindowHandle(new IntPtr(999)));
    }

    [Fact]
    public void Empty_index_is_safe_to_query()
    {
        Assert.False(RunningAppIndex.Empty.Any);
        Assert.Null(RunningAppIndex.Empty.FindByExecutable(@"C:\apps\chrome.exe"));
        Assert.Null(RunningAppIndex.Build(null).FindByWindowHandle(new IntPtr(1)));
    }
}
