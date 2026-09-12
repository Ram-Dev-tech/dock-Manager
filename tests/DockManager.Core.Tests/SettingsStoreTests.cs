using DockManager.Core.Dock;
using DockManager.Core.Settings;
using DockManager.Core.Tests.Support;

namespace DockManager.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Defaults_are_left_edge_normal_size_auto_hide()
    {
        var settings = new DockSettings();

        Assert.Equal(DockEdge.Left, settings.Edge);
        Assert.Equal(DockSizeScale.Normal, settings.Size);
        Assert.Equal(32, settings.IconSize);
        Assert.True(settings.AutoHide);
        Assert.False(settings.LaunchAtStartup);
        Assert.Equal(DockSettings.DefaultHideDelayMs, settings.HideDelayMs);
    }

    [Fact]
    public void Update_clamps_saves_and_notifies()
    {
        var repository = new InMemorySettingsRepository();
        var store = new SettingsStore(repository);
        DockSettings? observed = null;
        store.Changed += (_, e) => observed = e.Settings;

        store.Update(settings =>
        {
            settings.Edge = DockEdge.Right;
            settings.IconSize = 9999;
            settings.HideDelayMs = -10;
        });

        Assert.Equal(DockEdge.Right, store.Current.Edge);
        Assert.Equal(DockLayoutMetrics.MaxIconSize, store.Current.IconSize);
        Assert.Equal(0, store.Current.HideDelayMs);
        Assert.Same(store.Current, observed);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public void Update_does_not_mutate_the_previous_instance()
    {
        var store = new SettingsStore(new InMemorySettingsRepository());
        var before = store.Current;

        store.Update(settings => settings.Edge = DockEdge.Right);

        Assert.Equal(DockEdge.Left, before.Edge);
        Assert.Equal(DockEdge.Right, store.Current.Edge);
    }

    [Fact]
    public void Load_falls_back_to_defaults_when_the_repository_throws()
    {
        var store = new SettingsStore(new ThrowingSettingsRepository());

        var loaded = store.Load();

        Assert.Equal(DockEdge.Left, loaded.Edge);
        Assert.Equal(32, loaded.IconSize);
    }

    [Fact]
    public void Save_failures_do_not_break_the_running_session()
    {
        var store = new SettingsStore(new ThrowingSettingsRepository());
        store.Load();

        var updated = store.Update(settings => settings.AutoHide = false);

        Assert.False(updated.AutoHide);
    }

    [Fact]
    public void Load_reads_what_was_saved()
    {
        var repository = new InMemorySettingsRepository();
        repository.Save(new DockSettings { Edge = DockEdge.Right, Size = DockSizeScale.Large, IconSize = 40 });

        var store = new SettingsStore(repository);
        var loaded = store.Load();

        Assert.Equal(DockEdge.Right, loaded.Edge);
        Assert.Equal(DockSizeScale.Large, loaded.Size);
        Assert.Equal(40, loaded.IconSize);
    }

    [Fact]
    public void Sanitize_fixes_an_invalid_edge_and_size()
    {
        var settings = new DockSettings
        {
            Edge = (DockEdge)42,
            Size = (DockSizeScale)7,
            Sensitivity = (EdgeSensitivity)99,
            CursorPollIntervalMs = 1,
        }.Sanitized();

        Assert.Equal(DockEdge.Left, settings.Edge);
        Assert.Equal(DockSizeScale.Normal, settings.Size);
        Assert.Equal(EdgeSensitivity.Normal, settings.Sensitivity);
        Assert.Equal(2, settings.EdgeActivationPixels);
        Assert.Equal(15, settings.CursorPollIntervalMs);
    }

    [Theory]
    [InlineData(DockSizeScale.Compact, 26)]
    [InlineData(DockSizeScale.Normal, 32)]
    [InlineData(DockSizeScale.Large, 40)]
    public void Size_presets_map_to_icon_sizes(DockSizeScale scale, int expected)
        => Assert.Equal(expected, scale.IconSizeFor());

    [Fact]
    public void Settings_produce_matching_layout_and_visibility_configuration()
    {
        var settings = new DockSettings { IconSize = 40, AutoHide = false, HideDelayMs = 123 };

        var metrics = settings.CreateLayoutMetrics();
        var visibility = settings.CreateVisibilityOptions();

        Assert.Equal(40, metrics.IconSize);
        Assert.False(visibility.AutoHide);
        Assert.Equal(123, visibility.HideDelayMs);
    }
}
