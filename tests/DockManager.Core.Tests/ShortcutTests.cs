using DockManager.Core.Settings;
using DockManager.Core.Shortcuts;

namespace DockManager.Core.Tests;

public class ShortcutTests
{
    [Fact]
    public void Defaults_bind_open_dock_and_item_stepping()
    {
        var shortcuts = ShortcutDefaults.Create();

        Assert.Equal(3, shortcuts.Count);
        Assert.Contains(shortcuts, s => s.Action == DockShortcutAction.OpenDock && !s.IsEmpty);
        Assert.Contains(shortcuts, s => s.Action == DockShortcutAction.NextItem);
        Assert.Contains(shortcuts, s => s.Action == DockShortcutAction.PreviousItem);
    }

    [Fact]
    public void Formats_a_combo_the_way_a_user_reads_it()
    {
        var record = new ShortcutRecord
        {
            Action = DockShortcutAction.OpenDock,
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = 0x20,
        };

        Assert.Equal("Ctrl + Shift + Space", ShortcutFormatter.Format(record));
    }

    [Fact]
    public void Empty_records_format_as_none()
    {
        Assert.Equal("None", ShortcutFormatter.Format(new ShortcutRecord()));
        Assert.Equal("None", ShortcutFormatter.Format(null));
    }

    [Theory]
    [InlineData(0x41u, "A")]
    [InlineData(0x35u, "5")]
    [InlineData(0x70u, "F1")]
    [InlineData(0x7Bu, "F12")]
    [InlineData(0x25u, "Left")]
    public void Formats_common_keys(uint key, string expected)
        => Assert.Equal(expected, ShortcutFormatter.FormatKey(key));

    [Theory]
    [InlineData(ShortcutModifiers.Control, 0x20u, true)]
    [InlineData(0u, 0x41u, false)]            // no modifier: would swallow typing
    [InlineData(ShortcutModifiers.Control, 0x11u, false)] // Ctrl alone
    public void Only_real_combos_are_capturable(uint modifiers, uint key, bool capturable)
        => Assert.Equal(capturable, ShortcutFormatter.IsCapturable(modifiers, key));

    [Theory]
    [InlineData(ShortcutModifiers.Win, 0x44u)]                                  // Win+D
    [InlineData(ShortcutModifiers.Win | ShortcutModifiers.Control, 0x4Cu)]      // Win+Ctrl+L
    [InlineData(ShortcutModifiers.Alt, 0x09u)]                                  // Alt+Tab
    [InlineData(ShortcutModifiers.Alt | ShortcutModifiers.Shift, 0x09u)]        // Alt+Shift+Tab
    [InlineData(ShortcutModifiers.Alt, 0x1Bu)]                                  // Alt+Esc
    [InlineData(ShortcutModifiers.Control, 0x1Bu)]                              // Ctrl+Esc
    [InlineData(ShortcutModifiers.Control | ShortcutModifiers.Shift, 0x1Bu)]    // Ctrl+Shift+Esc
    public void System_shortcuts_are_reserved(uint modifiers, uint key)
        => Assert.True(ShortcutValidator.IsReservedBySystem(modifiers, key));

    [Fact]
    public void The_defaults_are_never_reserved()
    {
        foreach (var record in ShortcutDefaults.Create())
        {
            Assert.False(ShortcutValidator.IsReservedBySystem(record.Modifiers, record.Key));
        }
    }

    [Fact]
    public void Rejection_reason_explains_reserved_combos_in_words()
        => Assert.Equal(
            "This shortcut belongs to Windows and cannot be used.",
            ShortcutValidator.RejectionReason(ShortcutModifiers.Alt, 0x09));

    [Fact]
    public void Sanitize_clears_duplicate_combos_so_actions_never_collide()
    {
        var settings = new DockSettings
        {
            Shortcuts =
            [
                new ShortcutRecord { Action = DockShortcutAction.OpenDock, Modifiers = ShortcutModifiers.Control, Key = 0x41 },
                new ShortcutRecord { Action = DockShortcutAction.NextItem, Modifiers = ShortcutModifiers.Control, Key = 0x41 },
            ],
        };

        var sanitized = settings.Sanitized();

        Assert.False(sanitized.Shortcuts[0].IsEmpty);
        Assert.True(sanitized.Shortcuts[1].IsEmpty);
    }

    [Fact]
    public void Sanitize_copies_shortcut_records_so_drafts_do_not_alias()
    {
        var original = new DockSettings();
        var sanitized = original.Sanitized();

        sanitized.Shortcuts[0].Key = 0x99;

        Assert.NotEqual(0x99u, original.Shortcuts[0].Key);
    }

    [Fact]
    public void Phase2_files_without_phase3_fields_still_load()
    {
        var json = """
        {
          "edge": "Left",
          "hoverDelayMs": 150,
          "disabledIntegrations": ["chrome"]
        }
        """;

        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var loaded = System.Text.Json.JsonSerializer.Deserialize<DockSettings>(json, options);

        Assert.NotNull(loaded);
        Assert.Equal(DockTheme.System, loaded!.Theme);
        Assert.Equal(EdgeSensitivity.Normal, loaded.Sensitivity);
        Assert.True(loaded.AnimationsEnabled);
        Assert.Empty(loaded.MonitorName);
    }
}
