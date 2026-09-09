using DockManager.Core.Shell;

namespace DockManager.Core.Tests;

public class PathNormalizerTests
{
    [Theory]
    [InlineData(@"C:\Apps\Chrome.exe", @"c:\apps\chrome.exe")]
    [InlineData("c:/apps/chrome.exe", @"c:\apps\chrome.exe")]
    [InlineData(@"C:\Apps\Chrome.exe\", @"c:\apps\chrome.exe")]
    [InlineData("  C:\\Apps\\Chrome.exe  ", @"c:\apps\chrome.exe")]
    public void NormalizeKey_canonicalises_windows_paths(string input, string expected)
        => Assert.Equal(expected, PathNormalizer.NormalizeKey(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeKey_handles_empty_input(string? input)
        => Assert.Equal(string.Empty, PathNormalizer.NormalizeKey(input));

    [Theory]
    [InlineData(@"C:\data\report.pdf", "report.pdf")]
    [InlineData(@"C:\data\projects\", "projects")]
    [InlineData("report.pdf", "report.pdf")]
    public void FileName_returns_the_last_segment(string input, string expected)
        => Assert.Equal(expected, PathNormalizer.FileName(input));

    [Theory]
    [InlineData(@"C:\apps\Google Chrome.exe", "Google Chrome")]
    [InlineData(@"C:\data\archive.tar.gz", "archive.tar")]
    [InlineData(@"C:\data\LICENSE", "LICENSE")]
    public void DisplayName_strips_only_the_last_extension(string input, string expected)
        => Assert.Equal(expected, PathNormalizer.DisplayName(input));

    [Theory]
    [InlineData(@"C:\apps\chrome.EXE", ".exe")]
    [InlineData(@"C:\data\notes", "")]
    public void Extension_is_lower_cased(string input, string expected)
        => Assert.Equal(expected, PathNormalizer.Extension(input));

    [Fact]
    public void SameExecutable_matches_on_full_path_or_file_name()
    {
        Assert.True(PathNormalizer.SameExecutable(@"C:\Apps\chrome.exe", @"c:/apps/CHROME.EXE"));
        Assert.True(PathNormalizer.SameExecutable(@"D:\Portable\chrome.exe", @"C:\Program Files\chrome.exe"));
        Assert.False(PathNormalizer.SameExecutable(@"C:\Apps\chrome.exe", @"C:\Apps\code.exe"));
        Assert.False(PathNormalizer.SameExecutable(@"C:\Apps\chrome.exe", null));
        Assert.False(PathNormalizer.SameExecutable(string.Empty, string.Empty));
    }
}
