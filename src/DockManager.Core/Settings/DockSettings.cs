using DockManager.Core.Dock;

namespace DockManager.Core.Settings;

/// <summary>Dock size presets. They only differ by icon size, which drives every other spacing.</summary>
public enum DockSizeScale
{
    Compact = 0,
    Normal = 1,
    Large = 2,
}

public static class DockSizeScaleExtensions
{
    public static int IconSizeFor(this DockSizeScale scale) => scale switch
    {
        DockSizeScale.Compact => 26,
        DockSizeScale.Large => 40,
        _ => 32,
    };

    public static DockSizeScale Nearest(int iconSize) => iconSize switch
    {
        <= 28 => DockSizeScale.Compact,
        >= 36 => DockSizeScale.Large,
        _ => DockSizeScale.Normal,
    };
}

/// <summary>
/// Everything Phase 1 lets the user configure. Deliberately small: position, size, auto-hide and
/// startup. Anything richer belongs to a later phase.
/// </summary>
public sealed class DockSettings
{
    public const int MinHideDelayMs = 0;
    public const int MaxHideDelayMs = 5000;
    public const int DefaultHideDelayMs = 380;

    /// <summary>Left or right edge of the primary display. Default is left.</summary>
    public DockEdge Edge { get; set; } = DockEdge.Left;

    /// <summary>Size preset. Selecting one also sets <see cref="IconSize"/>.</summary>
    public DockSizeScale Size { get; set; } = DockSizeScale.Normal;

    /// <summary>Icon size in DIPs. Drives the width of the whole dock.</summary>
    public int IconSize { get; set; } = 32;

    /// <summary>Whether the dock hides itself when the cursor leaves.</summary>
    public bool AutoHide { get; set; } = true;

    /// <summary>Grace period after the cursor leaves before the dock starts to hide.</summary>
    public int HideDelayMs { get; set; } = DefaultHideDelayMs;

    /// <summary>Dwell time at the edge before revealing. 0 keeps the dock instant.</summary>
    public int RevealDelayMs { get; set; }

    /// <summary>Start the dock when Windows starts.</summary>
    public bool LaunchAtStartup { get; set; }

    /// <summary>How many pixels of the screen edge count as the activation strip.</summary>
    public int EdgeActivationPixels { get; set; } = 2;

    /// <summary>Opacity of the dock panel background.</summary>
    public double PanelOpacity { get; set; } = 0.96;

    /// <summary>Show a tooltip with the item name and state.</summary>
    public bool ShowTooltips { get; set; } = true;

    /// <summary>How often the cursor is polled while the dock is hidden, in milliseconds.</summary>
    public int CursorPollIntervalMs { get; set; } = 40;

    // ----- Phase 2: application aware hover panel -------------------------------------------

    /// <summary>Reveal the secondary panel with an application's open items on hover.</summary>
    public bool ShowAppItemsOnHover { get; set; } = true;

    /// <summary>Show a small captured preview of a hovered window in the panel.</summary>
    public bool ShowWindowPreviews { get; set; } = true;

    /// <summary>Collapse several windows of one application into a single entry.</summary>
    public bool GroupMultipleWindows { get; set; }

    /// <summary>How long the cursor must rest on an application before the panel opens.</summary>
    public int HoverDelayMs { get; set; } = 200;

    /// <summary>Ids of integrations the user switched off (e.g. "chrome", "edge").</summary>
    public List<string> DisabledIntegrations { get; set; } = [];

    public DockLayoutMetrics CreateLayoutMetrics() => DockLayoutMetrics.ForIconSize(IconSize);

    public DockVisibilityOptions CreateVisibilityOptions() => new()
    {
        AutoHide = AutoHide,
        HideDelayMs = HideDelayMs,
        RevealDelayMs = RevealDelayMs,
    };

    public DockSettings Clone() => (DockSettings)MemberwiseClone();

    /// <summary>Applies the documented ranges and returns the corrected settings.</summary>
    public DockSettings Sanitized()
    {
        var copy = Clone();
        if (!copy.Edge.IsValid())
        {
            copy.Edge = DockEdge.Left;
        }

        if (!Enum.IsDefined(copy.Size))
        {
            copy.Size = DockSizeScale.Normal;
        }

        copy.IconSize = Math.Clamp(copy.IconSize, DockLayoutMetrics.MinIconSize, DockLayoutMetrics.MaxIconSize);
        copy.HideDelayMs = Math.Clamp(copy.HideDelayMs, MinHideDelayMs, MaxHideDelayMs);
        copy.RevealDelayMs = Math.Clamp(copy.RevealDelayMs, 0, 2000);
        copy.EdgeActivationPixels = Math.Clamp(copy.EdgeActivationPixels, 1, 24);
        copy.PanelOpacity = Math.Clamp(copy.PanelOpacity, 0.35d, 1d);
        copy.CursorPollIntervalMs = Math.Clamp(copy.CursorPollIntervalMs, 15, 250);
        copy.HoverDelayMs = Math.Clamp(copy.HoverDelayMs, 0, 2000);
        copy.DisabledIntegrations = copy.DisabledIntegrations is null ? [] : copy.DisabledIntegrations.ToList();
        return copy;
    }
}
