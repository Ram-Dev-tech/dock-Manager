using DockManager.Core.Settings;

namespace DockManager.Core.Tests;

public class Phase2SettingsTests
{
    [Fact]
    public void Phase2_defaults_are_sane()
    {
        var settings = new DockSettings();

        Assert.True(settings.ShowAppItemsOnHover);
        Assert.True(settings.ShowWindowPreviews);
        Assert.False(settings.GroupMultipleWindows);
        Assert.Equal(200, settings.HoverDelayMs);
        Assert.Empty(settings.DisabledIntegrations);
    }

    [Fact]
    public void Hover_delay_is_clamped()
    {
        Assert.Equal(0, new DockSettings { HoverDelayMs = -5 }.Sanitized().HoverDelayMs);
        Assert.Equal(2000, new DockSettings { HoverDelayMs = 99999 }.Sanitized().HoverDelayMs);
    }

    [Fact]
    public void Sanitize_never_returns_a_null_disabled_list()
    {
        var settings = new DockSettings { DisabledIntegrations = null! }.Sanitized();
        Assert.NotNull(settings.DisabledIntegrations);
    }

    [Fact]
    public void Sanitize_copies_the_disabled_list_so_drafts_do_not_alias()
    {
        var original = new DockSettings { DisabledIntegrations = ["chrome"] };
        var sanitized = original.Sanitized();

        sanitized.DisabledIntegrations.Add("edge");

        Assert.Equal(new[] { "chrome" }, original.DisabledIntegrations);
    }

    [Fact]
    public void Phase1_settings_files_without_phase2_fields_still_load()
    {
        // A settings document written by Phase 1: the new fields must keep their defaults.
        var json = """
        {
          "edge": "Right",
          "iconSize": 32,
          "autoHide": true
        }
        """;

        // Enums are persisted as strings by the repository, so the test mirrors that configuration.
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var loaded = System.Text.Json.JsonSerializer.Deserialize<DockSettings>(json, options);

        Assert.NotNull(loaded);
        Assert.Equal(DockManager.Core.Dock.DockEdge.Right, loaded!.Edge);
        Assert.True(loaded.ShowAppItemsOnHover);
        Assert.Equal(200, loaded.HoverDelayMs);
        Assert.Empty(loaded.DisabledIntegrations);
    }

    [Fact]
    public void Disabled_integrations_round_trip()
    {
        var settings = new DockSettings { DisabledIntegrations = ["chrome", "edge"] };
        var sanitized = settings.Sanitized();

        Assert.Equal(new[] { "chrome", "edge" }, sanitized.DisabledIntegrations);
    }
}
