namespace DockManager.Core.Tests;

public class VSCodeTitleParserTests
{
    [Fact]
    public void Parses_document_and_project()
    {
        var (document, project) = VSCodeTitleParser.Parse("README.md - my-app - Visual Studio Code");
        Assert.Equal("README.md", document);
        Assert.Equal("my-app", project);
    }

    [Fact]
    public void Parses_project_only()
    {
        var (document, project) = VSCodeTitleParser.Parse("website - Visual Studio Code");
        Assert.Null(document);
        Assert.Equal("website", project);
    }

    [Fact]
    public void Handles_insiders_suffix()
    {
        var (_, project) = VSCodeTitleParser.Parse("my-app - Visual Studio Code Insiders");
        Assert.Equal("my-app", project);
    }

    [Fact]
    public void Strips_dirty_marker_from_the_document()
    {
        var (document, _) = VSCodeTitleParser.Parse("● settings.json - ClipBo - Visual Studio Code");
        Assert.Equal("settings.json", document);
    }

    [Fact]
    public void Plain_title_yields_nothing()
    {
        var (document, project) = VSCodeTitleParser.Parse("Visual Studio Code");
        Assert.Null(document);
        Assert.Null(project);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_titles_yield_nothing(string? title)
    {
        var (document, project) = VSCodeTitleParser.Parse(title);
        Assert.Null(document);
        Assert.Null(project);
    }

    [Fact]
    public void Untitled_documents_are_kept()
    {
        var (document, project) = VSCodeTitleParser.Parse("Untitled-1 - ClipBo - Visual Studio Code");
        Assert.Equal("Untitled-1", document);
        Assert.Equal("ClipBo", project);
    }
}
