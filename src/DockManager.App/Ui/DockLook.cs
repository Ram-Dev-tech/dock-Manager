using System.ComponentModel;
using System.Runtime.CompilerServices;
using DockManager.Core.Dock;
using DockManager.Core.Settings;

namespace DockManager.App.Ui;

/// <summary>
/// The numbers the dock template binds to through <c>DynamicResource</c>. Changing a setting only
/// replaces these values, so the panel resizes without rebuilding the window.
/// </summary>
public sealed class DockLook : INotifyPropertyChanged
{
    private DockLayoutMetrics _metrics = DockLayoutMetrics.ForIconSize(32);
    private double _panelOpacity = 0.96;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double IconSize => _metrics.IconSize;

    public double ItemSize => _metrics.ItemSize;

    public double DockPadding => _metrics.DockPadding;

    public double SeparatorThickness => _metrics.SeparatorThickness;

    public double SectionGap => _metrics.SectionGap;

    public double GlyphFontSize => Math.Round(_metrics.IconSize * 0.5d);

    public double RunningIndicatorSize => Math.Max(4d, Math.Round(_metrics.IconSize * 0.16d));

    public double ItemCornerRadius => Math.Max(6d, Math.Round(_metrics.ItemSize * 0.28d));

    public double PanelCornerRadius => Math.Max(10d, Math.Round(_metrics.DockWidth * 0.26d));

    public double PanelOpacity => _panelOpacity;

    public DockLayoutMetrics Metrics => _metrics;

    public void Apply(DockSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _metrics = settings.CreateLayoutMetrics();
        _panelOpacity = settings.PanelOpacity;

        OnPropertyChanged(nameof(IconSize));
        OnPropertyChanged(nameof(ItemSize));
        OnPropertyChanged(nameof(DockPadding));
        OnPropertyChanged(nameof(SeparatorThickness));
        OnPropertyChanged(nameof(SectionGap));
        OnPropertyChanged(nameof(GlyphFontSize));
        OnPropertyChanged(nameof(RunningIndicatorSize));
        OnPropertyChanged(nameof(ItemCornerRadius));
        OnPropertyChanged(nameof(PanelCornerRadius));
        OnPropertyChanged(nameof(PanelOpacity));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
