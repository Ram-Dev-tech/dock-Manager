using System.Windows;
using System.Windows.Media;
using DockManager.Core.Settings;
using Microsoft.Win32;

namespace DockManager.App.Ui;

/// <summary>
/// Resolves the configured theme against the Windows setting and publishes the resulting brushes as
/// application resources, so the dock and the hover panel stay in sync through DynamicResource.
/// </summary>
internal static class ThemeService
{
    /// <summary>Maps <see cref="DockTheme.System"/> onto the current Windows app theme.</summary>
    public static DockTheme Resolve(DockTheme configured)
    {
        if (configured != DockTheme.System)
        {
            return configured;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int useLight)
            {
                return useLight == 0 ? DockTheme.Dark : DockTheme.Light;
            }
        }
        catch (Exception)
        {
            // Registry unavailable; the dark default stands.
        }

        return DockTheme.Dark;
    }

    /// <summary>Swaps the shared palette. Called on startup, on settings change and on theme change.</summary>
    public static void Apply(DockTheme resolved, double panelOpacity)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var alpha = (byte)Math.Clamp((int)Math.Round(panelOpacity * 255), 89, 255);

        if (resolved == DockTheme.Light)
        {
            resources["PanelBackground"] = Brush(Color.FromArgb(alpha, 0xF7, 0xF7, 0xF9));
            resources["PanelBorder"] = Brush(Color.FromArgb(0x33, 0x00, 0x00, 0x00));
            resources["TextPrimary"] = Brush(Color.FromRgb(0x1B, 0x1D, 0x24));
            resources["TextSecondary"] = Brush(Color.FromRgb(0x5A, 0x60, 0x70));
            resources["AccentBrush"] = Brush(Color.FromRgb(0x0F, 0x6C, 0xBD));
            resources["ItemHover"] = Brush(Color.FromArgb(0x14, 0x00, 0x00, 0x00));
            resources["ItemActive"] = Brush(Color.FromArgb(0x24, 0x00, 0x00, 0x00));
        }
        else
        {
            resources["PanelBackground"] = Brush(Color.FromArgb(alpha, 0x1C, 0x1C, 0x21));
            resources["PanelBorder"] = Brush(Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF));
            resources["TextPrimary"] = Brush(Color.FromRgb(0xF2, 0xF5, 0xF7));
            resources["TextSecondary"] = Brush(Color.FromRgb(0x9A, 0xA0, 0xAA));
            resources["AccentBrush"] = Brush(Color.FromRgb(0x6E, 0xC1, 0xFF));
            resources["ItemHover"] = Brush(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF));
            resources["ItemActive"] = Brush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
        }
    }

    private static Brush Brush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
